using CsvSerializer.Attributes;

namespace GtfsModel
{
    /// <summary>
    /// Záznam o jednom tripu na oběhu. Není součástí specifikace GTFS, pouze zneužívám jeho ukládací mechanismus.
    /// </summary>
    public class RunTrip
    {
        /// <summary>
        /// ID kmenové linky.
        /// </summary>
        [CsvField("route_id", 1)]
        public string RouteId { get; set; }

        /// <summary>
        /// Číslo pořadí.
        /// </summary>
        [CsvField("run_number", 2)]
        public int RunNumber { get; set; }

        /// <summary>
        /// Kalendář jako bitová mapa od 1. dne feedu
        /// </summary>
        [CsvField("service_id", 3)]
        public string ServiceId { get; set; }

        /// <summary>
        /// ID tripu na oběhu
        /// </summary>
        [CsvField("trip_id", 4)]
        public string TripId { get; set; }

        /// <summary>
        /// Typ vozu dle číselníku ASW JŘ
        /// </summary>
        [CsvField("vehicle_type", 5)]
        public int VehicleType { get; set; }

        /// <summary>
        /// Licenční číslo linky
        /// </summary>
        [CsvField("route_licence_number", 6, CsvFieldPostProcess.None, 0)]
        public int RouteLicenceNumber { get; set; }

        /// <summary>
        /// Číslo spoje dle CIS (u metra číslo vlaku)
        /// </summary>
        [CsvField("trip_number", 7, CsvFieldPostProcess.None, 0)]
        public int TripNumber { get; set; }
    }
}
