using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Spoj
    {
        [XmlAttribute("s")]
        public int SpojID;

        [XmlAttribute("id")]
        public int GrafID;

        [XmlAttribute("zvd")]
        public byte CZavodu;

        [XmlAttribute("l")]
        public int CLinky;

        [XmlAttribute("p")]
        public short Poradi;

        [XmlAttribute("sm")]
        [DefaultValue(true)]
        public bool SmerTam;

        [XmlAttribute("dd")]
        public byte CDruhuDopravy;

        [XmlAttribute("pr")]
        public short CProv;

        [XmlAttribute("d")]
        public short CDopravce;

        [XmlAttribute("tv")]
        public short CTypuVozu;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlAttribute("ty")]
        public byte CTypVyk;

        [XmlAttribute("ch")]
        public byte CCharVyk;

        [XmlAttribute("po")]
        public List<int> PozID;

        [XmlAttribute("np")]
        [DefaultValue(false)]
        public bool Nizkopodlazni;

        [XmlAttribute("ids")]
        [DefaultValue(false)]
        public bool IDS;

        [XmlAttribute("jk")]
        [DefaultValue(false)]
        public bool JizdniKola;

        [XmlAttribute("vy")]
        [DefaultValue(false)]
        public bool Vylukovy;

        [XmlAttribute("sp1")]
        [DefaultValue(false)]
        public bool Prvni;

        [XmlAttribute("spN")]
        [DefaultValue(false)]
        public bool Posledni;

        [XmlAttribute("doh")]
        [DefaultValue(false)]
        public bool Dohodnuty;

        [XmlAttribute("pos")]
        [DefaultValue(false)]
        public bool Posilovy;

        [XmlAttribute("man")]
        [DefaultValue(false)]
        public bool Manipulacni;

        [XmlAttribute("lc")]
        [DefaultValue(0)]
        public int LicCislo;

        [XmlElement("x")]
        public List<Zastaveni> Zstv;

        [XmlAttribute("c")]
        [DefaultValue(0)]
        public int Cislo;

        [XmlAttribute("neve")]
        [DefaultValue(false)]
        public bool Neverejny;

        public Spoj()
        {
            SmerTam = true;
        }

        public override string ToString()
        {
            return $"Spoj {SpojID} linky {CLinky} poř. {Poradi} dopravce {CDopravce} grafu {GrafID} druhu {CDruhuDopravy}: char={CCharVyk}, tvyk={CTypVyk}, man={Manipulacni}, np={Nizkopodlazni}";
        }
    }
}
