using System;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Identifikace objektu v CZPTT
    /// </summary>
    public class PlannedTransportIdentifiers : CompositeIdentifierPlannedType
    {
    }

    /// <summary>
    /// Identifikace objektu v CZPTT použitá ve výlukovém jízdním řádu
    /// </summary>
    public class RelatedPlannedTransportIdentifiers : CompositeIdentifierPlannedType
    {
    }

    /// <summary>
    /// Identifikace vlaku
    /// </summary>
    [Serializable]
    public abstract class CompositeIdentifierPlannedType
    {
        public enum ObjectTypeEnum
        {
            [XmlEnum("TR")]
            TR,

            [XmlEnum("PA")]
            PA,
        }

        /// <summary>
        /// Určuje typ objektu, který identifikátor označuje
        /// 
        /// TR - TRAIN
        /// PA - PATH
        /// </summary>
        [XmlElement]
        public ObjectTypeEnum ObjectType { get; set; }

        /// <summary>
        /// 4místný kód společnosti
        /// </summary>
        [XmlElement]
        public string Company { get; set; }

        /// <summary>
        /// 12 alfanumerických znaků označujících vlak (je v něm zakódováno i číslo vlaku)
        /// </summary>
        [XmlElement]
        public string Core { get; set; }

        /// <summary>
        /// 2 alfanumerické znaky označující variantu vlaku (např. když se víkendový liší od všednodenního)
        /// </summary>
        [XmlElement]
        public string Variant { get; set; }

        /// <summary>
        /// Rok převážné platnosti JŘ
        /// </summary>
        [XmlElement]
        public int TimetableYear { get; set; }

        /// <summary>
        /// Core ID unikátní i mezi Companies (bez varianty a roku)
        /// </summary>
        public string CompanyAndCoreAndYear => $"{Company}_{Core}_{TimetableYear}";

        public override bool Equals(object obj)
        {
            if (!(obj is CompositeIdentifierPlannedType other))
            {
                return false;
            }

            return ObjectType == other.ObjectType && Company == other.Company && Core == other.Core && Variant == other.Variant && TimetableYear == other.TimetableYear;
        }

        public override int GetHashCode()
        {
            return ObjectType.GetHashCode() * 237312 + Company.GetHashCode() * 47921 + Core.GetHashCode() * 17341 + Variant.GetHashCode() * 3131 + TimetableYear.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ObjectType}_{Company}_{Core}_{Variant}_{TimetableYear}";
        }
    }
}
