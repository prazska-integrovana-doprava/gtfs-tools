using System.Collections.Generic;
using System.Xml.Serialization;

namespace TrainsEditor.SystemDescriptionModel
{
    [XmlRoot("System")]
    public class SystemData
    {
        [XmlAttribute("n", DataType = "string")]
        public string SystemName;

        [XmlElement("d")]
        public List<Agency> Agencies;

        [XmlElement("z")]
        public List<Station> Stops;

        [XmlElement("l")]
        public List<Route> Routes;

    }
}
