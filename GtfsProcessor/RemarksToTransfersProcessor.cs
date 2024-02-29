using System.Collections.Generic;
using System.Linq;
using GtfsLogging;
using GtfsModel.Enumerations;
using CommonLibrary;
using GtfsModel.Extended;
using System;
using GtfsProcessor.DataClasses;
using GtfsProcessor.Logging;

namespace GtfsProcessor
{
    /// <summary>
    /// Zapracovává do GTFS modelu poznámky o garantovaných návaznostech (<see cref="AswModel.Extended.Remark"/>).
    /// 
    /// Každá poznámka, pokud je vyčkávací, je přiřazena k nějakému zastavení a obsahuje info o druhém spoji a zastávce, odkud probíhá přestup.
    /// 
    /// Poznámky jsou v GTFS ukládány jako <see cref="TimedTransfer"/>.
    /// </summary>
    class RemarksToTransfersProcessor
    {
        // porovnání detekující shodné přestupní poznámky (pro detekci duplicit)
        private class RemarkComparer : IEqualityComparer<AswModel.Extended.Remark>
        {
            public bool Equals(AswModel.Extended.Remark x, AswModel.Extended.Remark y)
            {
                return x.FromStop.Equals(y.FromStop) && x.ToStop.Equals(y.ToStop) && x.FromRouteLineNumber == y.FromRouteLineNumber && x.FromRouteStopDirection.Equals(y.FromRouteStopDirection);
            }

            public int GetHashCode(AswModel.Extended.Remark obj)
            {
                return obj.FromStop.GetHashCode() ^ obj.ToStop.GetHashCode()
                    ^ (obj.FromRouteLineNumber.GetHashCode()) ^ (obj.FromRouteStopDirection?.GetHashCode()).GetValueOrDefault();
            }
        }


        ICommonLogger log = Loggers.CommonLoggerInstance;
        ISimpleLogger transferLog = Loggers.TransferLoggerInstance;

        private DateTime globalStartDate;
        private Dictionary<int, List<Route>> gtfsRoutesByAswId;
        private Dictionary<AswModel.Extended.Stop, StopVariantsMapping> stopsTransformation;
        
        public RemarksToTransfersProcessor(IEnumerable<Route> routes, Dictionary<AswModel.Extended.Stop, StopVariantsMapping> stopsTransformation, DateTime globalStartDate)
        {
            gtfsRoutesByAswId = routes.GroupBy(r => r.AswId).ToDictionary(group => group.Key, group => group.ToList());
            this.stopsTransformation = stopsTransformation;
            this.globalStartDate = globalStartDate;
        }

        /// <summary>
        /// Přijme všechna zastavení, která mají přiřazené nějaké poznámky a návazné poznámky zpracuje do instancí <see cref="TimedTransfer"/>.
        /// </summary>
        /// <param name="stopTimesWithTimedTransferRemarks">Zastavení s návaznými poznámkami</param>
        /// <returns></returns>
        public IEnumerable<TimedTransfer> ParseTimedTransferRemarks(Dictionary<StopTime, List<AswModel.Extended.Remark>> stopTimesWithTimedTransferRemarks)
        {
            // pro detekci shodných přestupů
            var allTransfers = new Dictionary<TimedTransfer, StopTime>();

            foreach (var stopTimeWithRemarks in stopTimesWithTimedTransferRemarks)
            {
                var stopTime = stopTimeWithRemarks.Key;
                var timedTransferRemarks = stopTimeWithRemarks.Value;

                // pro detekci shodných poznámek (občas tam projektant má dvě totožné návazné poznámky, to reportujeme jako chybu)
                var processedRemarks = new Dictionary<AswModel.Extended.Remark, AswModel.Extended.Remark>(new RemarkComparer()); // pro detekci duplicitních poznámek

                foreach (var remark in timedTransferRemarks)
                {
                    if (processedRemarks.ContainsKey(remark))
                    { 
                        // chyba projektanta, dvě shodné návazné poznámky, druhou nezpracováváme
                        log.Log(LogMessageType.WARNING_REMARK_DUPLICATE, $"{stopTime}: Duplicitní poznámky {processedRemarks[remark].Id} a {remark.Id}. Druhá bude ignorována.");
                        continue;
                    }
                    else
                    {
                        processedRemarks.Add(remark, remark);
                    }

                    var transfers = ProcessRemark(stopTime, remark);
                    foreach (var transfer in transfers)
                    {
                        if (allTransfers.ContainsKey(transfer))
                        {
                            // tohle možná ani nastat nemůže, těžko říct, protože dvě stejné poznámky vzniklé chybou projektanta bychom už zdetekovali dříve
                            // a proces zastavili - ale teoreticky se asi může stát leccos a pokud by vznikla jedna vazba dvakrát, zahlásíme to
                            transferLog.Log($"   *** přestup na {transfer.ToTrip} zast. {transfer.ToStop} už byl jednou vygenerován od {allTransfers[transfer]}.");
                        }
                        else if (transfer.MaxWaitingTimeSeconds == 0)
                        {
                            transferLog.Log($"   *** vyčkávací čas nastaven na 0, ignoruji - přestup na {transfer.ToTrip} zast. {transfer.ToStop}.");
                        }
                        else
                        {
                            allTransfers.Add(transfer, stopTime);
                        }
                    }
                }
            }

            return allTransfers.Keys;
        }

        // najde spoje, které odpovídají poznámce v zastavení a vrátí příslušné přestupní záznamy
        // (typicky by měl být jeden); zároveň masivně loguje, co vše našel
        private IEnumerable<TimedTransfer> ProcessRemark(StopTime waitingStopTime, AswModel.Extended.Remark remark)
        {
            var waitingTrip = waitingStopTime.Trip;
            StopVariantsMapping toStop = null; // najdeme první použitou verzi zastávky (pro ni budeme mít transformaci)
            foreach (var stopVer in remark.ToStop.AllVersions())
            {
                toStop = stopsTransformation.GetValueOrDefault(stopVer);
                if (toStop != null) break;
            }

            if (toStop == null)
            {
                transferLog.Log($"CHYBA: Zastávka {remark.ToStop.FirstVersion()} nebyla zpracována (jezdí tam něco?)");
                yield break;
            }

            if (!toStop.EqualsToAnyVariant(waitingStopTime.Stop))
            {
                // TODO log
                //transferLog.Log($"CHYBA: Zastavení {waitingStopTime} má vyčkávací poznámku, která odkazuje na jinou zastávku {remark.ToStop}");
                yield break;
            }

            var waitingTripServiceBitmap = waitingTrip.CalendarRecord.AsServiceBitmap(globalStartDate);
            var fromRoutes = gtfsRoutesByAswId.GetValueOrDefault(remark.FromRouteLineNumber); // kvůli verzím může jedno číslo odkazovat na víc reálných linek
            if (fromRoutes == null || !fromRoutes.Any())
            {
                // TODO nechceme to psát také spíše do transferLogu?
                log.Log(LogMessageType.WARNING_REMARK_MISSING_LINE, $"Nelze vyhodnotit přestupní vazbu, linka {remark.FromRouteLineNumber} nebyla nalezena");
                yield break;
            }

            // druh dopravy - měl by být ve všech verzích stejný, takže vezmeme první a zčeknem to
            var trafficType = fromRoutes.First().Type;
            if (fromRoutes.Any(routeVer => routeVer.Type != trafficType))
            {
                log.Log(LogMessageType.WARNING_REMARK_MISSING_LINE_VERSION, $"Při vyhodnocování přestupní vazby linka {remark.FromRouteLineNumber} má pro různé kalendáře odlišný typ.");
            }

            // minimální čas mezi příjezdem spoje a odjezdem navazujícího spoje
            var minTransferTime = remark.MinimumTransferTimeSeconds;
            if (trafficType == TrafficType.Rail && minTransferTime == 0)
                minTransferTime = 120; // u přestupů z vlaků musí být aspoň 2 minuty rezerva TODO vážně dvě?
            transferLog.Log($"{waitingStopTime} cal {waitingTripServiceBitmap} ve směru {waitingTrip.Headsign} navazuje na linku {remark.FromRouteLineNumber} v {remark.FromStop} ze směru {remark.FromRouteStopDirection} ({minTransferTime} sekund na přestup)");

            var maxArrivalTime = waitingStopTime.DepartureTime.AddSeconds(-minTransferTime);
            var otherStopTimes = FindTripOnRouteBefore(remark, fromRoutes.SelectMany(r => r.Trips), maxArrivalTime, waitingTripServiceBitmap, 15).ToArray();

            if (!otherStopTimes.Any())
            {
                // nenašly se žádné spoje, na které bychom mohli navazovat, prozkoumáme tedy ještě pro účely logování i 5 minut navíc a až hodinu zpět
                var hourStopTimes = FindTripOnRouteBefore(remark, fromRoutes.SelectMany(r => r.Trips), waitingStopTime.DepartureTime.AddMinutes(5), waitingTripServiceBitmap, 60);
                if (!hourStopTimes.Any())
                {
                    transferLog.Log($"   XXX žádný návazný spoj více než hodinu zpět nenalezen");
                }
                else
                {
                    foreach (var otherStopTime in hourStopTimes)
                    {
                        if (otherStopTime.ArrivalTime > maxArrivalTime)
                            transferLog.Log($"   X {TransferToString(otherStopTime, waitingStopTime.DepartureTime)} - ujede");
                        else
                            transferLog.Log($"   X {TransferToString(otherStopTime, waitingStopTime.DepartureTime)} - příliš brzy, mělo by být max 15 minut");
                    }
                }
            }

            foreach (var otherStopTime in otherStopTimes)
            {
                var otherTrip = otherStopTime.Trip;

                // vazby dlouhé 10-15 minut nezaznamenáváme do transfers.txt, pouze do logu (třeba pro projektanty)
                // TODO upravit a zaznamenávat do GTFS vše
                bool addToTransfers = (waitingStopTime.DepartureTime - otherStopTime.ArrivalTime <= 600);

                if (addToTransfers)
                {
                    transferLog.Log($"  - {TransferToString(otherStopTime, waitingStopTime.DepartureTime)}");

                    var transfer = new TimedTransfer()
                    {
                        FromStop = otherStopTime.Stop,
                        ToStop = waitingStopTime.Stop,
                        FromTrip = otherTrip,
                        ToTrip = waitingTrip,
                        MaxWaitingTimeSeconds = remark.MaximumWaitingTimeSeconds,
                    };

                    yield return transfer;
                }
                else
                {
                    transferLog.Log($"  ^ {TransferToString(otherStopTime, waitingStopTime.DepartureTime)} - neukládá se do GTFS, více než 10 minut");
                }
            }
        }

        // najde v seznamu 'trips' spoj pro přestupní vazbu dle 'remark', přijíždějící nejpozději v 'maxArrivalTime' a nejdříve v 'maxArrivalTime' - 'maxMinutesWaiting'
        // a fungující v alespoň některý ze dnů v uvedeném kalendáři
        private IEnumerable<StopTime> FindTripOnRouteBefore(AswModel.Extended.Remark remark, IEnumerable<Trip> trips, Time maxArrivalTime, ServiceDaysBitmap departureCalendar, int maxMinutesWaiting)
        {
            var minArrivalTime = maxArrivalTime.AddMinutes(-maxMinutesWaiting);
            
            // jen spoje, které jedou alespoň v jeden stejný den jako spoj, na který navazujeme
            trips = trips.Where(trip => !trip.CalendarRecord.AsServiceBitmap(globalStartDate).Intersect(departureCalendar).IsEmpty);

            // průjezdy zastávkou, kde dochází k návaznosti
            var stopTimes = trips.Select(trip => FindStopTimeOnTrip(trip, remark.FromStop, remark.FromRouteStopDirection)).Where(st => st != null).ToArray();

            // příjezdy jen omezený čas před maxArrivalTime
            stopTimes = stopTimes.Where(st => st.ArrivalTime <= maxArrivalTime && st.ArrivalTime >= minArrivalTime).ToArray();

            return stopTimes;
        }

        // najde první zastavení v zastávce 'stopAt', které je ale až po zastavení v 'fromDirection'
        private StopTime FindStopTimeOnTrip(Trip trip, AswModel.Extended.StopRef stopAt, AswModel.Extended.StopRef fromDirection)
        {
            bool directionConfirmed = false;
            foreach (var stopTime in trip.StopTimes)
            {
                if (!directionConfirmed && stopTime.Stop.AswNodeId == fromDirection.NodeId) // podle informací od CHAPSu se porovnává pouze podle čísla uzlu (na č. sloupku nezáleží)
                {
                    directionConfirmed = true;
                }
                else if (directionConfirmed && stopTime.Stop.AswNodeId == stopAt.NodeId 
                    && (stopTime.Stop.AswStopId == stopAt.StopId || (stopTime.Stop.AswStopId > 300 && stopAt.StopId > 300))) // čísla zastávek se musí rovnat, jen u vlaků na to kašlem
                {
                    return stopTime;
                }
            }

            return null;
        }

        private string TransferToString(StopTime otherStopTime, Time departureTime)
        {
            var otherTrip = otherStopTime.Trip;
            int nMins = (departureTime - otherStopTime.ArrivalTime) / 60;
            return $"na {otherStopTime.Trip} zast. {otherStopTime.Stop} příj. {otherStopTime.ArrivalTime} cal {otherStopTime.Trip.CalendarRecord.AsServiceBitmap(globalStartDate)} ze směru {otherTrip.PublicStopTimes.First().Stop.Name} (přestup {nMins} minut).";
        }
    }
}
