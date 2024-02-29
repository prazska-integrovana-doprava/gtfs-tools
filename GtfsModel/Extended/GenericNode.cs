using System;
using GtfsModel.Enumerations;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Reprezentuje obecné body ve stanicích (<see cref="GtfsStop.LocationType"/> = <see cref="LocationType.GenericNode"/>) ze stops.txt
    /// (rozšíření <see cref="GtfsStop"/>).
    /// </summary>
    public class GenericNode : BaseStop, IStopWithParent
    {
        /// <summary>
        /// Stanice, do které zastávka patří (viz GTFS dokumentace)
        /// </summary>
        public Station ParentStation { get; set; }

        public override LocationType LocationType => LocationType.GenericNode;

        /// <summary>
        /// Vrátí GTFS záznam vstupu
        /// </summary>
        public override GtfsStop ToGtfsStop()
        {
            return new GtfsStop()
            {
                Id = GtfsId,
                Name = "",
                Latitude = Position.GpsLatitude,
                Longitude = Position.GpsLongitude,
                ZoneId = "",
                Url = "",
                LocationType = LocationType,
                ParentStationId = ParentStation.GtfsId,
                WheelchairBoarding = WheelchairBoarding.Unknown,
                PlatformCode = "",
            };
        }

        public override string ToString()
        {
            return $"Node {GtfsId} of station {ParentStation.Name}";
        }
    }
}
