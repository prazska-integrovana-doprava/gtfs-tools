using System.ComponentModel;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
    public class Trasa
    {
        [XmlAttribute("zvd")]
        public byte CZavodu;

        [XmlAttribute("u1")]
        [DefaultValue(0)]
        public short CUzlu1;

        [XmlAttribute("z1")]
        [DefaultValue(0)]
        public short CZast1;

        [XmlAttribute("u2")]
        [DefaultValue(0)]
        public short CUzlu2;

        [XmlAttribute("z2")]
        [DefaultValue(0)]
        public short CZast2;

        [XmlAttribute("var")]
        [DefaultValue(0)]
        public short VarTr;

        [XmlAttribute("kj")]
        public string KJ;

        [XmlElement("traj")]
        public Trajektorie Traj;

        public override string ToString()
        {
            return $"Trasa {CUzlu1}/{CZast1} -> {CUzlu2}/{CZast2} var {VarTr} zav {CZavodu} v {KJ}";
        }
    }
}
