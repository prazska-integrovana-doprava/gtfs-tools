using GtfsModel.Enumerations;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Reprezentuje vstupy do stanic (<see cref="GtfsStop.LocationType"/> = <see cref="LocationType.Entrance"/>) ze stops.txt
    /// (rozšíření <see cref="GtfsStop"/>).
    /// </summary>
    public class StationEntrance : BaseStop, IStopWithParent
    {
        /// <summary>
        /// Název zastávky dle ASW JŘ (nemusí být unikátní)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Stanice, do které vstup vede
        /// </summary>
        public Station ParentStation { get; set; }

        /// <summary>
        /// Bezbariérová přístupnost vstupu
        /// </summary>
        public WheelchairBoarding WheelchairBoarding { get; set; }

        public override LocationType LocationType => LocationType.Entrance;

        /// <summary>
        /// Vrátí GTFS záznam vstupu
        /// </summary>
        public override GtfsStop ToGtfsStop()
        {
            return new GtfsStop()
            {
                Id = GtfsId,
                Name = Name,
                Latitude = Position.GpsLatitude,
                Longitude = Position.GpsLongitude,
                ZoneId = "",
                Url = "",
                LocationType = LocationType,
                ParentStationId = ParentStation.GtfsId,
                WheelchairBoarding = WheelchairBoarding,
                PlatformCode = "",
                AswNodeId = ParentStation.AswNodeId,
            };
        }

        public override string ToString()
        {
            return $"Entrance {GtfsId} {Name} of station {ParentStation.Name}";
        }
    }
}
