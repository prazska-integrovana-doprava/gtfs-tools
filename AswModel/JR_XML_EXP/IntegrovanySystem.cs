using System.Xml.Serialization;

namespace JR_XML_EXP
{
    [XmlRoot("INTEGROVANYSYSTEM")]
    public class IntegrovanySystem
    {
        [XmlAttribute("c")]
        public short CIDS;

        [XmlAttribute("z")]
        public string Zkratka;

        [XmlAttribute("n")]
        public string Nazev;

        [XmlAttribute("tapoj")]
        public string Pojmenovani;
    }
}
