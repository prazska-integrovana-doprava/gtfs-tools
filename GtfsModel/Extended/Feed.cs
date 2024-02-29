using CommonLibrary;
using GtfsModel.Enumerations;
using GtfsModel.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Nadstavba nad <see cref="GtfsFeed"/>.
    /// </summary>
    public class Feed
    {
        /// <summary>
        /// Pro metodu <see cref="MergeWith"/> slučující dva feedy
        /// </summary>
        public enum MergeDuplicityRule
        {
            AllowDuplicityTakeOriginal,
            AllowDuplicityTakeNew,
            DisallowDuplicity,
        }

        public Dictionary<int, GtfsAgency> Agency { get; set; }

        public Dictionary<string, BaseStop> Stops { get; set; }

        public Dictionary<string, Route> Routes { get; set; }

        public Dictionary<string, Trip> Trips { get; set; }
        
        public Dictionary<string, CalendarRecord> Calendar { get; set; }

        public Dictionary<string, Shape> Shapes { get; set; }

        public List<BaseTransfer> Transfers { get; set; }

        public GtfsFeedInfo FeedInfo { get; set; }

        public Feed()
        {
            Agency = new Dictionary<int, GtfsAgency>();
            Stops = new Dictionary<string, BaseStop>();
            Routes = new Dictionary<string, Route>();
            Trips = new Dictionary<string, Trip>();
            Calendar = new Dictionary<string, CalendarRecord>();
            Shapes = new Dictionary<string, Shape>();
            Transfers = new List<BaseTransfer>();
        }

        public static Feed Construct(string gtfsFolder)
        {
            var gtfsFeed = GtfsFeedSerializer.DeserializeFeed(gtfsFolder);
            return Construct(gtfsFeed);
        }

        public static Feed Construct(GtfsFeed gtfsFeed)
        {
            var result = new Feed();

            // dopravci
            foreach (var agency in gtfsFeed.Agency)
            {
                result.Agency.Add(agency.Id, agency);
            }

            // nejdříve stanice, protože na ně pak odkazují ostatní body
            foreach (var station in gtfsFeed.Stops.Where(s => s.LocationType == LocationType.Station))
            {
                result.Stops.Add(station.Id, BaseStop.Construct(station, null));
            }

            foreach (var gtfsStop in gtfsFeed.Stops.Where(s => s.LocationType != LocationType.Station))
            {
                var stop = BaseStop.Construct(gtfsStop, result.Stops);
                result.Stops.Add(gtfsStop.Id, stop);
                (stop as StationEntrance)?.ParentStation?.EntrancesToStation.Add(stop as StationEntrance);
                (stop as Stop)?.ParentStation?.StopsInStation.Add(stop as Stop);
            }

            // kalendáře
            foreach (var calendar in gtfsFeed.Calendar)
            {
                result.Calendar.Add(calendar.Id, CalendarRecord.Construct(calendar));
            }

            foreach (var gtfsCalendarDate in gtfsFeed.CalendarDates)
            {
                var calendarDate = CalendarExceptionRecord.Construct(gtfsCalendarDate);
                result.Calendar[gtfsCalendarDate.ServiceId].Exceptions.Add(calendarDate.Date, calendarDate);
            }

            // shapes
            foreach (var gtfsShapePoint in gtfsFeed.Shapes)
            {
                var shapePoint = ShapePoint.Construct(gtfsShapePoint);
                var shape = result.Shapes.GetValueAndAddIfMissing(gtfsShapePoint.ShapeId, new Shape() { Id = gtfsShapePoint.ShapeId });
                shape.Points.Add(shapePoint);
            }

            // body by měly být v souboru seřazené, ale pro jistotu
            foreach (var shape in result.Shapes)
            {
                shape.Value.Points.Sort((x, y) => x.SequenceIndex.CompareTo(y.SequenceIndex));
            }
            
            // linky
            foreach (var route in gtfsFeed.Routes)
            {
                result.Routes.Add(route.Id, Route.Construct(route, gtfsFeed.RouteSubAgencies));
            }

            // spoje
            var blocks = new Dictionary<string, List<Trip>>();
            foreach (var gtfsTrip in gtfsFeed.Trips)
            {
                var trip = Trip.Construct(gtfsTrip, result.Calendar, result.Routes, result.Shapes);
                result.Trips.Add(trip.GtfsId, trip);
                trip.Route.Trips.Add(trip);
                if (trip.Shape != null)
                    trip.Shape.TripsOfThisShape.Add(trip);
                if (!string.IsNullOrEmpty(trip.BlockId))
                {
                    var tripsInBlock = blocks.GetValueAndAddIfMissing(trip.BlockId, new List<Trip>());
                    tripsInBlock.Add(trip);
                }
            }

            // stoptimes
            foreach (var gtfsStopTime in gtfsFeed.StopTimes)
            {
                var stopTime = StopTime.Construct(gtfsStopTime, result.Trips, result.Stops);
                stopTime.Trip.StopTimes.Add(stopTime);
            }

            // stoptimes by měly být v souboru již seřazené, ale pro jistotu
            foreach (var trip in result.Trips)
            {
                trip.Value.StopTimes.Sort((x, y) => x.SequenceNumber.CompareTo(y.SequenceNumber));
            }

            // propojení spojů v bloku
            foreach (var block in blocks)
            {
                var tripsOrdered = block.Value.OrderBy(trip => trip.StopTimes.First().DepartureTime).ToArray();
                for (int i = 0; i < tripsOrdered.Length - 1; i++)
                {
                    tripsOrdered[i].NextTripInBlock = tripsOrdered[i + 1];
                    tripsOrdered[i + 1].PreviousTripInBlock = tripsOrdered[i];
                }
            }

            // přestupy
            foreach (var transfer in gtfsFeed.Transfers)
            {
                result.Transfers.Add(BaseTransfer.Construct(transfer, result.Stops, result.Trips));
            }

            result.FeedInfo = gtfsFeed.FeedInfo.FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Přidá do tohoto feedu data z jiného feedu
        /// </summary>
        /// <param name="feed">Jiný feed</param>
        public void MergeWith(GtfsFeed feed, MergeDuplicityRule agencyMergeRule, MergeDuplicityRule stopsMergeRule, MergeDuplicityRule routesMergeRule,
            MergeDuplicityRule tripsMergeRule, MergeDuplicityRule calendarMergeRule, MergeDuplicityRule shapesMergeRule)
        {
            var otherFeedEx = Construct(feed);
            MergeWith(otherFeedEx, agencyMergeRule, stopsMergeRule, routesMergeRule, tripsMergeRule, calendarMergeRule, shapesMergeRule);
        }

        /// <summary>
        /// Přidá do tohoto feedu data z jiného feedu
        /// </summary>
        /// <param name="feed">Jiný feed</param>
        public void MergeWith(Feed feed, MergeDuplicityRule agencyMergeRule, MergeDuplicityRule stopsMergeRule, MergeDuplicityRule routesMergeRule,
            MergeDuplicityRule tripsMergeRule, MergeDuplicityRule calendarMergeRule, MergeDuplicityRule shapesMergeRule)
        {
            MergeIntoDictionary(Agency, feed.Agency, agencyMergeRule);
            MergeIntoDictionary(Stops, feed.Stops, stopsMergeRule);
            MergeIntoDictionary(Routes, feed.Routes, routesMergeRule);
            MergeIntoDictionary(Trips, feed.Trips, tripsMergeRule);
            MergeIntoDictionary(Calendar, feed.Calendar, calendarMergeRule);
            MergeIntoDictionary(Shapes, feed.Shapes, shapesMergeRule);
            Transfers.AddRange(feed.Transfers);
        }

        // vrací položky, které byly duplicitní (podle nastavení buď přepsaly původní položky, nebo byly ignorovány)
        private IEnumerable<KeyValuePair<TKey, TValue>> MergeIntoDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue> otherDictionary,
            MergeDuplicityRule duplicityRule = MergeDuplicityRule.AllowDuplicityTakeOriginal)
        {
            var duplicateItems = new List<KeyValuePair<TKey, TValue>>();

            foreach (var item in otherDictionary)
            {
                if (!dictionary.ContainsKey(item.Key))
                {
                    // položka v původní feedu není => přidáme
                    dictionary.Add(item.Key, item.Value);
                }
                else if (duplicityRule == MergeDuplicityRule.AllowDuplicityTakeNew)
                {
                    // položka v původním feedu je, ale máme ji přepsat => přepíšeme a informujeme o duplicitě
                    dictionary[item.Key] = item.Value;
                    duplicateItems.Add(item);
                }
                else if (duplicityRule == MergeDuplicityRule.DisallowDuplicity)
                {
                    // položka v původním feedu je a není povoleno mít duplicity => chyba
                    throw new ArgumentException($"Prvek s ID {item.Key} již ve feedu je a duplicity nejsou povoleny.");
                }
                else
                {
                    // položka v původním feedu je, ale duplicity jsou povoleny, informujeme o duplicitě
                    duplicateItems.Add(item);
                }
            }

            return duplicateItems;
        }

        public GtfsFeed ToGtfsFeed()
        {
            return new GtfsFeed()
            {
                Agency = Agency.Values.ToList(),
                Calendar = Calendar.Values.Select(cal => cal.ToGtfsCalendar()).ToList(),
                CalendarDates = Calendar.Values.SelectMany(cal => cal.Exceptions.Values.Select(ex => ex.ToGtfsCalendarDate(cal.GtfsId))).ToList(),
                FeedInfo = new List<GtfsFeedInfo>() { FeedInfo },
                Routes = Routes.Values.Select(r => r.ToGtfsRoute(Agency.Values.First().Id)).ToList(), // TODO to ruční specifikování agency je dost hnus
                RouteSubAgencies = Routes.Values.SelectMany(r => r.SubAgencies).Distinct().ToList(),
                Shapes = Shapes.Values.SelectMany(s => s.ToGtfsShape()).ToList(),
                Stops = Stops.Values.Select(s => s.ToGtfsStop()).ToList(),
                StopTimes = Trips.Values.SelectMany(t => t.StopTimes.Select(st => st.ToGtfsStopTime())).ToList(),
                Transfers = Transfers.Select(tr => tr.ToGtfsTransfer()).ToList(),
                Trips = Trips.Values.Select(t => t.ToGtfsTrip()).ToList(),
                TripRuns = null, // neřešíme zde, musí si vyřešit volající sám
            };
        }
    }
}
