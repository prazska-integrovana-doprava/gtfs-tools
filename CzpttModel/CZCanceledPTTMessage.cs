using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Root zprávy o zrušení jednoho vlaku
    /// </summary>
    [XmlRoot]
    public class CZCanceledPTTMessage
    {
        /// <summary>
        /// Identifikace zrušeného vlaku (PA ID a TR ID)
        /// </summary>
        [XmlElement]
        public List<PlannedTransportIdentifiers> PlannedTransportIdentifiers { get; set; }

        /// <summary>
        /// Datum a čas zrušení (jeden vlak může být zrušen / upraven vícekrát, tak aby se poznalo, kdy je poslední změna, která platí)
        /// </summary>
        [XmlElement]
        public DateTime CZPTTCancelation { get; set; }

        /// <summary>
        /// Kalendář určující, které dny je vlak zrušen
        /// </summary>
        [XmlElement]
        public PlannedCalendar PlannedCalendar { get; set; }

        public PlannedTransportIdentifiers GetTrainIdentifier()
        {
            return PlannedTransportIdentifiers.FirstOrDefault(idf => idf.ObjectType == CompositeIdentifierPlannedType.ObjectTypeEnum.TR);
        }

        public PlannedTransportIdentifiers GetPathIdentifier()
        {
            return PlannedTransportIdentifiers.FirstOrDefault(idf => idf.ObjectType == CompositeIdentifierPlannedType.ObjectTypeEnum.PA);
        }
    }
}
