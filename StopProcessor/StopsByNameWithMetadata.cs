using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace StopProcessor
{
    /// <summary>
    /// Data o zastávkách s metadaty (verze, datum a čas vytvoření). Serializuje se do XML a JSON
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "stops")]
    public class StopsByNameWithMetadata
    {
        [XmlAttribute(AttributeName = "generatedAt")]
        [JsonProperty(PropertyName = "generatedAt")]
        public DateTime GeneratedAt { get; set; }

        [XmlAttribute(AttributeName = "dataFormatVersion")]
        [JsonProperty(PropertyName = "dataFormatVersion")]
        public string DataFormatVersion { get; set; }

        [XmlElement(ElementName = "group")]
        [JsonProperty(PropertyName = "stopGroups")]
        public List<StopCollectionForName> StopGroups { get; set; }

        // kvůli serializaci
        public StopsByNameWithMetadata()
        {
        }

        public static StopsByNameWithMetadata FromStopList(List<StopCollectionForName> stopsByName)
        {
            return new StopsByNameWithMetadata()
            {
                GeneratedAt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second),
                DataFormatVersion = "3",
                StopGroups = stopsByName
            };
        }

    }
}
