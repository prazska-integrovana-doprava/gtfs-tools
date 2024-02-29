using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class DruhDopravy
    {
        [XmlAttribute("c")]
        public byte CDruhuDop;

        [XmlAttribute("z")]
        public string Zkratka;

        [XmlAttribute("n")]
        public string Nazev;
    }
}
