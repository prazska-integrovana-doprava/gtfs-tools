using CommonLibrary;
using GtfsLogging;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using JdfToGtfsProcessor.Stops;

namespace JdfToGtfsProcessor.Transfers
{
    /// <summary>
    /// Zpracovává garantované přestupní poznámky z externích dat
    /// NEpoužívá soubor navaznosti.txt
    /// </summary>
    internal class ExternalTimedTransfersProcessor
    {
        private List<XmlTransfer> busToBusTransfers;

        private List<XmlTransfer> trainToBusTransfers;

        private ISimpleLogger log;
        private StopDatabase stopDb;

        public ExternalTimedTransfersProcessor(List<XmlTransfer> busToBusTransfers, List<XmlTransfer> trainToBusTransfers, ISimpleLogger log, StopDatabase stopDb)
        {
            this.busToBusTransfers = busToBusTransfers;
            this.trainToBusTransfers = trainToBusTransfers;
            this.log = log;
            this.stopDb = stopDb;
        }

        /// <summary>
        /// Zpracuje přestupní poznámky mezi autobusy. Vrací vygenerované přestupy.
        /// </summary>
        /// <param name="routes">Linky a jejich spoje</param>
        public List<TimedTransfer> ProcessBusToBusTransfers(IDictionary<string, Route> routes)
        {
            var result = new List<TimedTransfer>();

            foreach (var transfer in busToBusTransfers)
            {
                var stop1name = stopDb.StopsToGtfsMapping.GetValueOrDefault(transfer.Stop1)?.StopName ?? "?";
                log.Log($"BUS {transfer.Line1}/{transfer.Trip1} v zastávce {stop1name} ({transfer.Stop1}) odj {transfer.Departure1Time} čeká na BUS {transfer.Line2}/{transfer.Trip2} příj. {transfer.Arrival2Time} max {transfer.MaxWait} min.");

                // teoreticky to může být množina spojů, pokud je tam nějaká změna v průběhu platnosti
                var trips1 = FindBusTrip(routes, transfer.Line1, transfer.Trip1);
                var trips2 = FindBusTrip(routes, transfer.Line2, transfer.Trip2);
                ProcessTransferOnTrips(trips1, trips2, transfer, result);
            }

            return result;
        }

        /// <summary>
        /// Zpracuje přestupní poznámky vyčkávání autobusů na vlaky. Vrací vygenerované přestupy.
        /// </summary>
        /// <param name="routes">Linky a jejich spoje</param>
        public List<TimedTransfer> ProcessTrainToBusTransfers(IDictionary<string, Route> routes)
        {
            // nejdřív si vyrobíme mapu čísel vlaků na spoje
            var trainTripsByNumber = new Dictionary<int, List<Trip>>();
            var trainTrips = routes.Values.Where(r => r.Type == TrafficType.Rail).SelectMany(r => r.Trips);
            foreach (var trainTrip in trainTrips)
            {
                var trainNumber = int.Parse(new string(trainTrip.ShortName.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray()));
                trainTripsByNumber.GetValueAndAddIfMissing(trainNumber, new()).Add(trainTrip);
            }

            var result = new List<TimedTransfer>();
            
            foreach (var transfer in trainToBusTransfers)
            {
                var stop1name = stopDb.StopsToGtfsMapping.GetValueOrDefault(transfer.Stop1)?.StopName ?? "?";
                log.Log($"BUS {transfer.Line1}/{transfer.Trip1} v zastávce {stop1name} ({transfer.Stop1}) odj {transfer.Departure1Time} čeká na VLAK {transfer.Line2} příj. {transfer.Arrival2Time} max {transfer.MaxWait} min.");

                var trips1 = FindBusTrip(routes, transfer.Line1, transfer.Trip1);
                var trips2 = FindTrainTrip(trainTripsByNumber, transfer.Line2);
                ProcessTransferOnTrips(trips1, trips2, transfer, result);
            }

            return result;
        }

        private List<Trip> FindBusTrip(IDictionary<string, Route> routes, int routeLicenceNumber, int tripNumber)
        {
            if (routes.TryGetValue(routeLicenceNumber.ToString(), out var route))
            {
                var result = route.Trips.Where(t => t.TripNumber == tripNumber).ToList();
                if (result.Count > 0)
                {
                    log.Log($"  - [OK] {routeLicenceNumber}/{tripNumber} = {string.Join(", ", result.Select(t => t.GtfsId))}");
                }
                else
                {
                    log.Log($"  - [WARN] na lince {routeLicenceNumber} nenalezen spoj {tripNumber}.");
                }

                return result;
            }
            else
            {
                log.Log($"  - [WARN] nenalezena linka {routeLicenceNumber}");
                return new();
            }
        }

        private List<Trip> FindTrainTrip(IDictionary<int, List<Trip>> trainTrips, int trainNumber)
        {
            if (trainTrips.TryGetValue(trainNumber, out var trips))
            {
                log.Log($"  - [OK] vlak {trainNumber} = {string.Join(", ", trips.Select(t => t.GtfsId))}");
                return trips;
            }
            else
            {
                log.Log($"  - [WARN] nenalezen vlak {trainNumber}");
                return new();
            }
        }

        private void ProcessTransferOnTrips(List<Trip> trips1, List<Trip> trips2, XmlTransfer transfer, List<TimedTransfer> result)
        {
            foreach (var trip1 in trips1)
            {
                var stopTime1 = FindStopTimeOnTrip(trip1, transfer.Stop1, transfer.Departure1Time, TimeSpec.DEPARTURE);
                foreach (var trip2 in trips2)
                {
                    if (!trip1.CalendarRecord.IntersectDatesWith(trip2.CalendarRecord).Any())
                    {
                        continue;
                    }

                    var stopTime2 = FindStopTimeOnTrip(trip2, transfer.Stop2, transfer.Arrival2Time, TimeSpec.ARRIVAL);
                    if (stopTime1 != null && stopTime2 != null)
                    {
                        if (stopTime2.ArrivalTime > stopTime1.DepartureTime)
                        {
                            log.Log($"  - [WARN] {trip2.GtfsId} má příjezd ({stopTime2.ArrivalTime}) po odjezdu {trip1.GtfsId} ({stopTime1.DepartureTime}). Ignoruji.");
                            continue;
                        }

                        if (result.Any(tt => tt.FromTrip == trip2 && tt.FromStop == stopTime2.Stop && tt.ToTrip == trip1 && tt.ToStop == stopTime1.Stop))
                        {
                            log.Log($"  - [WARN] vazba z {trip2.GtfsId} na {trip1.GtfsId} už byla jednou vložena.");
                            continue;
                        }

                        result.Add(new TimedTransfer
                        {
                            FromTrip = trip2,
                            FromStop = stopTime2.Stop,
                            ToTrip = trip1,
                            ToStop = stopTime1.Stop,
                            MaxWaitingTimeSeconds = transfer.MaxWait * 60
                        });
                    }
                }
            }
        }

        internal enum TimeSpec
        {
            ARRIVAL,
            DEPARTURE
        }

        private StopTime? FindStopTimeOnTrip(Trip trip, int stopNumber, Time time, TimeSpec timeSpec)
        {
            var stopTimes = trip.StopTimes.Where(st => st.Stop.CisId == stopNumber).ToList();
            if (stopTimes.Count == 0)
            {
                log.Log($"  - [WARN] {trip.GtfsId} nemá zastávku s CIS ID {stopNumber}");
                return null;
            }
            else if (stopTimes.Count == 1)
            {
                var result = stopTimes.First();
                var delta = 0;
                if (timeSpec == TimeSpec.DEPARTURE) delta = result.DepartureTime - time;
                else delta = result.ArrivalTime - time;
                if (delta > -60 && delta < 60)
                {
                    log.Log($"  - [OK] {result} (dle ID zastávky)");
                    return result;
                }
                else if (delta >= -300 && delta <= 300)
                {
                    log.Log($"  - [WARN] {result} (dle ID zastávky, čas se liší o {delta / 60} minut; v toleranci)");
                    return result;
                }
                else
                {
                    log.Log($"  - [WARN] {result} (dle ID zastávky, čas se liší o {delta / 60} minut; mimo toleranci, ignoruji)");
                    return null;
                }
            }
            else
            {
                var result = stopTimes.FirstOrDefault(st =>
                    timeSpec == TimeSpec.DEPARTURE && st.DepartureTime.ModuloDay() == time
                    || timeSpec == TimeSpec.ARRIVAL && st.ArrivalTime.ModuloDay() == time);
                if (result != null)
                {
                    log.Log($"  - [OK] {result} (dle zastávky + času)");
                }
                else
                {
                    log.Log($"  - [WARN] nejednoznačné! {string.Join("; ", stopTimes)}");
                }

                return result;
            }
        }
    }
}
