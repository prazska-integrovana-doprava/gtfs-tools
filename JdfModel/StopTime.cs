using CsvSerializer.Attributes;
using System.Linq;

namespace JdfModel
{
    /// <summary>
    /// Zasspoje.txt
    /// </summary>
    public class StopTime
    {
        /// <summary>
        /// Číslo linky - povinné šestimístné číslo, vazba do <see cref="Route"/>
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int RouteId { get; set; }

        /// <summary>
        /// Číslo spoje - povinné číslo
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        /// <summary>
        /// Číslo tarifní - povinné číslo
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public int StopIndex { get; set; }

        /// <summary>
        /// Číslo zastávky - povinné číslo, vazba do <see cref="Stop"/>
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public int StopId { get; set; }

        /// <summary>
        /// Kód označníku - povinné číslo v případě, že je nastaven příznak Použití označníků v záznamu linky v souboru Linky.txt
        ///  - spolu s Číslem zastávky vazba do souboru Oznacniky.txt
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string Unused5 { get; set; }

        /// <summary>
        /// Číslo stanoviště - nepovinné číslo (reálně string)
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string PlatformCode { get; set; }

        /// <summary>
        /// Pev. kód 1 - nepovinné číslo, vazba do <see cref="FixedCode"/>
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string FixedCode1 { get; set; }

        /// <summary>
        /// Pev. kód 2 - nepovinné číslo, vazba do <see cref="FixedCode"/>
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string FixedCode2 { get; set; }

        /// <summary>
        /// Pev. kód 3 - nepovinné číslo, vazba do <see cref="FixedCode"/>
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string FixedCode3 { get; set; }

        /// <summary>
        /// Kilometry - povinné číslo v případě, že je vyplněn čas příjezdu nebo odjezdu, nebo pokud čas příjezdu nebo odjezdu obsahuje |
        /// </summary>
        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public int? DistanceFromStartKm { get; set; }

        /// <summary>
        /// Čas příjezdu - povinný v koncové zastávce, číslo, &lt;, |
        ///   - povinný (mimo výchozí zastávku) časový údaj příjezdu pro nejdelší možnou jízdu u spojů, které jsou zcela či zčásti na objednání či podmínečně provozované
        /// </summary>
        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string ArrivalTime { get; set; }

        /// <summary>
        /// Čas odjezdu - nepovinný v koncové zastávce, číslo, &lt;, |
        ///   - časový údaj odjezdu pro nejkratší možnou jízdu u spojů, které jsou zcela či zčásti na objednání či podmínečně provozované
        /// </summary>
        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string DepartureTime { get; set; }

        /// <summary>
        /// Čas příjezdu min. - číslo, &lt;, |
        ///   - povinný údaj (mimo výchozí zastávku) jen u spojů, které jsou zcela či zčásti na objednání či podmínečně provozované
        ///   - časový údaj příjezdu pro nejkratší možnou jízdu
        /// </summary>
        [CsvField("", 13, CsvFieldPostProcess.Quote)]
        public string Unused13 { get; set; }

        /// <summary>
        /// Čas odjezdu max. - číslo, &lt;, |
        ///   - povinný údaj (mimo koncovou zastávku) jen u spojů, které jsou zcela či zčásti na objednání či podmínečně provozované
        ///   - časový údaj odjezdu pro nejdelší možnou jízdu
        /// </summary>
        [CsvField("", 14, CsvFieldPostProcess.Quote)]
        public string Unused14 { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo, vazba do <see cref="Route"/>
        /// </summary>
        [CsvField("", 15, CsvFieldPostProcess.Quote)]
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
            return $"{RouteId} {TripNumber} @ {StopId}";
        }
    }
}
