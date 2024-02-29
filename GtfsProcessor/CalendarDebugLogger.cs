using System.Collections.Generic;
using System.Linq;
using GtfsLogging;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using GtfsProcessor.Logging;

namespace GtfsProcessor
{
    /// <summary>
    /// Vypíše do logovacího souboru informace o GTFS kalendářích.
    /// </summary>
    class CalendarDebugLogger
    {
        private IEnumerable<CalendarRecord> calendars;
        private ISimpleLogger calendarLog = Loggers.CalendarLoggerInstance;

        public CalendarDebugLogger(IEnumerable<CalendarRecord> calendars)
        {
            this.calendars = calendars;
        }

        public void LogCalendars()
        {
            var calendarsByDaysOfWeek = JoinByServiceDays(calendars);

            calendarLog.Log("REKAPITULACE:");
            foreach (var calendarGroup in calendarsByDaysOfWeek.Values.OrderByDescending(group => group.Sum(cal => cal.Trips.Count)))
            {
                calendarLog.Log($"Skupina {calendarGroup.First().ServiceAsBinaryString:0000000}:");
                foreach (var calendar in calendarGroup)
                {
                    calendarLog.Log($"    * ID={calendar.GtfsId}, Od={calendar.StartDate:d.M.yyyy}, Do={calendar.EndDate:d.M.yyyy}, Výjimky: [{calendar.Exceptions.Count()}]");
                    foreach (var ex in calendar.Exceptions.Values)
                    {
                        if (ex.ExceptionType == CalendarExceptionType.Add)
                            calendarLog.Log($"       +{ex.Date:d.M.yyyy}");
                        else
                            calendarLog.Log($"       -{ex.Date:d.M.yyyy}");
                    }

                    calendarLog.Log($"       * Spoje: [{calendar.Trips.Count}] {string.Join(", ", calendar.Trips.OrderBy(trip => trip.GetHashCode()).Take(10).Select(trip => trip.ToString()))}");
                }
            }
        }

        // sloučí kalendáře do skupin podle Service Days
        private Dictionary<int, List<CalendarRecord>> JoinByServiceDays(IEnumerable<CalendarRecord> calendars)
        {
            var result = new Dictionary<int, List<CalendarRecord>>();
            foreach (var calendar in calendars)
            {
                if (!result.ContainsKey(calendar.ServiceAsFlags))
                {
                    result.Add(calendar.ServiceAsFlags, new List<CalendarRecord>());
                }

                result[calendar.ServiceAsFlags].Add(calendar);
            }

            return result;
        }
    }
}
