using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Linka
    {
        [XmlAttribute("c")]
        public int CLinky;

        [XmlAttribute("d")]
        public short CDopravce;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlAttribute("lc")]
        public List<int> LicCislo;

        [XmlAttribute("a")]
        [DefaultValue("")]
        public string AliasLinky;

        [XmlAttribute("aois")]
        [DefaultValue("")]
        public string AliasOIS;

        [XmlAttribute("tl")]
        [DefaultValue("")]
        public string TLinky;

        [XmlAttribute("n")]
        [DefaultValue("")]
        public string Nazev;

        [XmlAttribute("kup")]
        [DefaultValue(0)]
        public short CPref;

        [XmlAttribute("ids")]
        [DefaultValue(false)]
        public bool IDS;

        [XmlAttribute("sko")]
        [DefaultValue(false)]
        public bool Skolni;

        [XmlAttribute("noc")]
        [DefaultValue(false)]
        public bool Nocni;

        [XmlAttribute("inv")]
        [DefaultValue(false)]
        public bool Invalidni;

        [XmlAttribute("mtr")]
        [DefaultValue(false)]
        public bool Metrolinka;

        [XmlAttribute("kli")]
        [DefaultValue(0)]
        public int KategorieProIdos;

        public override string ToString()
        {
            return $"Linka {CLinky} (alias {AliasLinky}) typu {TLinky}";
        }
    }
}
