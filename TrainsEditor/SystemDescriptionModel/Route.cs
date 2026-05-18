using System.ComponentModel;
using System.Xml.Serialization;

namespace TrainsEditor.SystemDescriptionModel
{
    public class Route
    {
        [XmlAttribute("d")]
        public int AgencyId;

        [XmlAttribute("a")]
        [DefaultValue("")]
        public string ShortName;

        [XmlAttribute("n")]
        [DefaultValue("")]
        public string LongName;

        [XmlAttribute("color")]
        [DefaultValue("")]
        public string ColorCodeHtml;

        public override string ToString()
        {
            return $"{ShortName} {LongName}";
        }
    }
}
