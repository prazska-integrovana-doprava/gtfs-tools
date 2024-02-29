using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Tablo
    {
        [XmlAttribute("u")]
        public short CUzlu;

        [XmlAttribute("z")]
        [DefaultValue(0)]
        public short CZast;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlAttribute("ois")]
        [DefaultValue(0)]
        public int CisloOIS;

        [XmlAttribute("cis")]
        [DefaultValue(0)]
        public int CisloCIS;

        [XmlAttribute("nza")]
        [DefaultValue("")]
        public string NazevZast;

        [XmlAttribute("ri")]
        [DefaultValue("")]
        public string DisplayRidice;

        [XmlAttribute("ji")]
        [DefaultValue("")]
        public string NazevProTiskJizdenek;

        [XmlAttribute("vtm")]
        [DefaultValue("")]
        public string VnitrniTabloMHD;

        [XmlAttribute("vtn")]
        [DefaultValue("")]
        public string VnitrniTabloNeMHD;

        [XmlAttribute("btm")]
        [DefaultValue("")]
        public string BocniTabloMHD;

        [XmlAttribute("btn")]
        [DefaultValue("")]
        public string BocniTabloNeMHD;

        [XmlAttribute("bta")]
        [DefaultValue("")]
        public string BocniTabloAlternativni;

        [XmlAttribute("ctm")]
        [DefaultValue("")]
        public string CelniTabloMHD;

        [XmlAttribute("ctn")]
        [DefaultValue("")]
        public string CelniTabloNeMHD;

        [XmlAttribute("cta")]
        [DefaultValue("")]
        public string CelniTabloAlternativni;

        [XmlAttribute("ztm")]
        [DefaultValue("")]
        public string ZadniTabloMHD;

        [XmlAttribute("ztn")]
        [DefaultValue("")]
        public string ZadniTabloNeMHD;

        [XmlAttribute("lcdm")]
        [DefaultValue("")]
        public string LcdTabloMHD;

        [XmlAttribute("lcdn")]
        [DefaultValue("")]
        public string LcdTabloNeMHD;

        [XmlAttribute("hl")]
        [DefaultValue("")]
        public string Hlaseni;

        [XmlAttribute("n")]
        [DefaultValue("")]
        public string Nazev;

        [XmlAttribute("nf")]
        [DefaultValue("")]
        public string NazevFoneticky;
    }
}
