using System;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Základ pro <see cref="CZPTTLocation"/>
    /// </summary>
    [Serializable]
    public class LocationIdent
    {
        /// <summary>
        /// Česká republika pro <see cref="CountryCodeISO"/>
        /// </summary>
        public const string CountryCodeCZ = "CZ";

        /// <summary>
        /// Označuje stát podle číselníku ISO 3166 2 AN zkratou. Česká republika je označena <see cref="CountryCodeCZ"/>.
        /// </summary>
        [XmlElement]
        public string CountryCodeISO { get; set; }

        /// <summary>
        /// 5ti místné číslo označující primární lokalitu. Pro SŽDC jde o číselník SR 70 bez kontrolky.
        /// </summary>
        [XmlElement]
        public int LocationPrimaryCode { get; set; }

        /// <summary>
        /// Název lokality, bere se z DB KANGO, název na 35 znaků
        /// </summary>
        [XmlElement]
        public string PrimaryLocationName { get; set; }

        /// <summary>
        /// Identifikace podřízené lokality, bude identifikovat staniční koleje, nemusí být uvedena v každé lokalitě.
        /// </summary>
        [XmlElement]
        public LocationSubsidiaryIdentification LocationSubsidiaryIdentification { get; set; }

    }
}
