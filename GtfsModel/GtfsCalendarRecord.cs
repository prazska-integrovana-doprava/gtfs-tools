using CsvSerializer.Attributes;
using System;

namespace GtfsModel
{
    /// <summary>
    /// Záznam o kalendáři v GTFS
    /// </summary>
    public class GtfsCalendarRecord
    {
        /// <summary>
        /// Unikátní identifikátor kalendáře v datasetu
        /// </summary>
        [CsvField("service_id", 1)]
        public string Id { get; set; }
                
        [CsvField("monday", 2)]
        public bool Monday { get; set; }

        [CsvField("tuesday", 3)]
        public bool Tuesday { get; set; }

        [CsvField("wednesday", 4)]
        public bool Wednesday { get; set; }

        [CsvField("thursday", 5)]
        public bool Thursday { get; set; }

        [CsvField("friday", 6)]
        public bool Friday { get; set; }

        [CsvField("saturday", 7)]
        public bool Saturday { get; set; }

        [CsvField("sunday", 8)]
        public bool Sunday { get; set; }

        /// <summary>
        /// Počátek platnosti kalendáře ve formátu YYYYMMDD
        /// </summary>
        [CsvField("start_date", 9)]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Konec platnosti kalendáře ve formátu YYYYMMDD
        /// </summary>
        [CsvField("end_date", 10)]
        public DateTime EndDate { get; set; }

        public override string ToString()
        {
            return Id;
        }
    }
}
