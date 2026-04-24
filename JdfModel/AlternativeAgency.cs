using CsvSerializer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdfModel
{
    /// <summary>
    /// AltDop.txt
    /// </summary>
    public class AlternativeAgency
    {
        /// <summary>
        /// Číslo linky - povinné šestimístné číslo, vazba do Route
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int RouteNumber { get; set; }

        /// <summary>
        /// Číslo spoje - povinné číslo (0 = platí pro všechny spoje linky)
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        /// <summary>
        /// Povinné osmimístné číslo, vazba do Agency
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string AgencyId { get; set; }

        /// <summary>
        /// Pev. kód 1 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public string FixedCode1 { get; set; }

        /// <summary>
        /// Pev. kód 2 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string FixedCode2 { get; set; }

        /// <summary>
        /// Pev. kód 3 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string FixedCode3 { get; set; }

        /// <summary>
        /// Pev. kód 4 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string FixedCode4 { get; set; }

        /// <summary>
        /// Pev. kód 5 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string FixedCode5 { get; set; }

        /// <summary>
        /// Pev. kód 6 - nepovinné číslo, vazba do FixedCode
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string FixedCode6 { get; set; }

        /// <summary>
        /// Typ časového kódu - nepovinné číslo, povoleny hodnoty 5, 6.
        /// </summary>
        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public char Unused10 { get; set; }

        /// <summary>
        /// Rezerva - nepovinný text
        /// </summary>
        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string Unused11 { get; set; }

        /// <summary>
        /// Datum od - nepovinné datum
        /// </summary>
        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public DateTime? Unused12 { get; set; }

        /// <summary>
        /// Datum do - nepovinné datum
        /// </summary>
        [CsvField("", 13, CsvFieldPostProcess.Quote)]
        public DateTime? Unused13 { get; set; }

        /// <summary>
        /// Rozlišení dopravce - povinné číslo, vazba na rozlišení dopravce
        /// </summary>
        [CsvField("", 14, CsvFieldPostProcess.Quote)]
        public int AgencyVersion { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo
        /// </summary>
        [CsvField("", 15, CsvFieldPostProcess.Quote)]
        public int RouteVersion { get; set; }

        /// <summary>
        /// Všechny pevné kódy (neprázdné)
        /// </summary>
        public string[] FixedCodes
        {
            get
            {
                return new[] { FixedCode1, FixedCode2, FixedCode3, FixedCode4, FixedCode5, FixedCode6 }
                .Where(v => !string.IsNullOrEmpty(v)).ToArray();
            }
        }

        public override string ToString()
        {
            return $"{AgencyId}";
        }
    }
}
