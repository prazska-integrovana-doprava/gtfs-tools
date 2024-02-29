using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Bod
    {
        [XmlAttribute("X")]
        public double X;

        [XmlAttribute("Y")]
        public double Y;
    }
}
