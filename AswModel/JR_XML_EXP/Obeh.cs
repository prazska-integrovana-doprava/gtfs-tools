using System.Collections.Generic;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Obeh
    {
        [XmlAttribute("l")]
        public int CLinky;
        
        [XmlAttribute("p")]
        public short Poradi;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlAttribute("sp")]
        public List<int> SpojID;

        [XmlAttribute("tv")]
        public short CTypuVozu;

        [XmlAttribute("td")]
        public byte CProvDne;

        [XmlElement("ds")]
        public List<DlouhySpoj> DlouheSpoje;

        public override string ToString()
        {
            return $"Oběh {CLinky}/{Poradi} v {KJ} ({SpojID.Count} spojů)";
        }
    }
}
