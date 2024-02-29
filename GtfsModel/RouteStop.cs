using CsvSerializer.Attributes;
using GtfsModel.Enumerations;

namespace GtfsModel
{
    /// <summary>
    /// Zastávka na lince (pro soubor route_stops.txt, který popisuje trasy linek - agretací z tras spojů linky)
    /// </summary>
    public class RouteStop
    {
        /// <summary>
        /// GTFS ID linky
        /// </summary>
        [CsvField("route_id", 1)]
        public string RouteId { get; set; }

        /// <summary>
        /// Směr (pro každý generujeme posloupnost zvlášť)
        /// </summary>
        [CsvField("direction_id", 2)]
        public Direction DirectionId { get; set; }

        /// <summary>
        /// GTFS ID zastávky na trase
        /// </summary>
        [CsvField("stop_id", 3)]
        public string StopId { get; set; }

        /// <summary>
        /// Pořadí zastávky na trase
        /// </summary>
        [CsvField("stop_sequence", 4)]
        public int StopSequence { get; set; }
    }
}
