using CommonLibrary;
using GtfsModel.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Jeden průjezd konkrétního spoje zastávkou
    /// </summary>
    public class StopTime
    {
        /// <summary>
        /// Trip, ke kterému zastavení patří
        /// </summary>
        public Trip Trip { get; set; }

        /// <summary>
        /// Čas příjezdu do zastávky. U výchozí zastávky obsahuje nedefinovanou hodnotu.
        /// </summary>
        public Time ArrivalTime { get; set; }

        /// <summary>
        /// Čas odjezdu ze zastávky. U cílové zastávky obsahuje nedefinovanou hodnotu.
        /// </summary>
        public Time DepartureTime { get; set; }

        /// <summary>
        /// Zastávka
        /// </summary>
        public Stop Stop { get; set; }

        /// <summary>
        /// Cílová stanice, která se má zobrazit, když je spoj v této zastávce (null/empty => použít výchozí trip headsign).
        /// </summary>
        public string StopHeadsign { get; set; }

        /// <summary>
        /// Typ nástupu dle číselníku GTFS
        /// </summary>
        public PickupType PickupType { get; set; }

        /// <summary>
        /// Typ výstupu dle číselníku GTFS
        /// </summary>
        public DropOffType DropOffType { get; set; }

        /// <summary>
        /// Vrací true, pokud je zastávka validní (nastavená, má alespoň GTFS ID a souřadnici)
        /// </summary>
        public bool IsValid => Stop != null && !string.IsNullOrEmpty(Stop.GtfsId) && Stop.Position.GpsLatitude != 0 && Stop.Position.GpsLongitude != 0;

        /// <summary>
        /// True, pokud jde o veřejné zastavení (alespoň výstup nebo nástup)
        /// </summary>
        public bool IsPublic => DropOffType != DropOffType.None || PickupType != PickupType.None;

        /// <summary>
        /// Pořadí zastávky na trase (po trase musí být vzestupné)
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Ujetá vzdálenost z trasy spoje (v metrech).
        /// </summary>
        public double ShapeDistanceTraveledMeters { get; set; }

        /// <summary>
        /// Typ výkonu na odjezdu ze zastávky
        /// </summary>
        public TripOperationType TripOperationType { get; set; }

        /// <summary>
        /// Možnosti přepravy kol na spoji v dané zastávce
        /// </summary>
        public BikeAccessibility BikesAllowed { get; set; }

        /// <summary>
        /// Vrací true, pokud jde o konečnou zastávku
        /// </summary>
        public bool IsLastPublicStop
        {
            get
            {
                return Trip.PublicStopTimes.Last() == this;
            }
        }

        /// <summary>
        /// Vrátí všechna zastavení, která ve spoji následují po tomto.
        /// </summary>
        public IEnumerable<StopTime> ListFollowingPublicStopTimes()
        {
            bool stopTimePassed = false;
            foreach (var st in Trip.StopTimes)
            {
                if (stopTimePassed && st.IsPublic)
                    yield return st;

                if (st == this)
                    stopTimePassed = true;
            }
        }

        /// <summary>
        /// Vytvoří data o zastavení spoje v zastávce z GTFS záznamu
        /// </summary>
        /// <param name="gtfsStopTime">GTFS záznam</param>
        public static StopTime Construct(GtfsStopTime gtfsStopTime, IDictionary<string, Trip> trips, IDictionary<string, BaseStop> stops)
        {
            return new StopTime()
            {
                ArrivalTime = gtfsStopTime.ArrivalTime,
                DepartureTime = gtfsStopTime.DepartureTime,
                DropOffType = gtfsStopTime.DropOffType,
                PickupType = gtfsStopTime.PickupType,
                SequenceNumber = gtfsStopTime.StopSequence,
                ShapeDistanceTraveledMeters = gtfsStopTime.ShapeDistanceTraveled * 1000,
                StopHeadsign = gtfsStopTime.StopHeadsign,
                Stop = (Stop) stops[gtfsStopTime.StopId],
                Trip = trips[gtfsStopTime.TripId],
                TripOperationType = gtfsStopTime.TripOperationType,
                BikesAllowed = gtfsStopTime.BikesAllowed,
            };
        }

        public GtfsStopTime ToGtfsStopTime()
        {
            return new GtfsStopTime()
            {
                TripId = Trip.GtfsId,
                ArrivalTime = ArrivalTime,
                DepartureTime = DepartureTime,
                StopId = Stop.GtfsId,
                StopSequence = SequenceNumber,
                StopHeadsign = StopHeadsign != null ? StopHeadsign : "",
                PickupType = PickupType,
                DropOffType = DropOffType,
                ShapeDistanceTraveled = ShapeDistanceTraveledMeters / 1000.0,
                TripOperationType = TripOperationType,
                BikesAllowed = BikesAllowed,
            };
        }

        public override string ToString()
        {
            return $"{Trip.GtfsId} at {Stop} at {DepartureTime}";
        }
    }
}
