using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Druh dopravy (osobní / soupravový atd.)
    /// </summary>
    public enum TrafficTypeEnum
    {
        [XmlEnum("11")]
        Os,

        [XmlEnum("C1")]
        Ex,

        [XmlEnum("C2")]
        R,

        [XmlEnum("C3")]
        Sp,

        // hodnoty níže nejsou využity
        [XmlEnum("C4")]
        Sv,

        [XmlEnum("C5")]
        Nex,

        [XmlEnum("C6")]
        Pn,

        [XmlEnum("C7")]
        Mn,

        [XmlEnum("C8")]
        Lv,

        [XmlEnum("C9")]
        Vlec,

        [XmlEnum("CA")]
        Sluz,

        [XmlEnum("CB")]
        Pom,
    }
}
