using System.Xml.Serialization;

namespace CzpttModel
{

    /// <summary>
    /// Pro nás relevantní pouze hodnoty 01-04. Handover se může vyskytovat tam, kde sousedí infrastruktura SŽDC 
    /// s infrastrukturou jiného provozovatele / vlastníka - vlečky, soukromé dráhy.
    /// Ostatní jsou jen pro mezistátní trasy.
    /// 
    /// Do doby určení od ŠZDC se budou používat pouze body s kódy 01 - 03.
    /// </summary>
    public enum JourneyLocationTypeCodeEnum
    {
        /// <summary>
        /// Výchozí lokalita celé (mezistátní) trasy
        /// </summary>
        [XmlEnum("01")]
        Origin,

        /// <summary>
        /// Nácestná lokalita (neznamená, že tam vlak staví)
        /// </summary>
        [XmlEnum("02")]
        Intermediate,

        /// <summary>
        /// Cílová lokalita (celé mezistátní) trasy - asi se nepoužívá
        /// </summary>
        [XmlEnum("03")]
        Destination,

        /// <summary>
        /// Lokalita, kde dochází k předání odpovědnosti za jízdní řád mezi správci (konstruktéry JŘ)
        /// </summary>
        [XmlEnum("04")]
        Handover,

        /// <summary>
        /// Lokalita, kde dochází k předání odpovědnosti za vlak a trasu mezi dopravci (provozní předání vlaku) - někdy může jít o předání právní odpovědnosti a jindy ne
        /// </summary>
        [XmlEnum("05")]
        Interchange,

        /// <summary>
        /// Lokalita, kde dochází k předání odpovědnosti za jízdní řád a současně provozní předání vlaku mezi dopravci
        /// </summary>
        [XmlEnum("06")]
        HandoverAndInterchange,

        /// <summary>
        /// Státní hranice, zde dochází k předání právní odpovědnosti a rovněž mezi dopravci, pokud dopravce není registrován jako oprávněný na území obou států
        /// </summary>
        [XmlEnum("07")]
        StateBorder,
    }
}
