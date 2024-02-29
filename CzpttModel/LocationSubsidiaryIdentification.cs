using System;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Informace podřízené lokality
    /// </summary>
    [XmlRoot]
    public class LocationSubsidiaryIdentification
    {
        [Serializable]
        public class LocationSubsidiaryCodeType
        {
            /// <summary>
            /// Typ podřízené lokality (číselník max. 2 položky), zatím se používá jen jeden kód:
            /// 1 - koleje - staniční koleje
            /// </summary>
            [XmlAttribute]
            public int LocationSubsidiaryTypeCode { get; set; }

            /// <summary>
            /// Identifikace staniční koleje
            /// </summary>
            [XmlText]
            public string Value { get; set; }
        }

        /// <summary>
        /// Identifikace staniční koleje
        /// </summary>
        [XmlElement]
        public LocationSubsidiaryCodeType LocationSubsidiaryCode { get; set; }

        /// <summary>
        /// Společnost, která odpovídá za kód podřízené lokality
        /// </summary>
        [XmlElement]
        public string AllocationCompany { get; set; }

        /// <summary>
        /// Název podřízené lokality
        /// </summary>
        [XmlElement]
        public string LocationSubsidiaryName { get; set; }
    }
}
