using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace GtfsModel.Enumerations
{
    /// <summary>
    /// Bezbariérová přístupnost zastávky dle číselníku GTFS.
    /// </summary>
    [DataContract]
    public enum WheelchairBoarding
    {
        [XmlEnum(Name = "unknown")]
        [EnumMember(Value = "unknown")]
        Unknown = 0,

        [XmlEnum(Name = "possible")]
        [EnumMember(Value = "possible")]
        Possible = 1,

        [XmlEnum(Name = "notPossible")]
        [EnumMember(Value = "notPossible")]
        NotPossible = 2
    }
}
