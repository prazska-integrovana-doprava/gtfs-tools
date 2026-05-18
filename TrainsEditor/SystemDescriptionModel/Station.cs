using System.ComponentModel;
using System.Xml.Serialization;

namespace TrainsEditor.SystemDescriptionModel
{
    public class Station
    {
        [XmlAttribute("n")]
        public string Name;

        [XmlAttribute("cis")]
        [DefaultValue(0)]
        public int CisNumber;

        [XmlAttribute("tp")]
        [DefaultValue("")]
        public string Zones;

        [XmlAttribute("lat")]
        [DefaultValue(0)]
        public float GpsLatitude;

        [XmlAttribute("lng")]
        [DefaultValue(0)]
        public float GpsLongitude;

        [XmlAttribute("bb")]
        [DefaultValue(false)]
        public bool WheelchairAccessible;

        [XmlAttribute("bbn")]
        [DefaultValue(false)]
        public bool WheelchairAccessibilityNotSet;

        public override string ToString()
        {
            return $"{CisNumber} {Name}";
        }
    }
}
