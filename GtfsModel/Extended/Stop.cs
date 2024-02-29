using System;
using CommonLibrary;
using GtfsModel.Enumerations;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Reprezentuje pouze zastávky (<see cref="GtfsStop.LocationType"/> = <see cref="LocationType.Stop"/>) ze stops.txt
    /// (rozšíření <see cref="GtfsStop"/>).
    /// </summary>
    public class Stop : BaseStop, IStopWithParent
    {
        /// <summary>
        /// Pokud jde o zastávku z ASW JŘ, je zde uvedeno číslo uzlu z číselníku zastávek, jinak 0.
        /// </summary>
        public int AswNodeId { get; set; }

        /// <summary>
        /// Pokud jde o zastávku z ASW JŘ, je zde uvedeno číslo sloupku z číselníku zastávek, jinak 0.
        /// </summary>
        public int AswStopId { get; set; }

        /// <summary>
        /// Název zastávky dle ASW JŘ (nemusí být unikátní)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Stanoviště
        /// </summary>
        public string PlatformCode { get; set; }

        /// <summary>
        /// Tarifní pásma dle číselníku ASW JŘ (mohou být oddělena čárkou)
        /// </summary>
        public string ZoneId { get; set; }

        /// <summary>
        /// Stanice, do které zastávka patří (viz GTFS dokumentace)
        /// </summary>
        public Station ParentStation { get; set; }

        /// <summary>
        /// Indikace bezbariérovosti zastávky dle výčtu GTFS
        /// </summary>
        public WheelchairBoarding WheelchairBoarding { get; set; }

        /// <summary>
        /// Kategorie umístění zastávky pro určení polohy zastávky v systému a výpočet jízdného na mezikrajských linkách
        /// </summary>
        public ZoneRegionType ZoneRegionType { get; set; }

        public override LocationType LocationType => LocationType.Stop;

        /// <summary>
        /// Vrátí GTFS záznam zastávky
        /// </summary>
        /// <returns>GTFS záznam zastávky</returns>
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
                ParentStationId = ParentStation != null ? ParentStation.GtfsId : "",
                WheelchairBoarding = WheelchairBoarding,
                PlatformCode = PlatformCode,
                AswNodeId = AswNodeId,
                AswStopId = AswStopId,
                ZoneRegionType = ZoneRegionType,
            };
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(PlatformCode))
                return $"Stop {GtfsId} {Name} {PlatformCode} [{ZoneId}]";
            else
                return $"Stop {GtfsId} {Name} [{ZoneId}]";
        }
    }
}
