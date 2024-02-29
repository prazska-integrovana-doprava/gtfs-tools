using CommonLibrary;
using CsvSerializer.Attributes;
using GtfsModel.Enumerations;

namespace GtfsModel
{
    /// <summary>
    /// Jeden zastávkový sloupek
    /// </summary>
    public class GtfsStop
    {
        /// <summary>
        /// Jednoznačné ID zastávky pro GTFS
        /// </summary>
        [CsvField("stop_id", 1)]
        public string Id { get; set; }

        /// <summary>
        /// Název zastávky
        /// </summary>
        [CsvField("stop_name", 3, CsvFieldPostProcess.Quote)]
        public string Name { get; set; }

        /// <summary>
        /// Zeměpisná šířka pro GTFS.
        /// </summary>
        [CsvField("stop_lat", 5)]
        public double Latitude { get; set; }

        /// <summary>
        /// Zeměpisná délka pro GTFS.
        /// </summary>
        [CsvField("stop_lon", 6)]
        public double Longitude { get; set; }

        /// <summary>
        /// Tarifní pásma (příp. zóny)
        /// </summary>
        [CsvField("zone_id", 7, CsvFieldPostProcess.Quote)]
        public string ZoneId { get; set; }

        /// <summary>
        /// Web adresa s detailem zastávky
        /// </summary>
        [CsvField("stop_url", 8, CsvFieldPostProcess.Quote)]
        public string Url { get; set; }

        /// <summary>
        /// Typ záznamu dle číselníku GTFS
        /// </summary>
        [CsvField("location_type", 9)]
        public LocationType LocationType { get; set; }

        /// <summary>
        /// ID Stanice, do které zastávka patří (viz GTFS dokumentace)
        /// </summary>
        [CsvField("parent_station", 10)]
        public string ParentStationId { get; set; }

        /// <summary>
        /// Indikace bezbariérovosti zastávky dle výčtu GTFS
        /// </summary>
        [CsvField("wheelchair_boarding", 12)]
        public WheelchairBoarding WheelchairBoarding { get; set; }

        /// <summary>
        /// ID úrovně, na které se bod nachází
        /// </summary>
        [CsvField("level_id", 13)]
        public string LevelId { get; set; }

        /// <summary>
        /// Kód stanoviště (typicky písmeno nebo kombinace písmena a čísla)
        /// </summary>
        [CsvField("platform_code", 14)]
        public string PlatformCode { get; set; }

        /// <summary>
        /// ID uzlu v systému ASW JŘ
        /// </summary>
        [CsvField("asw_node_id", 51, CsvFieldPostProcess.None, 0)]
        public int AswNodeId { get; set; }

        /// <summary>
        /// ID zastávky v rámci uzlu v systému ASW JŘ
        /// </summary>
        [CsvField("asw_stop_id", 52, CsvFieldPostProcess.None, 0)]
        public int AswStopId { get; set; }

        /// <summary>
        /// Kategorie umístění zastávky pro určení polohy zastávky v systému a výpočet jízdného na mezikrajských linkách. Definováno jen pro zastávky,
        /// u ostatních typů je null
        /// </summary>
        [CsvField("zone_region_type", 53, CsvFieldPostProcess.None, ZoneRegionType.Undefined)]
        public ZoneRegionType ZoneRegionType { get; set; }

        public override string ToString()
        {
            return $"{Id} {Name}";
        }
    }
}
