using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Dvojice název + hodnota
    /// </summary>
    [XmlRoot]
    public class NameAndValuePair
    {
        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public string Value { get; set; }

        public NameAndValuePair Clone()
        {
            return new NameAndValuePair()
            {
                Name = Name,
                Value = Value,
            };
        }
    }
}
