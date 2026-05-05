using GtfsLogging;
using JdfModel;
using System.Text.RegularExpressions;

namespace JdfToGtfsProcessor.Transfers
{
    internal class TripBlockFromTransfersProcessor
    {
        public void Process(GtfsModel.Extended.Feed feed, Dictionary<GtfsModel.Extended.StopTime, (List<TimedTransfer> timedTransfers, ISimpleLogger logger)> timedTransfersByStopTime) 
        {
            foreach (var stopTimeAndTransfers in timedTransfersByStopTime)
            {
                var stopTime = stopTimeAndTransfers.Key;
                var timedTransfers = stopTimeAndTransfers.Value.timedTransfers;
                var log = stopTimeAndTransfers.Value.logger;

                foreach (var remark in timedTransfers.Where(tt => tt.RemarkText.Contains("(bez přestupu)")))
                {
                    log.Log($"{stopTime.Trip.GtfsId}: {remark} -- v {stopTime.DepartureTime}");
                    var regex = new Regex(@"^na spoj \d+ navazuje v zastávce .+ spoj (\d{1,6}) linky (\d{1,6}) do .+\(bez přestupu\)");
                    var match = regex.Match(remark.RemarkText);
                    if (match.Success)
                    {
                        var trip = stopTime.Trip;
                        if (stopTime != trip.StopTimes.Last())
                        {
                            log.Log($"  - [WARN] Má poznámku o pokračování bez přestupu, ale {stopTime} není poslední zastávka spoje. Ignoruji záznam.");
                            continue;
                        }

                        var otherTripNumber = int.Parse(match.Groups[1].Value); //věříme, že je to int, když jsme matchovali na \d{1,6}
                        var otherRouteId = match.Groups[2].Value;
                        var otherRoute = feed.Routes.GetValueOrDefault(otherRouteId);
                        if (otherRoute == null && otherRouteId.Length <= 3 && stopTime.Trip.SubAgency != null)
                        {
                            var otherRouteIdCombined = int.Parse(otherRouteId); //věříme, že je to int, když jsme matchovali na \d{1,6}
                            otherRouteIdCombined += stopTime.Trip.SubAgency.LicenceNumber / 1000 * 1000;
                            log.Log($"  - [INFO] linka {otherRouteId} zadána jako třímístná, zkouším použít linku {otherRouteIdCombined}");
                            otherRouteId = otherRouteIdCombined.ToString();
                            otherRoute = feed.Routes.GetValueOrDefault(otherRouteId);
                        }

                        if (otherRoute != null)
                        {
                            var otherTrips = otherRoute.Trips.Where(t => t.TripNumber == otherTripNumber).ToArray();
                            if (!otherTrips.Any())
                            {
                                log.Log($"  - [WARN] Na lince {otherRouteId} nedohledán spoj {otherTripNumber}.");
                            }

                            foreach (var otherTrip in otherTrips)
                            {
                                if (!trip.CalendarRecord.IntersectDatesWith(otherTrip.CalendarRecord).Any())
                                {
                                    log.Log($"  - [INFO] Návazný spoj {otherTrip.GtfsId} - prázdný průnik kalendáře, přeskakuji.");
                                    continue;
                                }

                                if (otherTrip.StopTimes.First().Stop.CisId != stopTime.Stop.CisId)
                                {
                                    log.Log($"  - [WARN] Návazný spoj {otherTrip.GtfsId} má první zastávku {otherTrip.StopTimes.First()}, která se liší CIS číslem od poslední zastávky předchozího spoje. Spoje nebudou propojeny.");
                                    continue;
                                }

                                if (otherTrip.StopTimes.First().DepartureTime < stopTime.ArrivalTime)
                                {
                                    log.Log($"  - [WARN] Návazný spoj {otherTrip.GtfsId} odjzíždí z první zastávky v {otherTrip.StopTimes.First().DepartureTime}, což je dříve než příjezd předchozího spoje. Spoje nebudou propojeny.");
                                    continue;
                                }

                                trip.BlockId = trip.GtfsId;
                                otherTrip.BlockId = trip.GtfsId;
                                log.Log($"  - [INFO] Návazný spoj {otherTrip.GtfsId}, block ID = {trip.GtfsId}");
                            }
                        }
                        else
                        {

                            log.Log($"  - [WARN] linka {otherRouteId} nenalezena. Ignoruji záznam.");
                        }
                    }
                    else 
                    {
                        log.Log($"  - [WARN] Poznámka obsahuje text \"(bez přestupu)\", ale nemá správný formát. Nebude zpracována.");
                    }
                }
            }
        }
    }
}
