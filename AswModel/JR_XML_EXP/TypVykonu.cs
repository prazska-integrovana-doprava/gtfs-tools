using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class TypVykonu
    {
        [XmlAttribute("c")]
        public byte CTypuVyk;

        [XmlAttribute("n")]
        public string Nazev;

        [XmlAttribute("li")]
        [DefaultValue(false)]
        public bool JeLinkovy;
    }
}