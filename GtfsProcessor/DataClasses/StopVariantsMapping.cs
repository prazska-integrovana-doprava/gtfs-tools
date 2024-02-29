using AswModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor.DataClasses
{
    /// <summary>
    /// Mapování ASW záznamu zastávky <see cref="Stop"/> na GTFS zastávky <see cref="GtfsModel.Extended.Stop"/>.
    /// 
    /// Jedna zastávka v ASW může mít odraz ve více zastávkách v GTFS. V základu jsou to dvě varianty - pro MHD (s pásmem P) a pro příměsto (bez pásma P)
    /// </summary>
    class StopVariantsMapping
    {
        /// <summary>
        /// Typy linek projíždějící zastávkou - ve výsledku určují, kolik GTFS záznamů bude třeba
        /// </summary>
        public HashSet<AswLineType> UsedVariants { get; private set; }

        /// <summary>
        /// Zastávka s pásmem P pro MHD Praha
        /// </summary>
        public GtfsModel.Extended.Stop PraguePublicTransportStop { get; set; }

        /// <summary>
        /// Zastávka bez pásma P
        /// </summary>
        public GtfsModel.Extended.Stop SuburbanTransportStop { get; set; }

        /// <summary>
        /// Pro podivné linky, které neidentifikujeme a pro náhradní dopravu za vlaky
        /// </summary>
        public GtfsModel.Extended.Stop UniversalStop { get; set; }

        public StopVariantsMapping()
        {
            UsedVariants = new HashSet<AswLineType>();
        }

        public GtfsModel.Extended.Stop GetGtfsStop(AswLineType lineType)
        {
            switch (lineType)
            {
                case AswLineType.PraguePublicTransport:
                    return PraguePublicTransportStop;

                case AswLineType.RegionalTransport:
                case AswLineType.SuburbanTransport:
                    return SuburbanTransportStop;

                case AswLineType.RailTransport:
                case AswLineType.SpecialTransport:
                    return UniversalStop;

                default:
                    throw new ArgumentException("Going to process line with type '-' or with weird line type");
            }
        }

        /// <summary>
        /// Vrací true, pokud zadaná GTFS zastávka odpovídá této ASW zastávce (tzn. vlastně jedné z GTFS variant této ASW zastávky)
        /// </summary>
        /// <param name="stop">GTFS zastávka</param>
        public bool EqualsToAnyVariant(GtfsModel.Extended.Stop stop)
        {
            return stop.Equals(PraguePublicTransportStop) || stop.Equals(SuburbanTransportStop) || stop.Equals(UniversalStop);
        }

        /// <summary>
        /// Vrací GTFS zastávky, které jsou potřeba podle typů linek, které zastávkou projíždí. Množina může být i prázdná, pokud zastávkou neprojíždí žádné spoje.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GtfsModel.Extended.Stop> GetUsedStopVariants()
        {
            // TODO nemusí být řazené, to jen kvůli testování
            return UsedVariants.OrderBy(lt => (int)lt).Select(v => GetGtfsStop(v)).Distinct();
            //return UsedVariants.Select(v => GetGtfsStop(v)).Distinct();
        }
    }
}
