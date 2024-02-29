using System.Collections.Generic;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Grafikon
    {
        [XmlAttribute("id")]
        public int GrafID;

        [XmlAttribute("c")]
        public int Cislo;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlAttribute("po")]
        public List<int> PozID;

        [XmlAttribute("zvd")]
        public byte CZavodu;

        [XmlAttribute("pd")]
        public string ProvozniDen;

        public override string ToString()
        {
            return $"Grafikon {GrafID} číslo {Cislo} KJ {KJ} závodu {CZavodu} v {ProvozniDen}";
        }
    }
}
