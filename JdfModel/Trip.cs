using CsvSerializer.Attributes;
using System.Linq;

namespace JdfModel
{
    /// <summary>
    /// Spoje.txt
    /// </summary>
    public class Trip
    {
        /// <summary>
        /// Číslo linky - povinné šestimístné číslo, vazba do Routes
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int RouteId { get; set; }

        /// <summary>
        /// Číslo spoje - povinné číslo - liché číslo u spojů vedených ve směru vedení linky, sudé číslo u spojů vedených ve směru zpět
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        /// <summary>
        /// Pev. kód 1 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string FixedCode1 { get; set; }

        /// <summary>
        /// Pev. kód 2 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public string FixedCode2 { get; set; }

        /// <summary>
        /// Pev. kód 3 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string FixedCode3 { get; set; }

        /// <summary>
        /// Pev. kód 4 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string FixedCode4 { get; set; }

        /// <summary>
        /// Pev. kód 5 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string FixedCode5 { get; set; }

        /// <summary>
        /// Pev. kód 6 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string FixedCode6 { get; set; }

        /// <summary>
        /// Pev. kód 7 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string FixedCode7 { get; set; }

        /// <summary>
        /// Pev. kód 8 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public string FixedCode8 { get; set; }

        /// <summary>
        /// Pev. kód 9 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string FixedCode9 { get; set; }

        /// <summary>
        /// Pev. kód 10 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string FixedCode10 { get; set; }

        /// <summary>
        /// Kód skupiny spojů - povinné číslo v případě, že je nastaven příznak Seskupení spojů v záznamu linky v souboru Linky.txt - vazba do souboru SpojSkup.txt
        /// </summary>
        [CsvField("", 13, CsvFieldPostProcess.Quote)]
        public int? Unused13 { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo, vazba do Routes
        /// </summary>
        [CsvField("", 14, CsvFieldPostProcess.Quote)]
        public int RouteVersion { get; set; }

        /// <summary>
        /// Všechny pevné kódy (neprázdné)
        /// </summary>
        public string[] FixedCodes
        {
            get
            {
                return new[] { FixedCode1, FixedCode2, FixedCode3, FixedCode4, FixedCode5, FixedCode6, FixedCode7, FixedCode8, FixedCode9, FixedCode10 }
                .Where(v => !string.IsNullOrEmpty(v)).ToArray();
            }
        }

        public override string ToString()
        {
            return $"{RouteId} {TripNumber}";
        }
    }
}
