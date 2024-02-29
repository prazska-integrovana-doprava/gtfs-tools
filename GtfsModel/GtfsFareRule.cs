using CsvSerializer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GtfsModel
{
    /// <summary>
    /// Záznam o aplikaci tarifu (na linku nebo zónu)
    /// </summary>
    public class GtfsFareRule
    {
        /// <summary>
        /// ID tarifu z fare_attributes.txt
        /// </summary>
        [CsvField("fare_id", 1)]
        public string FareId { get; set; }

        /// <summary>
        /// Obsažená zóna, na kterou tarif může platit
        /// </summary>
        [CsvField("contains_id", 2)]
        public string ContainsId { get; set; }

        /// <summary>
        /// Linka, na kterou se tarif aplikuje
        /// </summary>
        [CsvField("route_id", 3)]
        public string RouteId { get; set; }
    }
}
