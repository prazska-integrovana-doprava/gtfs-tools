using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Dopravce
    {
        [XmlAttribute("c")]
        public short CDopravce;

        [XmlAttribute("n")]
        public string Nazev;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlAttribute("ncis")]
        [DefaultValue("")]
        public string NazevCIS;

        [XmlAttribute("ico")]
        [DefaultValue("")]
        public string ICO;

        [XmlAttribute("dic")]
        [DefaultValue("")]
        public string DIC;

        [XmlAttribute("ul")]
        [DefaultValue("")]
        public string Ulice;

        [XmlAttribute("me")]
        [DefaultValue("")]
        public string Mesto;

        [XmlAttribute("psc")]
        [DefaultValue("")]
        public string PSC;

        [XmlAttribute("tel")]
        [DefaultValue("")]
        public string Telefon;

        [XmlAttribute("teld")]
        [DefaultValue("")]
        public string TelefonDispecink;

        [XmlAttribute("teli")]
        [DefaultValue("")]
        public string TelefonInformace;

        [XmlAttribute("em")]
        [DefaultValue("")]
        public string Email;
    }
}
