using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace GtfsModel.Enumerations
{
    /// <summary>
    /// Typ dopravy dle číselníku GTFS
    /// </summary>
    [DataContract]
    public enum TrafficType
    {
        [XmlEnum(Name = "undefined")]
        [EnumMember(Value = "undefined")]
        Undefined = -1,

        [XmlEnum(Name = "tram")]
        [EnumMember(Value = "tram")]
        Tram = 0,

        [XmlEnum(Name = "metro")]
        [EnumMember(Value = "metro")]
        Metro = 1,

        [XmlEnum(Name = "train")]
        [EnumMember(Value = "train")]
        Rail = 2,

        [XmlEnum(Name = "bus")]
        [EnumMember(Value = "bus")]
        Bus = 3,

        [XmlEnum(Name = "ferry")]
        [EnumMember(Value = "ferry")]
        Ferry = 4,

        [XmlEnum(Name = "cableCar")]
        [EnumMember(Value = "cableCar")]
        CableCar = 5,

        [XmlEnum(Name = "gondola")]
        [EnumMember(Value = "gondola")]
        Gondola = 6,

        [XmlEnum(Name = "funicular")]
        [EnumMember(Value = "funicular")]
        Funicular = 7,

        [XmlEnum(Name = "trolleybus")]
        [EnumMember(Value = "trolleybus")]
        Trolleybus = 11,
    }

}
