using GtfsModel.Extended;
using GtfsModel.Functions;

namespace JdfToGtfsProcessor.Calendars
{
    internal class CalendarDatabase
    {
        private HashSet<BaseCalendarRecord> existingCalendars;

        public IEnumerable<BaseCalendarRecord> AllCalendars 
        {
            get
            {
                return existingCalendars; 
            }
        }

        public CalendarDatabase()
        {
            existingCalendars = new HashSet<BaseCalendarRecord>(new CalendarContentComparer());
        }

        /// <summary>
        /// Pokud už existuje shodný kalendář dříve vytvořený, vrátí ho. Jinak si ho uloží a vrátí ho taky.
        /// </summary>
        /// <param name="calendar">Kalendář</param>
        /// <returns>Existující nebo zadaný kalendář</returns>
        public BaseCalendarRecord GetExistingOrAddNewCalendar(BaseCalendarRecord calendar)
        {
            if (existingCalendars.TryGetValue(calendar, out var existing))
                return existing;

            existingCalendars.Add(calendar);
            return calendar;
        }

        public void SetCalendarIds()
        {
            var idManager = new CalendarIdManager();
            foreach (var calendar in AllCalendars)
            {
                calendar.GtfsId = idManager.CreateCalendarId(calendar);
            }
        }
    }
}
