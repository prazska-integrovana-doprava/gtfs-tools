using CommonLibrary;
using GtfsModel.Extended;
using GtfsModel.Functions;
using System;
using System.Collections.Generic;

namespace TrainsEditor.GtfsExport
{
    /// <summary>
    /// Generuje kalendáře podle service days. Obsahuje kontextovou paměť, takže při dotazu se shodným vstupem vrátí stejnou instanci kalendáře.
    /// </summary>
    class CalendarConstructor
    {
        // reprezentuje unikátní kalendář (počátek platnosti + bitmapa)
        private class StartDateAndBitmap
        {
            public DateTime StartDate { get; private set; }
            public ServiceDaysBitmap Bitmap { get; private set; }

            public StartDateAndBitmap(DateTime startDate, ServiceDaysBitmap bitmap)
            {
                StartDate = startDate;
                Bitmap = bitmap;
            }

            public override bool Equals(object obj)
            {
                var other = obj as StartDateAndBitmap;
                if (other == null)
                    return false;

                return StartDate == other.StartDate && Bitmap.Equals(other.Bitmap);
            }

            public override int GetHashCode()
            {
                return StartDate.GetHashCode() * 173 + Bitmap.GetHashCode();
            }

            public override string ToString()
            {
                return $"{StartDate}:{Bitmap}";
            }
        }

        // počáteční datum, od kterého data generujeme (starší ignorujeme)
        private DateTime _referenceStartDate;

        // všechny již vytvořené kalendáře
        private Dictionary<StartDateAndBitmap, CalendarRecord> calendars;

        // pro generování identifikátorů
        private CalendarIdManager calendarIdManager;

        public CalendarConstructor(DateTime referenceStartDate)
        {
            _referenceStartDate = referenceStartDate;
            calendars = new Dictionary<StartDateAndBitmap, CalendarRecord>();
            calendarIdManager = new CalendarIdManager();
        }

        /// <summary>
        /// Vrátí kalendář pro zadanou dvojici počátku platnosti + bitmapy. Pokud už stejný kalendář byl jednou vytvořen, vrátí se stejná instance,
        /// jinak se vytvoří nový kalendář.
        /// </summary>
        /// <param name="startDate">První den (počátek bitmapy)</param>
        /// <param name="bitmap">Bitmapa</param>
        public CalendarRecord GetCalendarFor(DateTime startDate, ServiceDaysBitmap bitmap)
        {
            var startDateAndBitmap = new StartDateAndBitmap(startDate, bitmap);
            if (!calendars.ContainsKey(startDateAndBitmap))
            {
                var calendarRecord = new CalendarRecord()
                {
                    StartDate = startDate,
                    EndDate = startDate.AddDays(bitmap.Length - 1),
                };

                ProcessDayOfWeek(calendarRecord, DayOfWeek.Monday, bitmap);
                ProcessDayOfWeek(calendarRecord, DayOfWeek.Tuesday, bitmap);
                ProcessDayOfWeek(calendarRecord, DayOfWeek.Wednesday, bitmap);
                ProcessDayOfWeek(calendarRecord, DayOfWeek.Thursday, bitmap);
                ProcessDayOfWeek(calendarRecord, DayOfWeek.Friday, bitmap);
                ProcessDayOfWeek(calendarRecord, DayOfWeek.Saturday, bitmap);
                ProcessDayOfWeek(calendarRecord, DayOfWeek.Sunday, bitmap);
                calendarRecord.IncorporateExceptions(bitmap);

                calendarRecord.GtfsId = calendarIdManager.CreateCalendarId(calendarRecord, "TR");
                calendars.Add(startDateAndBitmap, calendarRecord);

                if (!calendarRecord.AsServiceBitmap().Equals(bitmap))
                {
                    throw new Exception("Inconsistency during calendar creation. Result calendar is not equal to original bitmap");
                }

                return calendarRecord;
            }
            else
            {
                return calendars[startDateAndBitmap];
            }
        }

        /// <summary>
        /// Vrátí všechny dosud vytvořené kalendáře.
        /// </summary>
        public IEnumerable<CalendarRecord> GetAllCalendars()
        {
            return calendars.Values;
        }

        // zapíše InService pro dayOfWeek a připraví i všechny výjimky
        private void ProcessDayOfWeek(CalendarRecord calendarRecord, DayOfWeek dayOfWeek, ServiceDaysBitmap bitmap)
        {
            int activeCount = 0, inactiveCount = 0;

            for (int i = 0; i < bitmap.Length; i++)
            {
                var date = calendarRecord.StartDate.AddDays(i);
                if (DaysOfWeekCalendars.TrainsInstance.GetDayOfWeekFor(date) == dayOfWeek 
                    && date >= _referenceStartDate) // TODO hack, aby mi to dávalo dny v týdnu jak potřebuju když se objeví všednodenní vlaky,
                                            // které jedou stejně jako víkendové, ale už jen kratší část roku
                {
                    if (bitmap[i])
                    {
                        activeCount++;
                    }
                    else
                    {
                        inactiveCount++;
                    }
                }
            }

            if (activeCount >= 1 && activeCount * 2 >= inactiveCount)
            {
                calendarRecord.SetOperatesOn(dayOfWeek, true);
            }
        }
    }
}
