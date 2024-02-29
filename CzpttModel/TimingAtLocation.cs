using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Informace o čase v bodě
    /// </summary>
    [XmlRoot]
    public class TimingAtLocation
    {
        /// <summary>
        /// Rozlišení druhu času (příjezd/odjezd)
        /// </summary>
        public enum TimingQualifierCodeEnum
        {
            [XmlEnum("ALA")]
            Arrival,

            [XmlEnum("ALD")]
            Departure,

            // specifikace zná ještě ELA, LLA, ELD, LLD
        }

        [Serializable]
        public class TimingType
        {
            /// <summary>
            /// Určuje, o jaký čas se jedná (příjezd/odjezd)
            /// </summary>
            [XmlAttribute]
            public TimingQualifierCodeEnum TimingQualifierCode { get; set; }

            /// <summary>
            /// Čas
            /// </summary>
            [XmlIgnore]
            public DateTime Time { get; set; }

            /// <summary>
            /// Časové zóny, které uvádí soubory od SŽ nedávají moc smysl, protože vlak, který jede celoročně, časovou zónu stejně mění ze zimní na letní a zpět
            /// - má tedy smysl brát pouze čas bez zóny a považovat ho prostě za lokální
            /// </summary>
            [XmlElement(ElementName = "Time")]
            public string TimeString
            {
                get { return Time.ToString("HH:mm:ss.fffffff"); }
                set { Time = DateTime.Parse(new string(value.TakeWhile(ch => ch != '+').ToArray())); }
            }

            /// <summary>
            /// Číslo určující počet přechodů přes půlnoc vůči kalendáři a výchozí lokalitě trasy
            /// </summary>
            [XmlElement]
            public int Offset { get; set; }
        }

        /// <summary>
        /// Hodnota <see cref="DwellTime"/> v sekundách.
        /// </summary>
        [XmlIgnore]
        public int DwellTimeSeconds
        {
            get { return (int)Math.Round(DwellTime * 60f); }
            set { DwellTime = value / 60f; }
        }

        /// <summary>
        /// Určuje časy pro příjezdy a průjezdy lokalitou
        /// </summary>
        [XmlElement]
        public List<TimingType> Timing { get; set; }

        /// <summary>
        /// Určuje délku pobytu v lokalitě, uvedenou v minutách na jedno desetinné místo
        /// </summary>
        [XmlElement]
        [DefaultValue(0)]
        public double DwellTime { get; set; }

    }
}