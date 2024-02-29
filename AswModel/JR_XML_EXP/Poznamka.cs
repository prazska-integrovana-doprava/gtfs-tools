using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Poznamka
    {
        [XmlAttribute("c")]
        public int PozID;

        [XmlAttribute("t")]
        public string Text;

        [XmlAttribute("zkr1")]
        [DefaultValue("")]
        public string Zkratka1;

        [XmlAttribute("zkr2")]
        [DefaultValue("")]
        public string Zkratka2;

        [XmlAttribute("zkr3")]
        [DefaultValue("")]
        public string Zkratka3;

        [XmlAttribute("ois")]
        [DefaultValue(false)]
        public bool bOIS;

        [XmlAttribute("zjr")]
        [DefaultValue(false)]
        public bool bZJR;

        [XmlAttribute("csad")]
        [DefaultValue(false)]
        public bool bCSAD;

        [XmlAttribute("vjr")]
        [DefaultValue(false)]
        public bool bVJR;

        [XmlAttribute("ljr")]
        [DefaultValue(false)]
        public bool bLJR;

        [XmlAttribute("kjr")]
        [DefaultValue(false)]
        public bool bKJR;

        [XmlAttribute("jdf")]
        [DefaultValue(false)]
        public bool bJDF;

        [XmlAttribute("tt")]
        [DefaultValue(false)]
        public bool bTT;

        [XmlAttribute("n")]
        [DefaultValue(false)]
        public bool bNavazna;

        [XmlAttribute("tn")]
        [DefaultValue("")]
        public string TypNavazne;

        [XmlAttribute("u")]
        [DefaultValue(0)]
        public short CUzlu;

        [XmlAttribute("z")]
        [DefaultValue(0)]
        public short CZast;

        [XmlAttribute("u2")]
        [DefaultValue(0)]
        public short CUzlu2;

        [XmlAttribute("z2")]
        [DefaultValue(0)]
        public int CZast2;

        [XmlAttribute("nl")]
        [DefaultValue(0)]
        public int CLinky2;

        [XmlAttribute("anl")]
        [DefaultValue("")]
        public string AliasLinky2;

        [XmlAttribute("cd")]
        [DefaultValue(0)]
        public int CekaciDoba;

        [XmlAttribute("usm")]
        [DefaultValue(0)]
        public short CUzluSmer;

        [XmlAttribute("zsm")]
        [DefaultValue(0)]
        public short CZastSmer;

        [XmlAttribute("dd")]
        public byte CDruhuDop;

        [XmlAttribute("mind")]
        [DefaultValue(0)]
        public int MinDoba;

        public override string ToString()
        {
            return $"Poznámka {PozID} Návazná: {bNavazna} typu {TypNavazne}, 1=({CUzlu}/{CZast}), 2=({CUzlu2}/{CZast2}), ze=({CUzluSmer}/{CZastSmer}), nav.linka={CLinky2} {Text}";
        }
    }
}
