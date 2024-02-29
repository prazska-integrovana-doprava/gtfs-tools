using CsvSerializer.Attributes;
using System;

namespace GtfsModel
{
    /// <summary>
    /// Informace o samotném GTFS feedu.
    /// </summary>
    public class GtfsFeedInfo
    {
        /// <summary>
        /// Název publikující instituce
        /// </summary>
        [CsvField("feed_publisher_name", 1, CsvFieldPostProcess.Quote)]
        public string PublisherName { get; set; }

        /// <summary>
        /// URL publikující instituce
        /// </summary>
        [CsvField("feed_publisher_url", 2, CsvFieldPostProcess.Quote)]
        public string PublisherUrl { get; set; }

        /// <summary>
        /// Jazyk feedu
        /// </summary>
        [CsvField("feed_lang", 3)]
        public string Lang { get; set; }

        /// <summary>
        /// Počátek platnosti feedu
        /// </summary>
        [CsvField("feed_start_date", 4)]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Konec platnosti feedu
        /// </summary>
        [CsvField("feed_end_date", 5)]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Kontaktní e-mail pro feed
        /// </summary>
        [CsvField("feed_contact_email", 7, CsvFieldPostProcess.Quote)]
        public string ContactEmail { get; set; }
    }
}
