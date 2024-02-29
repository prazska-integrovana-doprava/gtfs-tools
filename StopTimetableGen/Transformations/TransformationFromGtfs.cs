using CommonLibrary;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using StopTimetableGen.StopTimetableModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.Transformations
{
    /// <summary>
    /// Zajišťuje převod z GTFS do StopTimetableModelu
    /// </summary>
    class TransformationFromGtfs
    {
        private class WeekdayName
        {
            public string Name { get; set; }
            public string TimetableSymbols { get; set; }
        }

        private Feed gtfsFeed;
        private Route selectedRoute;
        private DateTime startDate;
        private DateTime endDate;
        private IList<WeekdaySubset> weekdaySubsets;
        private string[] ignoredStops;
        private bool benevolentDays;
        private bool ignoreWheelchairAccessibility;

        private Dictionary<WeekdaySubset, WeekdayName> weekdaySubsetNames = new Dictionary<WeekdaySubset, WeekdayName>
        {
            { WeekdaySubset.AllDays, new WeekdayName { Name = "Celý týden", TimetableSymbols = "" } },
            { WeekdaySubset.Saturdays, new WeekdayName { Name = "Sobota", TimetableSymbols = "" } },
            { WeekdaySubset.Sundays, new WeekdayName { Name = "Neděle", TimetableSymbols = "" } },
            { WeekdaySubset.Weekends, new WeekdayName { Name = "Víkend a svátek", TimetableSymbols = "" } },
            { WeekdaySubset.Workdays, new WeekdayName { Name = "Pracovní den", TimetableSymbols = "" } },
        };

        public TransformationFromGtfs(Feed gtfsFeed, string selectedRoute, DateTime startDate, DateTime endDate, IList<WeekdaySubset> weekdaySubsets, 
            string[] ignoredStops, bool benevolentDays, bool ignoreWheelchairAccessibility)
        {
            this.gtfsFeed = gtfsFeed;
            this.selectedRoute = gtfsFeed.Routes[selectedRoute];
            this.startDate = startDate.Date;
            this.endDate = endDate.Date;
            this.weekdaySubsets = weekdaySubsets;
            this.ignoredStops = ignoredStops;
            this.benevolentDays = benevolentDays;
            this.ignoreWheelchairAccessibility = ignoreWheelchairAccessibility;
        }

        /// <summary>
        /// Převede data z GTFS do StopTimetableModelu.
        /// </summary>
        /// <returns>Soubor zastávkových jízdních řádů linky</returns>
        public LineTimetables PerformExport()
        {
            var agency = selectedRoute.SubAgencies.FirstOrDefault();
            var result = new LineTimetables()
            {
                LineId = selectedRoute.AswId,
                LineNumber = selectedRoute.ShortName,
                ValidFrom = startDate.Date,
                OperatorId = (agency?.SubAgencyId).GetValueOrDefault(),
                OperatorName = agency?.SubAgencyName,
            };

            var firstDirection = PerformExportForDirection(Direction.Outbound, result);
            result.StopTimetables.AddRange(firstDirection);

            var secondDirection = PerformExportForDirection(Direction.Inbound, result);
            result.StopTimetables.AddRange(secondDirection);

            return result;
        }

        // direction = 0 pro export spojů směr 0, direction = 1 pro export spojů směr 1
        private IEnumerable<StopTimetable> PerformExportForDirection(Direction direction, LineTimetables ownerLineInfo)
        {
            // TODO document
            var tripList = selectedRoute.Trips.Where(trip => trip.DirectionId == direction && SatisfiesPeriodLimit(trip.CalendarRecord)).ToList();
            var allStopTimes = tripList.SelectMany(trip => trip.PublicStopTimes).ToArray();
            var relevantStopsOrdered = new StopOrdering(tripList, new string[0]).ExtractStopsAndOrderTopologically().Where(s => s.AswNodeId != 0).ToArray();
            
            foreach (var stop in relevantStopsOrdered)
            {
                // všechny spoje přes tuto zastávku
                var tripsPassingThisStop = tripList.Where(trip => trip.PublicStopTimes.Any(st => st.Stop == stop));

                // všechny odjezdy ze zastávky (spoje, které zde končí nebo zastavují jen pro výstup, tu nejsou reprezentovány)
                var departureTimes = DepartureTrip.CreateAndMergeDepartures(allStopTimes.Where(st => st.Stop == stop && !st.IsLastPublicStop && st.PickupType != PickupType.None), ignoreWheelchairAccessibility).ToArray();

                // rozškatulkování odjezdů po hodinách
                var departuresByHours = departureTimes.GroupBy(st => st.DepartureTime.Hours % 24).ToDictionary(g => g.Key, g => g);

                // seznam zastávek společný pro všechny spoje / pokud je toto jedna z ignorovaných zastávek, zobrazujeme všechny
                var stopsOrdered = new StopOrdering(tripsPassingThisStop, ignoredStops.Contains(stop.Name) ? new string[0] : ignoredStops).ExtractStopsAndOrderTopologically().ToList();

                var stopTimetable = new StopTimetable()
                {
                    OwnerLine = ownerLineInfo,
                    Direction = (int)direction,
                    FirstHour = 3,
                    LastHour = 26,
                    Stop = stop,
                    Stops = CreateStopOnLinesList(stop, stopsOrdered, tripList).ToList(),
                };

                stopTimetable.Stops.First().Flags |= StopOnLine.StopFlags.FirstStop;
                stopTimetable.Stops.Last().Flags |= StopOnLine.StopFlags.LastStop;

                var remarkAssignment = new TripVariantsSort().GetTripRemarkAssignment(departureTimes, stopTimetable.Stops);
                var timeRemarksChecker = new TimeRemarksChecker(startDate, endDate);

                // přes provozní dny
                foreach (var weekdaySubset in weekdaySubsets)
                {
                    bool anyDeparture = false;

                    var timetableDay = new StopTimetableDay()
                    {
                        Title = weekdaySubsetNames[weekdaySubset].Name,
                        TitleSymbols = weekdaySubsetNames[weekdaySubset].TimetableSymbols,
                    };

                    // přes hodiny
                    for (int i = stopTimetable.FirstHour; i <= stopTimetable.LastHour; i++)
                    {
                        var hour = i % 24;
                        if (!departuresByHours.ContainsKey(hour))
                        {
                            continue; // nic v tuto hodinu nejede
                        }

                        // přes odjezdy v dané hodině
                        foreach (var departure in departuresByHours[hour].OrderBy(dep => dep.DepartureTime.ModuloDay()))
                        {
                            if (!timeRemarksChecker.SatisfiesWeekdays(departure.Calendar, weekdaySubset, benevolentDays))
                                continue; // v dané provozní dny nejede

                            anyDeparture = true;
                            var departureRecord = new Departure()
                            {
                                Minute = departure.DepartureTime.Minutes,
                                IsWheelchairAccessible = (departure.Trip.WheelchairAccessible == WheelchairAccessibility.Possible),
                            };

                            // poznámky, jednak provozní (nejede přes) a jednak časové (nejede v pátek)
                            var remark = remarkAssignment.GetValueOrDefault(departure);
                            var timeRemark = timeRemarksChecker.GetTimeRemark(departure, weekdaySubset);
                            AddRemarkIfNotNull(stopTimetable, departureRecord, remark);
                            AddRemarkIfNotNull(stopTimetable, departureRecord, timeRemark);
                            timetableDay.Hours[hour].Add(departureRecord);
                        }
                    }

                    if (anyDeparture)
                        stopTimetable.DayColumns.Add(timetableDay);
                }

                stopTimetable.Remarks.Insert(0, new SeparatorRemark()); // přidáme separátor a nad něj dáváme obecné poznámky, případně se zase vymaže
                if (stopTimetable.Stops.Any(s => s.OnRequestMode == StopOnLine.OnRequestStatus.OnRequest))
                {
                    stopTimetable.Remarks.Insert(0, new ManualRemark("x", "Zastávka na znamení"));
                }

                if (weekdaySubsets.Contains(WeekdaySubset.Saturdays) || weekdaySubsets.Contains(WeekdaySubset.Sundays))
                    stopTimetable.Remarks.Add(new SeparatorRemark());
                if (weekdaySubsets.Contains(WeekdaySubset.Saturdays))
                    stopTimetable.Remarks.Add(new ManualRemark("", StringHelper.GetSaturdayString(DaysOfWeekCalendars.TrainsInstance, startDate, endDate)));
                if (weekdaySubsets.Contains(WeekdaySubset.Sundays))
                    stopTimetable.Remarks.Add(new ManualRemark("", StringHelper.GetSundayString(DaysOfWeekCalendars.TrainsInstance, startDate, endDate)));

                if (stopTimetable.DayColumns.Any())
                    yield return stopTimetable;
            }
        }

        private bool SatisfiesPeriodLimit(CalendarRecord calendar)
        {
            if (calendar.StartDate > endDate || calendar.EndDate < startDate)
                return false;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (calendar.OperatesAt(date))
                {
                    return true;
                }
            }
            
            return false;
        }

        // neřeší First a Last stop (flagy je potřeba doplnit později ručně)
        private IEnumerable<StopOnLine> CreateStopOnLinesList(Stop srcStop, IEnumerable<Stop> stopsOnLine, IList<Trip> trips)
        {
            bool srcStopPassed = false;

            foreach (var s in stopsOnLine)
            {
                var result = new StopOnLine(s);

                // TODO potenciálně nepřesné - jednak u aktuální zastávky bychom měli brát formálně správně PickupType (ale výsledek by měl být stejný),
                // druhak bereme všechna zastavení, takže to nebude fungovat, když spoj staví vícekrát u stejného sloupku,
                // a třeťak nám stačí jeden spoj na znamení a už mám zastávku na znamení celou
                if (trips.SelectMany(t => t.GetStopTimesAt(s)).Count(st => st.DropOffType == DropOffType.DriverRequest) > trips.SelectMany(t => t.GetStopTimesAt(s)).Count(st => st.DropOffType == DropOffType.Regular))
                {
                    result.OnRequestMode = StopOnLine.OnRequestStatus.OnRequest;
                }

                if (trips.SelectMany(t => t.GetStopTimesAt(s)).Any(st => st.IsLastPublicStop))
                {
                    result.Flags |= StopOnLine.StopFlags.SomeTripsEndHere;
                }

                if (s == srcStop)
                {
                    result.StopClassification = StopOnLine.StopClass.CurrentStop;
                    srcStopPassed = true;
                }
                else if (!srcStopPassed)
                {
                    result.StopClassification = StopOnLine.StopClass.PastStop;
                }
                else
                {
                    result.StopClassification = StopOnLine.StopClass.FutureStop;
                    var travelTimes = trips.Select(t => GetTravelTimeBetweenStops(t, srcStop, s)).Where(tt => tt.HasValue).Select(tt => tt.Value);
                    result.TravelTimeMinutes = (int) Math.Round(travelTimes.Average() / 60);
                }

                yield return result;
            }
        }

        // vrací v sekundách
        private int? GetTravelTimeBetweenStops(Trip trip, Stop firstStop, Stop secondStop)
        {
            var firstStopTime = trip.GetStopTimesAt(firstStop).SingleOrDefault(); // předpoklad zastavení nejvýše jednou
            var secondStopTime = trip.GetStopTimesAt(secondStop).SingleOrDefault(); // předpoklad zastavení nejvýše jednou
            if (firstStopTime == null || secondStopTime == null)
            {
                return null;
            }

            var travelTime = secondStopTime.ArrivalTime - firstStopTime.DepartureTime;
            if (travelTime >= 0)
            {
                return travelTime;
            }
            else
            {
                return null;
            }
        }

        private void AddRemarkIfNotNull(StopTimetable stopTimetable, Departure departureRecord, IRemark remark)
        {
            if (remark != null)
            {
                departureRecord.Remarks.Add(remark);
                if (!stopTimetable.Remarks.Contains(remark))
                {
                    stopTimetable.Remarks.Add(remark);
                }
            }
        }

    }
}
