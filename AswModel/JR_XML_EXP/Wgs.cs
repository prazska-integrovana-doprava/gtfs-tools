using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Wgs
    {
        [XmlAttribute("lat")]
        public double Lat;

        [XmlAttribute("lon")]
        public double Lon;
    }
}
