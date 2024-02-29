using CsvSerializer.Attributes;
using GtfsModel.Enumerations;

namespace GtfsModel
{

    /// <summary>
    /// Záznam o lince
    /// </summary>
    public class GtfsRoute
    {
        /// <summary>
        /// ID linky pro GTFS
        /// </summary>
        [CsvField("route_id", 1)]
        public string Id { get; set; }

        /// <summary>
        /// ID dopravce
        /// </summary>
        [CsvField("agency_id", 2)]
        public int AgencyId { get; set; }

        /// <summary>
        /// Alias linky
        /// </summary>
        [CsvField("route_short_name", 3)]
        public string ShortName { get; set; }

        /// <summary>
        /// Trasa linky
        /// </summary>
        [CsvField("route_long_name", 4, CsvFieldPostProcess.Quote)]
        public string LongName { get; set; }

        /// <summary>
        /// Druh dopravy dle číselníku GTFS
        /// </summary>
        [CsvField("route_type", 6)]
        public TrafficType Type { get; set; }

        /// <summary>
        /// URL s detailem linky
        /// </summary>
        [CsvField("route_url", 7, CsvFieldPostProcess.Quote)]
        public string Url { get; set; }

        /// <summary>
        /// Barva čáry linky
        /// </summary>
        [CsvField("route_color", 8)]
        public string Color { get; set; }

        /// <summary>
        /// Barva písma pro linku
        /// </summary>
        [CsvField("route_text_color", 9)]
        public string TextColor { get; set; }

        /// <summary>
        /// Indikace, zda jde o noční linku
        /// </summary>
        [CsvField("is_night", 62)]
        public bool IsNight { get; set; }

        /// <summary>
        /// Indikace, zda jde o příměstskou nebo regionální linku
        /// </summary>
        [CsvField("is_regional", 63)]
        public bool IsRegional { get; set; }

        /// <summary>
        /// Indikace, zda jde o linku náhradní dopravy
        /// </summary>
        [CsvField("is_substitute_transport", 64)]
        public bool IsSubstituteTransport { get; set; }
        
        public override string ToString()
        {
            return $"{ShortName} ({Id})";
        }
    }
}
