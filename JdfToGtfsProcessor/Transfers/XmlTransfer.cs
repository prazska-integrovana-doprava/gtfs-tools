using CommonLibrary;
using System.Xml.Serialization;

namespace JdfToGtfsProcessor.Transfers
{
    public class XmlTransfer
    {
        // First connection
        [XmlAttribute("k1")]
        public int Channel1 { get; set; }

        [XmlAttribute("l1")]
        public int Line1 { get; set; }

        [XmlAttribute("sp1")]
        public int Trip1 { get; set; }

        [XmlAttribute("z1")]
        public int Stop1 { get; set; }

        [XmlAttribute("o1")]
        public int Departure1 { get; set; } // minutes since midnight

        // Second connection
        [XmlAttribute("k2")]
        public int Channel2 { get; set; }

        [XmlAttribute("l2")]
        public int Line2 { get; set; }

        [XmlAttribute("sp2")]
        public int Trip2 { get; set; }

        [XmlAttribute("z2")]
        public int Stop2 { get; set; }

        [XmlAttribute("p2")]
        public int Arrival2 { get; set; } // minutes since midnight

        // Max waiting time
        [XmlAttribute("c")]
        public int MaxWait { get; set; }

        // Helper properties
        [XmlIgnore]
        public Time Departure1Time => new Time(0, Departure1, 0);

        [XmlIgnore]
        public Time Arrival2Time => new Time(0, Arrival2, 0);
    }
}
