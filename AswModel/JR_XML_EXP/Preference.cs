using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Preference
    {
        [XmlAttribute("c")]
        public short CPref;

        [XmlAttribute("pop")]
        public string Popis;
    }
}
