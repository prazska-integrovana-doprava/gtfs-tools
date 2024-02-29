using CommonLibrary;
using CsvSerializer.Attributes;
using GtfsModel.Enumerations;

namespace GtfsModel
{
    /// <summary>
    /// Jeden průjezd konkrétního spoje zastávkou
    /// </summary>
    public class GtfsStopTime
    {
        /// <summary>
        /// ID spoje v souboru trips.txt
        /// </summary>
        [CsvField("trip_id", 1)]
        public string TripId { get; set; }

        /// <summary>
        /// Čas příjezdu do zastávky.
        /// </summary>
        [CsvField("arrival_time", 2)]
        public Time ArrivalTime { get; set; }

        /// <summary>
        /// Čas odjezdu ze zastávky.
        /// </summary>
        [CsvField("departure_time", 3)]
        public Time DepartureTime { get; set; }

        /// <summary>
        /// ID zastávky v souboru stops.txt
        /// </summary>
        [CsvField("stop_id", 4)]
        public string StopId { get; set; }

        /// <summary>
        /// Sekvence zastávek na trase (postupně všechna zastavení musí tvořit rostoucí posloupnost)
        /// </summary>
        [CsvField("stop_sequence", 5)]
        public int StopSequence { get; set; }

        /// <summary>
        /// Cílová stanice (název), která se má zobrazit, když je spoj v této zastávce (null/empty => použít výchozí trip headsign)
        /// </summary>
        [CsvField("stop_headsign", 6, CsvFieldPostProcess.Quote)]
        public string StopHeadsign { get; set; }

        /// <summary>
        /// Typ nástupu dle číselníku GTFS
        /// </summary>
        [CsvField("pickup_type", 7)]
        public PickupType PickupType { get; set; }

        /// <summary>
        /// Typ výstupu dle číselníku GTFS
        /// </summary>
        [CsvField("drop_off_type", 8)]
        public DropOffType DropOffType { get; set; }

        /// <summary>
        /// Ujetá vzdálenost z trasy spoje (v kilometrech, pro GTFS)
        /// </summary>
        [CsvField("shape_dist_traveled", 9)]
        public double ShapeDistanceTraveled { get; set; }

        /// <summary>
        /// Typ výkonu na odjezdu ze zastávky
        /// </summary>
        [CsvField("trip_operation_type", 61)]
        public TripOperationType TripOperationType { get; set; }

        /// <summary>
        /// Možnosti přepravy kol na spoji v dané zastávce
        /// </summary>
        [CsvField("bikes_allowed", 62)]
        public BikeAccessibility BikesAllowed { get; set; }

        public override string ToString()
        {
            return $"{TripId} at {StopId} at {DepartureTime}";
        }
    }
}
