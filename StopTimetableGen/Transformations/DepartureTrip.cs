using CommonLibrary;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonLibrary.EnumerableExtensions;

namespace StopTimetableGen.Transformations
{
    /// <summary>
    /// Třída pro reprezentaci odjezdu ze zastávky
    /// </summary>
    class DepartureTrip
    {
        /// <summary>
        /// Zastavení v zastávce
        /// </summary>
        public StopTime StopTime { get; set; }

        /// <summary>
        /// Čas odjezdu
        /// </summary>
        public Time DepartureTime { get { return StopTime.DepartureTime; } }

        /// <summary>
        /// Spoj
        /// </summary>
        public Trip Trip { get { return StopTime.Trip; } }

        /// <summary>
        /// Kalendář odjezdu
        /// </summary>
        public CalendarRecord Calendar { get; set; }

        public DepartureTrip(StopTime stopTime, CalendarRecord calendar)
        {
            StopTime = stopTime;
            Calendar = calendar;
        }

        public override string ToString()
        {
            return $"{Trip.ShortName} v {DepartureTime} z {StopTime.Stop.Name}";
        }

        /// <summary>
        /// Sestaví seznam odjezdů. Při tom sloučí odjezdy, které jsou ve stejný čas a po stejné trase.
        /// </summary>
        /// <param name="departures">Zastavení v zastávce - odjezdy.</param>
        public static IEnumerable<DepartureTrip> CreateAndMergeDepartures(IEnumerable<StopTime> departures, bool ignoreWheelchairAccessibility)
        {
            // TODO zvážit úpravu kalendáře tak, aby byl den v týdnu zastoupen už při jediném odjezdu v daný provozní den
            // - jinak se může stát, že se spoj nedostane do jízdního řádu, protože tam se třídí podle dnů v týdnu
            // - na druhou stranu to může být záměr - nezaplevelovat si JŘ jednodenními spoji
            var departureTrips = departures.Select(st => new DepartureTrip(st, st.Trip.CalendarRecord));
            return departureTrips.MergeIdentical(new DepartureCompareAndMerge(ignoreWheelchairAccessibility));
        }

        /// <summary>
        /// Porovnává a slučuje odjezdy ze zastávky
        /// </summary>
        private class DepartureCompareAndMerge : ICompareAndMerge<DepartureTrip>
        {
            private bool _ignoreWheelchairAccessibility;

            public DepartureCompareAndMerge(bool ignoreWheelchairAccessibility)
            {
                _ignoreWheelchairAccessibility = ignoreWheelchairAccessibility;
            }

            public bool AreIdentical(DepartureTrip first, DepartureTrip second)
            {
                return first.StopTime.Stop == second.StopTime.Stop && first.DepartureTime == second.DepartureTime
                    && (first.Trip.WheelchairAccessible == second.Trip.WheelchairAccessible || _ignoreWheelchairAccessibility)
                    && Enumerable.SequenceEqual(ListFollowingStops(first), ListFollowingStops(second));
            }

            private IEnumerable<Stop> ListFollowingStops(DepartureTrip departure)
            {
                return departure.StopTime.ListFollowingPublicStopTimes().Select(st => st.Stop);
            }

            public void MergeSecondIntoFirst(DepartureTrip first, DepartureTrip second)
            {
                if (!first.Trip.Headsign.Contains(second.Trip.Headsign))
                {
                    first.Trip.Headsign = $"{first.Trip.Headsign}/{second.Trip.Headsign}";
                }

                if (second.Trip.BikesAllowed == BikeAccessibility.NotPossible)
                {
                    first.Trip.BikesAllowed = BikeAccessibility.NotPossible;
                }
                else if (second.Trip.BikesAllowed == BikeAccessibility.Unknown && first.Trip.BikesAllowed != BikeAccessibility.NotPossible)
                {
                    first.Trip.BikesAllowed = BikeAccessibility.Unknown;
                }

                if (second.Trip.WheelchairAccessible == WheelchairAccessibility.NotPossible)
                {
                    first.Trip.WheelchairAccessible = WheelchairAccessibility.NotPossible;
                }
                else if (second.Trip.WheelchairAccessible == WheelchairAccessibility.Unknown && first.Trip.WheelchairAccessible != WheelchairAccessibility.NotPossible)
                {
                    first.Trip.WheelchairAccessible = WheelchairAccessibility.Unknown;
                }

                first.Calendar = MergeCalendars(first.Calendar, second.Calendar);
            }

            private CalendarRecord MergeCalendars(CalendarRecord first, CalendarRecord second)
            {
                var resultCalendar = new CalendarRecord()
                {
                    StartDate = first.StartDate < second.StartDate ? first.StartDate : second.StartDate,
                    EndDate = first.EndDate > second.EndDate ? first.EndDate : second.EndDate,
                    Monday = first.Monday || second.Monday,
                    Tuesday = first.Tuesday || second.Tuesday,
                    Wednesday = first.Wednesday || second.Wednesday,
                    Thursday = first.Thursday || second.Thursday,
                    Friday = first.Friday || second.Friday,
                    Saturday = first.Saturday || second.Saturday,
                    Sunday = first.Sunday || second.Sunday,
                };

                for (var date = resultCalendar.StartDate; date <= resultCalendar.EndDate; date = date.AddDays(1))
                {
                    var inService = first.OperatesAt(date) || second.OperatesAt(date);
                    if (inService && !resultCalendar.OperatesAt(date))
                    {
                        resultCalendar.AddException(date, CalendarExceptionType.Add);
                    }
                    else if (!inService && resultCalendar.OperatesAt(date))
                    {
                        resultCalendar.AddException(date, CalendarExceptionType.Remove);
                    }
                }

                return resultCalendar;
            }
        }
    }
}
