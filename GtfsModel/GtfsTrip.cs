using CommonLibrary;
using CsvSerializer.Attributes;
using GtfsModel.Enumerations;

namespace GtfsModel
{
    /// <summary>
    /// Jeden spoj
    /// </summary>
    public class GtfsTrip
    {
        /// <summary>
        /// ID linky v routes.txt, na které spoj jezdí
        /// </summary>
        [CsvField("route_id", 1)]
        public string RouteId { get; set; }

        /// <summary>
        /// ID kalendáře v calendar.txt, podle kterého spoj jezdí (které dny)
        /// </summary>
        [CsvField("service_id", 2)]
        public string ServiceId { get; set; }

        /// <summary>
        /// ID spoje v GTFS
        /// </summary>
        [CsvField("trip_id", 3)]
        public string Id { get; set; }

        /// <summary>
        /// Cílová zastávka (název)
        /// </summary>
        [CsvField("trip_headsign", 4, CsvFieldPostProcess.Quote)]
        public string Headsign { get; set; }

        /// <summary>
        /// Název spoje (např. číslo vlaku)
        /// </summary>
        [CsvField("trip_short_name", 5, CsvFieldPostProcess.Quote)]
        public string ShortName { get; set; }

        /// <summary>
        /// Směr
        /// </summary>
        [CsvField("direction_id", 6, CsvFieldPostProcess.None)]
        public Direction DirectionId { get; set; }

        /// <summary>
        /// ID bloku spojů (všechny spoje ve stejném bloku jsou odjety stejným vozidlem, je tedy možné mezi nimi
        /// garantovaně "přestupovat"). Aktuálně odpovídá vždy číslu prvního spoje celého bloku.
        /// </summary>
        [CsvField("block_id", 7)]
        public string BlockId { get; set; }

        /// <summary>
        /// Trasa spoje na mapě (ID obrazce v shapes.txt)
        /// </summary>
        [CsvField("shape_id", 8)]
        public string ShapeId { get; set; }

        /// <summary>
        /// Bezbariérová přístupnost spoje dle číselníku GTFS
        /// </summary>
        [CsvField("wheelchair_accessible", 9)]
        public WheelchairAccessibility WheelchairAccessible { get; set; }

        /// <summary>
        /// Možnost převézt ve spoji kolo (odvozuje se podle typu dopr. prostředku)
        /// </summary>
        [CsvField("bikes_allowed", 10)]
        public BikeAccessibility BikesAllowed { get; set; }

        /// <summary>
        /// Jednička pro divné spoje, které se nemají zobrazovat v mapách
        /// </summary>
        [CsvField("exceptional", 51)]
        public int IsExceptional { get; set; }

        /// <summary>
        /// Interní ID dopravce (index do route_sub_agencies.txt)
        /// </summary>
        [CsvField("sub_agency_id", 52, CsvFieldPostProcess.None, 0)]
        public int SubAgencyId { get; set; }
                
        public override string ToString()
        {
            return $"{RouteId} {Id}";
        }
    }
}
