using GtfsModel.Enumerations;
using System;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Záznam v calendar_dates.txt (rozšíření <see cref="GtfsCalendarDate"/>)
    /// </summary>
    public class CalendarExceptionRecord
    {
        /// <summary>
        /// Datum (index od začátku feedu, 0 = první den)
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Druh výjimky (jede / nejede)
        /// </summary>
        public CalendarExceptionType ExceptionType { get; set; }

        public static CalendarExceptionRecord Construct(GtfsCalendarDate gtfsCalendarDate)
        {
            return new CalendarExceptionRecord()
            {
                Date = gtfsCalendarDate.Date,
                ExceptionType = gtfsCalendarDate.ExceptionType,
            };
        }

        public GtfsCalendarDate ToGtfsCalendarDate(string serviceId)
        {
            return new GtfsCalendarDate()
            {
                ServiceId = serviceId,
                Date = Date,
                ExceptionType = ExceptionType,
            };
        }
        
        public override int GetHashCode()
        {
            return Date.GetHashCode() ^ ExceptionType.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as CalendarExceptionRecord;
            if (other == null)
                return false;

            return Date == other.Date && ExceptionType == other.ExceptionType;
        }

        public override string ToString()
        {
            return $"{ExceptionType} {Date:d.M.yyyy}";
        }
    }
}
