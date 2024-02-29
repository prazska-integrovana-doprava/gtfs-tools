using System;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Informace o kalendáři spoje
    /// </summary>
    [XmlRoot]
    public class PlannedCalendar
    {
        [Serializable]
        public class ValidityPeriodType
        {
            /// <summary>
            /// Počáteční den platnosti kalendáře
            /// </summary>
            [XmlElement]
            public DateTime StartDateTime { get; set; }

            /// <summary>
            /// Poslední den platnosti kalendáře
            /// </summary>
            [XmlElement]
            public DateTime EndDateTime { get; set; }
        }

        /// <summary>
        /// 1 = vlak je v daný den jedoucí, 0 = vlak je v daný den nejedoucí.
        /// Počátek = <see cref="ValidityPeriodType.StartDateTime"/>
        /// (u rušících záznamů je význam: 1 = vlak je tento den zrušen, 0 = vlak není tento den zrušen)
        /// </summary>
        [XmlElement]
        public string BitmapDays { get; set; }

        /// <summary>
        /// Určení období platnosti
        /// </summary>
        [XmlElement]
        public ValidityPeriodType ValidityPeriod { get; set; }
    }
}
