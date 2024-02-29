using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Zastaveni
    {
        [XmlAttribute("u")]
        public short CUzlu;

        [XmlAttribute("z")]
        [DefaultValue(0)]
        public short CZast;

        [XmlAttribute("var")]
        [DefaultValue(0)]
        public short VarTr;

        [XmlAttribute("cis")]
        [DefaultValue(0)]
        public int CisloCIS;

        [XmlAttribute("ncis")]
        [DefaultValue("")]
        public string NazevCIS;

        [XmlAttribute("p")]
        [DefaultValue(-1)]
        public int Prijezd;

        [XmlAttribute("o")]
        [DefaultValue(-1)]
        public int Odjezd;

        [XmlAttribute("ppoposunu")]
        [DefaultValue(0)]
        public int PrijezdPoPosunu;

        [XmlAttribute("opoposunu")]
        [DefaultValue(0)]
        public int OdjezdPoPosunu;

        [XmlAttribute("ty")]
        public byte CTypVyk;

        [XmlAttribute("ces")]
        [DefaultValue(true)]
        public bool Cestujici;

        [XmlAttribute("po")]
        public List<int> PozID;

        [XmlElement("v")]
        public List<Vzdalenost> Vzd;

        [XmlAttribute("zn")]
        [DefaultValue(false)]
        public bool NaZnameni;

        [XmlAttribute("vyst")]
        [DefaultValue(false)]
        public bool JenProVystup;

        [XmlAttribute("nast")]
        [DefaultValue(false)]
        public bool JenProNastup;

        [XmlAttribute("nz")]
        [DefaultValue(false)]
        public bool NaObjednani;

        [XmlAttribute("na")]
        [DefaultValue(false)]
        public bool Nacestna;

        [XmlAttribute("kh1")]
        [DefaultValue(0)]
        public short KH1;

        [XmlAttribute("kh2")]
        [DefaultValue(0)]
        public short KH2;

        [XmlAttribute("poj")]
        [DefaultValue(false)]
        public bool POJ;

        [XmlAttribute("bp")]
        [DefaultValue(false)]
        public bool BP;

        [XmlAttribute("zpl")]
        [DefaultValue(false)]
        public bool PrestLetmoZac;

        [XmlAttribute("kpl")]
        [DefaultValue(false)]
        public bool PrestLetmoKon;

        [XmlAttribute("s")]
        [DefaultValue(false)]
        public bool Stridani;

        [XmlAttribute("dm")]
        [DefaultValue(false)]
        public bool DeponovaciMisto;

        [XmlAttribute("zsol")]
        [DefaultValue(false)]
        public bool ZmenaSmeruOkruzniLinky;

        [XmlAttribute("s1")]
        [DefaultValue(false)]
        public bool ZacatekSmycky;

        [XmlAttribute("s2")]
        [DefaultValue(false)]
        public bool KonecSmycky;

        [XmlAttribute("oc")]
        [DefaultValue(false)]
        public bool Ocest;

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

        [XmlAttribute("icls")]
        [DefaultValue(0)]
        public int IndexCaryLinSez;

        public Zastaveni()
        {
            Prijezd = -1;
            Odjezd = -1;
            Cestujici = true;
        }

        public override string ToString()
        {
            return $"Zastavení v {CUzlu}/{CZast} příj. {Prijezd} odj. {Odjezd} var {VarTr}";
        }
    }
}
