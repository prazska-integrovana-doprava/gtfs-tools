using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Vzdalenost
    {
        [XmlAttribute("p")]
        [DefaultValue("")]
        public string Pasmo;

        [XmlAttribute("m")]
        [DefaultValue(0)]
        public int Metry;
    }
}
