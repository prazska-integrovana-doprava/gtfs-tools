using System.Collections.Generic;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Trajektorie
    {
        [XmlAttribute("c")]
        public int Pocet;

        [XmlElement("bod")]
        public List<Bod> Bod;
    }
}
