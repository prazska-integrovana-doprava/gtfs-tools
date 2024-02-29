using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class KategorieLinkyProIdos
    {
        [XmlAttribute("c")]
        public int Cislo;

        [XmlAttribute("n")]
        public string Nazev;
    }
}
