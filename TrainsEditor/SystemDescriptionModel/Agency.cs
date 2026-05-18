using System.Xml.Serialization;

namespace TrainsEditor.SystemDescriptionModel
{
    public class Agency
    {
        [XmlAttribute("c")]
        public int Id;

        [XmlAttribute("n")]
        public string Name;

        public override string ToString()
        {
            return $"{Id} {Name}";
        }
    }
}
