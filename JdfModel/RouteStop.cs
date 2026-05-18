using CsvSerializer.Attributes;
using System.Linq;

namespace JdfModel
{
    /// <summary>
    /// Zaslinky.txt
    /// </summary>
    public class RouteStop
    {
        /// <summary>
        /// Číslo linky - povinné šestimístné číslo, vazba do <see cref="Route"/>.
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int RouteId { get; set; }

        /// <summary>
        /// Číslo tarifní - povinné číslo (pořadí na trase)
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int StopIndex { get; set; }

        /// <summary>
        /// Tarifní pásmo - nepovinný text
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string FareZone { get; set; }

        /// <summary>
        /// Číslo zastávky - povinné číslo
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public int StopId { get; set; }

        /// <summary>
        /// Průměrná doba - nepovinný text, minuty od první zastávky linky
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string Unused5 { get; set; }

        /// <summary>
        /// Pev. kód 1 - nepovinné číslo, vazba do <see cref="FixedCode"/>
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string FixedCode1 { get; set; }

        /// <summary>
        /// Pev. kód 2 - nepovinné číslo, vazba do <see cref="FixedCode"/>
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string FixedCode2 { get; set; }

        /// <summary>
        /// Pev. kód 3 - nepovinné číslo, vazba do <see cref="FixedCode"/>
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string FixedCode3 { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo, vazba do <see cref="Route"/>
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public int RouteVersion { get; set; }

        /// <summary>
        /// Neprázdné hodnoty z <see cref="FixedCode1"/>, <see cref="FixedCode2"/> a <see cref="FixedCode3"/>
        /// </summary>
        public string[] FixedCodes
        {
            get
            {
                return new[] { FixedCode1, FixedCode2, FixedCode3 }
                .Where(v => !string.IsNullOrEmpty(v)).ToArray();
            }
        }

        public override string ToString()
        {
            return $"{StopId} @ {RouteId}";
        }
    }
}
