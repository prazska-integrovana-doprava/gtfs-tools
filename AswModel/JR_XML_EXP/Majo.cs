using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Majo
    {
        [XmlAttribute("c")]
        public short CMajo;

        [XmlAttribute("n")]
        public string Nazev;

        [XmlAttribute("z")]
        [DefaultValue("")]
        public string Zkratka;

        [XmlAttribute("t")]
        [DefaultValue("")]
        public string Telefon;

        [XmlAttribute("e")]
        [DefaultValue("")]
        public string Email;
    }
}
