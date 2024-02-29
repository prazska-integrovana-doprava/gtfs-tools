using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    [XmlRoot("JR_XML_EXP")]
    public class DavkaJR
    {
        [XmlAttribute("od", DataType = "date")]
        public DateTime DatumOd;

        [XmlAttribute("do", DataType = "date")]
        public DateTime DatumDo;

        [XmlElement("d")]
        public List<Dopravce> Dopravci;

        [XmlElement("dd")]
        public List<DruhDopravy> DruhyDop;

        [XmlElement("ids")]
        public List<IntegrovanySystem> IntegrovaneSystemy;

        [XmlElement("m")]
        public List<Majo> Majo;

        [XmlElement("z")]
        public List<Zastavka> Zastavky;

        [XmlElement("l")]
        public List<Linka> Linky;

        [XmlElement("k")]
        public List<KategorieLinkyProIdos> KategorieLinek;

        [XmlElement("tv")]
        public List<TypVozu> TypyVozu;

        [XmlElement("ty")]
        public List<TypVykonu> TypyVyk;

        [XmlElement("ch")]
        public List<CharakterVykonu> CharVyk;

        [XmlElement("r")]
        public List<Preference> Preference;

        [XmlElement("po")]
        public List<Poznamka> Poznamky;

        [XmlElement("o")]
        public List<Obeh> Obehy;

        [XmlElement("s")]
        public List<Spoj> Spoje;

        [XmlElement("g")]
        public List<Grafikon> Grafikony;

        [XmlElement("t")]
        public List<Tablo> Tabla;

        [XmlElement("tr")]
        public List<Trasa> Trasy;
  }
}
