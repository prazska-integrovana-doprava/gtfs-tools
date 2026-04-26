using System.Xml.Serialization;

namespace JdfToGtfsProcessor.Transfers
{
    [XmlRoot("NAVAZ")]
    public class XmlTransferCollection
    {
        [XmlElement("n")]
        public List<XmlTransfer>? Items { get; set; }
    }
}
