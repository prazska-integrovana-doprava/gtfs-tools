using CommonLibrary;
using GtfsLogging;
using JdfModel;
using JdfToGtfsProcessor.Calendars;
using JdfToGtfsProcessor.Stops;
using JdfToGtfsProcessor.Transfers;

namespace JdfToGtfsProcessor
{
    internal class JdfFeedProcessor
    {
        private StopDatabase stopDatabase;

        private RouteMapping routeMapping;

        private Dictionary<int, List<GtfsModel.Extended.Trip>> tripsByJdfRoute;

        private Dictionary<GtfsModel.Extended.StopTime, (List<TimedTransfer> timedTransfers, ISimpleLogger logger)> timedTransfersForStopTimes;

        private GtfsModel.Extended.Feed gtfsFeedEx;

        public JdfFeedProcessor(StopDatabase stopDatabase)
        {
            this.stopDatabase = stopDatabase;
            routeMapping = new RouteMapping();
            tripsByJdfRoute = new Dictionary<int, List<GtfsModel.Extended.Trip>>();
            timedTransfersForStopTimes = new Dictionary<GtfsModel.Extended.StopTime, (List<TimedTransfer>, ISimpleLogger)>();
            gtfsFeedEx = new GtfsModel.Extended.Feed();
        }

        public void InitFeed(FeedPublisher feedPublisher)
        {
            if (feedPublisher == FeedPublisher.KODIS)
            {
                gtfsFeedEx.Agency.Add("ODIS", new GtfsModel.GtfsAgency()
                {
                    Id = "ODIS",
                    Name = "ODIS",
                    Phone = "+420597608508",
                    Timezone = "Europe/Prague",
                    Url = "https://www.kodis.cz",
                    Lang = "cs",
                    Email = "info@kodis.cz"
                });

                gtfsFeedEx.FeedInfo = new GtfsModel.GtfsFeedInfo()
                {
                    PublisherName = "KODIS",
                    PublisherUrl = "https://kodis.cz",
                    Lang = "cs",
                    StartDate = DateTime.Now.Date,
                    EndDate = DateTime.Now.Date.AddDays(30),
                    ContactEmail = "info@kodis.cz",
                };
            }
            else
            {
                throw new ArgumentException("Unknown feed publisher " + feedPublisher);
            }
        }

        public void ProcessFeed(JdfFeed jdfFeed, ISimpleLogger log, ISimpleLogger missingPlatformCodeLog, ISimpleLogger routeLog, ISimpleLogger timedTransferLog, bool skipPastFeeds)
        {
            if (!jdfFeed.Routes.Any() || !jdfFeed.Stops.Any() || !jdfFeed.Trips.Any() || !jdfFeed.StopTimes.Any())
            {
                log.Log("Chybí potřebná data, dávka nenačtena");
                return;
            }

            var feedStartDate = jdfFeed.Routes.Values.Min(r => r.ValidFrom);
            var feedEndDate = jdfFeed.Routes.Values.Max(r => r.ValidTo);
            if (skipPastFeeds && feedEndDate < DateTime.Now.Date)
            {
                log.Log($"Ignoruji feed, je celý v minulosti (končí {feedEndDate})");
                return;
            }

            // zastávky
            stopDatabase.CreateStopDatabase(jdfFeed.Stops.Values, feedStartDate, log);

            // linky
            routeMapping.TransformJdfRoutesToGtfs(jdfFeed.Routes.Values, jdfFeed.RoutesExtendedData, gtfsFeedEx.Agency["ODIS"], jdfFeed.Agencies, jdfFeed.AlternativeAgencies, routeLog);

            foreach (var route in jdfFeed.Routes.Values)
            {
                // pokud je tahle linka už v dříve načteném feedu, tak ji v rámci své platnosti přepisuje, zkrátíme tedy platnosti spojů
                var tripsInDb = tripsByJdfRoute.GetValueOrDefault(route.RouteId);
                if (tripsInDb != null)
                {
                    foreach (var trip in tripsInDb.ToArray()) // to array je tam, abychom si vyrobili kopii, protože z původní kolekce budeme mazat
                    {
                        trip.CalendarRecord.ShortenBy(route.ValidFrom, route.ValidTo);
                        if (trip.CalendarRecord.IsEmpty)
                        {
                            tripsByJdfRoute[route.RouteId].Remove(trip);
                            foreach (var stopTime in trip.StopTimes)
                            {
                                timedTransfersForStopTimes.Remove(stopTime);
                            }
                        }
                    }
                }
            }

            var fixedCodesProcessor = new FixedCodesCalendarProcessor(log, jdfFeed.FixedCodes);
            var isWheelchairAccessibleFixedCode = jdfFeed.FixedCodes.Values.FirstOrDefault(fc => fc.CodeChar == FixedCodes.AccessibleVehicle);

            // spoje
            var trips = new Dictionary<(int route, int trip), GtfsModel.Extended.Trip>();
            var ignoredTripsDueToError = new HashSet<(int route, int trip)>();
            foreach (var trip in jdfFeed.Trips)
            {
                var route = jdfFeed.Routes.GetValueOrDefault(trip.RouteId);
                if (route == null)
                {
                    log.Log($"Linka {trip.RouteId} spoje {trip} nebyla nalezena. Spoj bude ignorován");
                    ignoredTripsDueToError.Add((trip.RouteId, trip.TripNumber));
                    continue;
                }

                GtfsModel.Extended.BaseCalendarRecord calendar;

                var tripTimeRemarks = jdfFeed.TimeRemarks.Where(tr => tr.RouteId == trip.RouteId && tr.TripNumber == trip.TripNumber);
                var timeRemarksProcessor = new TimeRemarksCalendarProcessor(trip, tripTimeRemarks, log);
                timeRemarksProcessor.CorrectTimeRemarks(route.ValidFrom, route.ValidTo);
                if (timeRemarksProcessor.HasRemarksOfType(TimeRemarkTypes.OperatesOnly))
                {
                    calendar = timeRemarksProcessor.ProcessOperatesOnlyRemarks();
                }
                else
                {
                    var calendarRecordFromFixedCodes = fixedCodesProcessor.CreateCalendarFromFixedCodes(trip, route);
                    calendar = timeRemarksProcessor.ProcessOperatesOnRemarks(calendarRecordFromFixedCodes);

                    // TODO ty poznámky nejsou úplně jasný

                    var operatesAlsoRemarks = tripTimeRemarks.Where(tr => tr.TimeRemarkType == TimeRemarkTypes.OperatesAlso);
                    foreach (var remark in operatesAlsoRemarks)
                    {
                        if (remark.DateFrom.HasValue)
                        {
                            calendar.AddException(remark.DateFrom.Value, GtfsModel.Enumerations.CalendarExceptionType.Add);

                            if (remark.DateTo.HasValue)
                            {
                                log.Log($"Spoj {trip} má poznámku {remark} \"jede také\" s vyplněným druhým datumem, což je v rozporu se specifikací JDF. Používám pouze první datum.");
                            }
                        }
                        else
                        {
                            log.Log($"Spoj {trip} má poznámku {remark} \"jede také\" bez vyplněného data. Ignoruji.");
                        }
                    }

                    var doesNotOperateOnRemarks = tripTimeRemarks.Where(tr => tr.TimeRemarkType == TimeRemarkTypes.DoesNotOperateOn).ToArray();
                    foreach (var doesNotOperateOnRemark in doesNotOperateOnRemarks)
                    {
                        if (doesNotOperateOnRemark.DateFrom.HasValue)
                        {
                            calendar.ShortenBy(doesNotOperateOnRemark.DateFrom.Value, doesNotOperateOnRemark.DateTo ?? doesNotOperateOnRemark.DateFrom.Value);
                        }
                        else
                        {
                            log.Log($"Spoj {trip} má poznámku {doesNotOperateOnRemark} \"nejede\" bez vyplněného data. Ignoruji.");
                        }
                    }

                    // TODO umět zpracovat
                    var unprocessedTimeRemarks = tripTimeRemarks.Where(tr => tr.TimeRemarkType == TimeRemarkTypes.OperatesOnEvenWeekdaysOnly || tr.TimeRemarkType == TimeRemarkTypes.OperatesOnOddWeekdaysOnly
                        || tr.TimeRemarkType == TimeRemarkTypes.OperatesOnEvenWeekdaysOnlyBetween || tr.TimeRemarkType == TimeRemarkTypes.OperatesOnOddWeekdaysOnlyBetween);
                    foreach (var remark in unprocessedTimeRemarks)
                    {
                        log.Log($"Spoj {trip} má poznámku {remark} typu {remark.TimeRemarkType}, který nezpracováváme. Poznámka je ignorována, kalendář bude nesprávný.");
                        throw new NotImplementedException();
                    }
                }

                if (calendar.IsEmpty)
                {
                    //log.Log($"Kalendář spoje {trip} je prázdná množina dnů, spoj bude ignorován.");
                    ignoredTripsDueToError.Add((trip.RouteId, trip.TripNumber));
                    continue;
                }

                var gtfsRoute = routeMapping.JdfRoutesToGtfs?.GetValueOrDefault(trip.RouteId); //jdfRoutesToGtfs musí být inicializované, protože jsme volali routeMapping.TransformJdfRoutesToGtfs a zároveň pro každou licenční linku tam musí být záznam, takže výsledek by neměl být nikdy null
                if (gtfsRoute == null)
                {
                    log.Log($"Nedohledána GTFS linka {trip.RouteId} pro spoj {trip}. Ignoruji spoj.");
                    ignoredTripsDueToError.Add((trip.RouteId, trip.TripNumber));
                    continue;
                }

                GtfsModel.RouteSubAgency? subAgency;
                var altAgencies = jdfFeed.AlternativeAgencies.Where(aa => aa.RouteNumber == trip.RouteId && (aa.TripNumber == 0 || aa.TripNumber == trip.TripNumber));
                if (!altAgencies.Any()) 
                {
                    subAgency = gtfsRoute.SubAgencies.FirstOrDefault(sa => sa.LicenceNumber == trip.RouteId);
                }
                else
                {
                    if (altAgencies.Any2())
                    {
                        log.Log($"Spoj {trip} má v altdop.txt více záznamů pro nastavení dopravce. Pravděpodobně jsou rozlišeny fixed cody nebo datumovou platností, což aplikace v tuto chvíli nepodporuje. Bude použit první záznam.");
                    }

                    subAgency = gtfsRoute.SubAgencies.FirstOrDefault(sa => sa.SubAgencyId == altAgencies.First().AgencyId);
                }

                if (subAgency == null)
                {
                    log.Log($"Nedohledán dopravce (gtfs sub agency) pro spoj {trip}.");
                }

                var wheelchairAccessible = isWheelchairAccessibleFixedCode != null ? trip.FixedCodes.Any(fc => fc == isWheelchairAccessibleFixedCode.CodeId) : false;

                var gtfsTrip = new GtfsModel.Extended.Trip()
                {
                    CalendarRecord = calendar,
                    BikesAllowed = GtfsModel.Enumerations.BikeAccessibility.Possible,
                    DirectionId = trip.TripNumber % 2 == 0 ? GtfsModel.Enumerations.Direction.Inbound : GtfsModel.Enumerations.Direction.Outbound,
                    TripNumber = trip.TripNumber,
                    Route = gtfsRoute,
                    SubAgency = subAgency,
                    WheelchairAccessible = wheelchairAccessible ? GtfsModel.Enumerations.WheelchairAccessibility.Possible : GtfsModel.Enumerations.WheelchairAccessibility.NotPossible
                };

                trips.Add((trip.RouteId, trip.TripNumber), gtfsTrip);
                tripsByJdfRoute.GetValueAndAddIfMissing(trip.RouteId, new List<GtfsModel.Extended.Trip>()).Add(gtfsTrip);

            }

            // zastavení
            foreach (var stopTime in jdfFeed.StopTimes)
            {
                var routeStop = jdfFeed.RouteStops.FirstOrDefault(rs => rs.RouteId == stopTime.RouteId && rs.StopIndex == stopTime.StopIndex);
                if (routeStop == null)
                {
                    log.Log($"Zastavení {stopTime} nemá záznam v souboru Zaslinky.txt. Ignoruji záznam.");
                    continue;
                }

                if (jdfFeed.Stops.GetValueOrDefault(routeStop.StopId)?.NamePart3 == "CLO")
                {
                    // virtuální hraniční zastávka
                    continue;
                }

                var gtfsTrip = trips.GetValueOrDefault((stopTime.RouteId, stopTime.TripNumber));
                if (gtfsTrip == null)
                {
                    if (!ignoredTripsDueToError.Contains((stopTime.RouteId, stopTime.TripNumber)))
                    {
                        log.Log($"Zastavení {stopTime} - nenalezen GTFS trip (chybí ve Spoje.txt, nebo nebyl vygenerován kvůli nějaké chybě). Ignoruji záznam.");
                    }

                    continue;
                }

                var departureTime = ParseJdfTime(stopTime.DepartureTime);
                var arrivalTime = ParseJdfTime(stopTime.ArrivalTime);
                if (!departureTime.HasValue && !arrivalTime.HasValue)
                    continue;

                var fixedCodeChars = routeStop.FixedCodes.Union(stopTime.FixedCodes).Select(fc => jdfFeed.FixedCodes[fc]?.CodeChar).ToArray();
                bool isRequestStop = fixedCodeChars.Contains(FixedCodes.StopOnRequest);
                bool isBoardingOnly = fixedCodeChars.Contains(FixedCodes.BoardingOnly);
                bool isAlightingOnly = fixedCodeChars.Contains(FixedCodes.AlightingOnly);

                var stopTimesForTrip = gtfsTrip.StopTimes;
                var previousStopTime = stopTimesForTrip.LastOrDefault();
                if (arrivalTime < new Time(11, 0, 0) && previousStopTime?.DepartureTime > new Time(13, 0, 0))
                {
                    arrivalTime = arrivalTime.Value.AddDay();
                }

                if (departureTime < new Time(11, 0, 0) && (arrivalTime > new Time(13, 0, 0) || previousStopTime?.DepartureTime > new Time(13, 0, 0)))
                {
                    departureTime = departureTime.Value.AddDay();
                }

                var stopId = stopTime.StopId.ToString() + "_" + stopTime.PlatformCode;
                var gtfsStop = stopDatabase.GetStopForStopTime(stopTime, OrderZonesByNumericalValue(routeStop.FareZone), missingPlatformCodeLog);
                if (gtfsStop == null)
                {
                    if (!stopDatabase.IgnoredStopsDueToError.Contains(stopTime.StopId))
                    {
                        log.Log($"Zastavení {stopTime} - nenalezena GTFS zastávka {stopTime.StopId} (chybí v Zastavky.txt nebo v ZAST_ODIS.xml nebo nebyla vygenerována kvůli nějaké chybě). Ignoruji záznam.");
                    }

                    continue;
                }

                var gtfsStopTime = new GtfsModel.Extended.StopTime()
                {
                    ArrivalTime = arrivalTime ?? departureTime!.Value, // výše máme podmínku, že alespoň jeden z dvojice departureTime a arrivalTime nesmí být null
                    BikesAllowed = GtfsModel.Enumerations.BikeAccessibility.Possible,
                    DepartureTime = departureTime ?? arrivalTime!.Value, // výše máme podmínku, že alespoň jeden z dvojice departureTime a arrivalTime nesmí být null
                    DropOffType = isBoardingOnly ? GtfsModel.Enumerations.DropOffType.None : isRequestStop ? GtfsModel.Enumerations.DropOffType.DriverRequest : GtfsModel.Enumerations.DropOffType.Regular,
                    PickupType = isAlightingOnly ? GtfsModel.Enumerations.PickupType.None : isRequestStop ? GtfsModel.Enumerations.PickupType.DriverRequest : GtfsModel.Enumerations.PickupType.Regular,
                    SequenceNumber = (previousStopTime?.SequenceNumber ?? 0) + 1,
                    FareKilometerDistance = stopTime.DistanceFromStartKm,
                    Trip = gtfsTrip,
                    Stop = gtfsStop,
                };

                stopTimesForTrip.Add(gtfsStopTime);

                var timedTransfers = jdfFeed.TimeTransfer.Where(tt => tt.RouteId == stopTime.RouteId && tt.TripNumber == stopTime.TripNumber && tt.StopIndex == stopTime.StopIndex).ToList();
                if (timedTransfers.Any())
                {
                    timedTransfersForStopTimes.Add(gtfsStopTime, (timedTransfers, timedTransferLog));
                }
            }
        }

        public GtfsModel.Extended.Feed GetResultGtfsFeed(ISimpleLogger log)
        {
            gtfsFeedEx.Routes = routeMapping.GtfsRoutes;
            foreach (var route in tripsByJdfRoute)
            {
                foreach (var trip in route.Value)
                {
                    var tripId = route.Key + "_" + trip.TripNumber.ToString() + "_" + trip.CalendarRecord.GetFirstDayOfService()!.Value.ToString("yyyyMMdd"); // stále hlídáme, že kalendář je neprázdný
                    trip.GtfsId = tripId;
                    trip.Headsign = trip.StopTimes.LastOrDefault()?.Stop?.Name;
                    if (gtfsFeedEx.Trips.ContainsKey(tripId))
                    {
                        log.Log($"Spoj {tripId} už v databázi je jako {gtfsFeedEx.Trips[tripId]}. Asi jde o duplicitu.");
                        continue;
                    }

                    gtfsFeedEx.Trips.Add(tripId, trip);
                    trip.Route.Trips.Add(trip);
                }
            }

            var usedStops = gtfsFeedEx.Trips.Values.SelectMany(t => t.StopTimes).Select(st => st.Stop).Distinct().OrderBy(s => s.CisId);
            gtfsFeedEx.Stops = usedStops.Cast<GtfsModel.Extended.BaseStop>().ToDictionary(s => s.GtfsId);

            var calendarDatabase = new CalendarDatabase();
            foreach (var trip in gtfsFeedEx.Trips.Values)
            {
                trip.CalendarRecord = calendarDatabase.GetExistingOrAddNewCalendar(trip.CalendarRecord);
            }

            calendarDatabase.SetCalendarIds();
            gtfsFeedEx.Calendar = calendarDatabase.AllCalendars.ToDictionary(cal => cal.GtfsId);

            new TripBlockFromTransfersProcessor().Process(gtfsFeedEx, timedTransfersForStopTimes);

            return gtfsFeedEx;
        }

        private static Time? ParseJdfTime(string timeStr)
        {
            if (timeStr.Length >= 3 && int.TryParse(timeStr, out int timeVal))
            {
                var hrs = timeVal / 100;
                var mins = timeVal % 100;
                return new Time(hrs, mins, 0);
            }
            else
            {
                return null;
            }
        }

        public static string OrderZonesByNumericalValue(string zones)
        {
            if (string.IsNullOrWhiteSpace(zones))
                return string.Empty;

            var numbers = new List<int>();
            var nonNumbers = new List<string>();

            foreach (var part in zones.Split(','))
            {
                var trimmed = part.Trim();

                if (int.TryParse(trimmed, out int number))
                {
                    numbers.Add(number);
                }
                else if (!string.IsNullOrEmpty(trimmed))
                {
                    nonNumbers.Add(trimmed);
                }
            }

            var orderedNumbers = numbers.OrderBy(n => n)
                                        .Select(n => n.ToString());

            var orderedNonNumbers = nonNumbers.OrderBy(s => s, StringComparer.Ordinal);

            return string.Join(",", orderedNumbers.Concat(orderedNonNumbers));
        }

    }
}
