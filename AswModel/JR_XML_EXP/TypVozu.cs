using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class TypVozu
    {
        [XmlAttribute("c")]
        public short CTypuVozu;

        [XmlAttribute("z")]
        public string Zkratka;

        [XmlAttribute("n")]
        public string Nazev;

        [XmlAttribute("dd")]
        public byte CDruhuDop;

        [XmlAttribute("np")]
        [DefaultValue(false)]
        public bool Nizkopodlazni;

        [XmlAttribute("pl")]
        [DefaultValue(false)]
        public bool SPlosinou;
    }
}
