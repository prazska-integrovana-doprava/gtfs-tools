using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Zastavka
    {
        [XmlAttribute("u")]
        public short CUzlu;

        [XmlAttribute("z")]
        public short CZast;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlAttribute("n")]
        public string Nazev;

        [XmlAttribute("n2")]
        [DefaultValue("")]
        public string Nazev2;

        [XmlAttribute("n3")]
        [DefaultValue("")]
        public string Nazev3;

        [XmlAttribute("n4")]
        [DefaultValue("")]
        public string Nazev4;

        [XmlAttribute("n5")]
        [DefaultValue("")]
        public string Nazev5;

        [XmlAttribute("n6")]
        [DefaultValue("")]
        public string Nazev6;

        [XmlAttribute("n7")]
        [DefaultValue("")]
        public string Nazev7;

        [XmlAttribute("n8")]
        [DefaultValue("")]
        public string Nazev8;

        [XmlAttribute("pop")]
        [DefaultValue("")]
        public string Popis;

        [XmlAttribute("cis")]
        [DefaultValue(0)]
        public int CisloCIS;

        [XmlAttribute("ois")]
        [DefaultValue(0)]
        public int CisloOIS;

        [XmlAttribute("ois2")]
        [DefaultValue(0)]
        public int CisloOIS2;

        [XmlAttribute("co")]
        [DefaultValue(0)]
        public int CisloObce;

        [XmlAttribute("no")]
        [DefaultValue("")]
        public string NazevObce;

        [XmlAttribute("cco")]
        [DefaultValue(0)]
        public int CisloCastiObce;

        [XmlAttribute("nco")]
        [DefaultValue("")]
        public string NazevCastiObce;

        [XmlAttribute("spz")]
        [DefaultValue("")]
        public string SPZ;

        [XmlAttribute("kr")]
        [DefaultValue("")]
        public string Kraj;

        [XmlAttribute("ids")]
        [DefaultValue(0)]
        public short IDS;

        [XmlAttribute("tp")]
        [DefaultValue("")]
        public string TarifniPasma;

        [XmlAttribute("ids2")]
        [DefaultValue(0)]
        public short IDS2;

        [XmlAttribute("tp2")]
        [DefaultValue("")]
        public string TarifniPasma2;

        [XmlAttribute("ids3")]
        [DefaultValue(0)]
        public short IDS3;

        [XmlAttribute("tp3")]
        [DefaultValue("")]
        public string TarifniPasma3;

        [XmlAttribute("sx")]
        [DefaultValue(0)]
        public double sX;

        [XmlAttribute("sy")]
        [DefaultValue(0)]
        public double sY;

        [XmlAttribute("sz")]
        [DefaultValue(0)]
        public double sZ;

        [XmlAttribute("lat")]
        [DefaultValue(0)]
        public float Lat;

        [XmlAttribute("lng")]
        [DefaultValue(0)]
        public float Lng;

        [XmlAttribute("sta")]
        [DefaultValue("")]
        public string Stanoviste;

        [XmlAttribute("m")]
        [DefaultValue(0)]
        public short CMajo;

        [XmlAttribute("ve")]
        [DefaultValue(true)]
        public bool Verejna;

        [XmlAttribute("dcsn")]
        [DefaultValue(false)]
        public bool Docasna;

        [XmlAttribute("bb")]
        [DefaultValue(false)]
        public bool Bezbarierova;

        [XmlAttribute("bbc")]
        [DefaultValue(false)]
        public bool BezbarierovaCastecne;

        [XmlAttribute("bbn")]
        [DefaultValue(false)]
        public bool BezbarierovostNestanovena;

        [XmlAttribute("wc")]
        [DefaultValue(false)]
        public bool WC;

        [XmlAttribute("xA")]
        [DefaultValue(false)]
        public bool PrestupA;

        [XmlAttribute("xB")]
        [DefaultValue(false)]
        public bool PrestupB;

        [XmlAttribute("xC")]
        [DefaultValue(false)]
        public bool PrestupC;

        [XmlAttribute("xS")]
        [DefaultValue(false)]
        public bool PrestupS;

        [XmlAttribute("xTra")]
        [DefaultValue(false)]
        public bool PrestupTramvaj;

        [XmlAttribute("xBus")]
        [DefaultValue(false)]
        public bool PrestupAutobus;

        [XmlAttribute("xTro")]
        [DefaultValue(false)]
        public bool PrestupTrolejbus;

        [XmlAttribute("xVla")]
        [DefaultValue(false)]
        public bool PrestupVlak;

        [XmlAttribute("xLod")]
        [DefaultValue(false)]
        public bool PrestupLod;

        [XmlAttribute("xLet")]
        [DefaultValue(false)]
        public bool PrestupLetadlo;

        [XmlAttribute("xLan")]
        [DefaultValue(false)]
        public bool PrestupLanovka;

        [XmlAttribute("kidos")]
        [DefaultValue(0)]
        public int kIDOS;

        [XmlAttribute("st")]
        [DefaultValue("")]
        public string Stat;

        [XmlAttribute("okruh")]
        [DefaultValue(0)]
        public int Okruh;

        [XmlAttribute("nu")]
        [DefaultValue("")]
        public string NazevUnikatni;

        [XmlIgnore]
        public bool IsUsed;

        public Zastavka()
        {
            Verejna = true;
        }

        public override string ToString()
        {
            return $"Zastávka {CUzlu}/{CZast} {Nazev} platná v {KJ}";
        }
    }
}
