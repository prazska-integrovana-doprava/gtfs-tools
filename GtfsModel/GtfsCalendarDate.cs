using CsvSerializer.Attributes;
using GtfsModel.Enumerations;
using System;

namespace GtfsModel
{
    /// <summary>
    /// Záznam v calendar_dates
    /// </summary>
    public class GtfsCalendarDate
    {
        /// <summary>
        /// ID kalendáře v calendar.txt, na který se výjimka aplikuje
        /// </summary>
        [CsvField("service_id", 1)]
        public string ServiceId { get; set; }

        /// <summary>
        /// Datum ve formátu YYYYMMDD
        /// </summary>
        [CsvField("date", 2)]
        public DateTime Date { get; set; }

        /// <summary>
        /// Druh výjimky (jede / nejede)
        /// </summary>
        [CsvField("exception_type", 3)]
        public CalendarExceptionType ExceptionType { get; set; }
    }
}
