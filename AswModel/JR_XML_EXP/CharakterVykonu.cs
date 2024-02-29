using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class CharakterVykonu
    {
        [XmlAttribute("c")]
        public byte CCharVyk;

        [XmlAttribute("n")]
        public string Nazev;
    }
}
