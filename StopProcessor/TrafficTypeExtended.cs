using GtfsModel.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StopProcessor
{
    [DataContract]
    public enum TrafficTypeExtended
    {
        [XmlEnum(Name = "undefined")]
        [EnumMember(Value = "undefined")]
        Undefined,

        [XmlEnum(Name = "tram")]
        [EnumMember(Value = "tram")]
        Tram,

        [XmlEnum(Name = "metroA")]
        [EnumMember(Value = "metroA")]
        MetroA,

        [XmlEnum(Name = "metroB")]
        [EnumMember(Value = "metroB")]
        MetroB,

        [XmlEnum(Name = "metroC")]
        [EnumMember(Value = "metroC")]
        MetroC,

        [XmlEnum(Name = "metroAB")]
        [EnumMember(Value = "metroAB")]
        MetroAB,

        [XmlEnum(Name = "metroBC")]
        [EnumMember(Value = "metroBC")]
        MetroBC,

        [XmlEnum(Name = "metroAC")]
        [EnumMember(Value = "metroAC")]
        MetroAC,

        [XmlEnum(Name = "train")]
        [EnumMember(Value = "train")]
        Train,

        [XmlEnum(Name = "bus")]
        [EnumMember(Value = "bus")]
        Bus,

        [XmlEnum(Name = "ferry")]
        [EnumMember(Value = "ferry")]
        Ferry,

        [XmlEnum(Name = "funicular")]
        [EnumMember(Value = "funicular")]
        Funicular,

        [XmlEnum(Name = "trolleybus")]
        [EnumMember(Value = "trolleybus")]
        Trolleybus
    }
}
