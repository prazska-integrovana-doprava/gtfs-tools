using CsvSerializer.Attributes;

namespace GtfsModel
{
    /// <summary>
    /// Záznam jednoho dopravce
    /// </summary>
    public class GtfsAgency
    {
        /// <summary>
        /// ID dopravce.
        /// </summary>
        [CsvField("agency_id", 1)]
        public int Id { get; set; }

        /// <summary>
        /// Název dopravce.
        /// </summary>
        [CsvField("agency_name", 2, CsvFieldPostProcess.Quote)]
        public string Name { get; set; }

        /// <summary>
        /// Stránka dopravce na webu.
        /// </summary>
        [CsvField("agency_url", 3, CsvFieldPostProcess.Quote)]
        public string Url { get; set; }

        /// <summary>
        /// Časová zóna.
        /// </summary>
        [CsvField("agency_timezone", 4)]
        public string Timezone { get; set; }

        /// <summary>
        /// Jazyk feedu.
        /// </summary>
        [CsvField("agency_lang", 5)]
        public string Lang { get; set; }

        /// <summary>
        /// Telefonní kontakt.
        /// </summary>
        [CsvField("agency_phone", 6, CsvFieldPostProcess.Quote)]
        public string Phone { get; set; }

        public override string ToString()
        {
            return $"{Id} {Name}";
        }
    }
}
