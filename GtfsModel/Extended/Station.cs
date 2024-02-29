using GtfsModel.Enumerations;
using System.Collections.Generic;
using System;
using CommonLibrary;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Reprezentuje pouze stanice (<see cref="GtfsStop.LocationType"/> = <see cref="LocationType.Station"/>) ze stops.txt
    /// (rozšíření <see cref="GtfsStop"/>).
    /// </summary>
    public class Station : BaseStop
    {
        /// <summary>
        /// Název zastávky dle ASW JŘ (nemusí být unikátní)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tarifní pásma dle číselníku ASW JŘ (mohou být oddělena čárkou)
        /// </summary>
        public string ZoneId { get; set; }

        /// <summary>
        /// Pokud jde o stanici z ASW JŘ, obsahuje číslo uzlu, jinak 0
        /// </summary>
        public int AswNodeId { get; set; }

        /// <summary>
        /// Indikace bezbariérovosti zastávky dle výčtu GTFS
        /// </summary>
        public WheelchairBoarding WheelchairBoarding { get; set; }

        /// <summary>
        /// Kategorie umístění zastávky pro určení polohy zastávky v systému a výpočet jízdného na mezikrajských linkách
        /// </summary>
        public ZoneRegionType ZoneRegionType { get; set; }

        public override LocationType LocationType => LocationType.Station;

        /// <summary>
        /// Seznam zastávek ve stanici
        /// </summary>
        public List<Stop> StopsInStation { get; private set; }

        /// <summary>
        /// Vstupy do stanice
        /// </summary>
        public List<StationEntrance> EntrancesToStation { get; private set; }
        

        public Station()
        {
            StopsInStation = new List<Stop>();
            EntrancesToStation = new List<StationEntrance>();
        }

        /// <summary>
        /// Vrátí GTFS záznam stanice
        /// </summary>
        /// <returns>GTFS záznam stanice</returns>
        public override GtfsStop ToGtfsStop()
        {
            return new GtfsStop()
            {
                Id = GtfsId,
                Name = Name,
                Latitude = Position.GpsLatitude,
                Longitude = Position.GpsLongitude,
                ZoneId = ZoneId,
                Url = "",
                LocationType = LocationType,
                ParentStationId = "",
                WheelchairBoarding = WheelchairBoarding,
                PlatformCode = "",
                AswNodeId = AswNodeId,
                ZoneRegionType = ZoneRegionType,
            };
        }

        public override string ToString()
        {
            return $"Station {GtfsId} {Name} [{ZoneId}] ({StopsInStation.Count} stops)";
        }
    }
}
