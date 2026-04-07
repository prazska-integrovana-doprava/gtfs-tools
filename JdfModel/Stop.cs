using CsvSerializer.Attributes;
using System.Linq;

namespace JdfModel
{
    /// <summary>
    /// Zastavky.txt
    /// </summary>
    public class Stop
    {
        /// <summary>
        /// Číslo zastávky - povinné číslo
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int StopId { get; set; }

        /// <summary>
        /// Název obce - povinný text
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public string NamePart1 { get; set; }

        /// <summary>
        /// Část obce - nepovinný text
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string NamePart2 { get; set; }

        /// <summary>
        /// Bližší místo - nepovinný text
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public string NamePart3 { get; set; }

        /// <summary>
        /// Blízká obec - povinná, jestliže stát je SZ nebo SK
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string DistrictCode { get; set; }

        /// <summary>
        /// Stát - povinný
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string CountryCode { get; set; }

        /// <summary>
        /// Pev. kód 1 - nepovinné číslo, vazba na FixedCode
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string FixedCode1 { get; set; }

        /// <summary>
        /// Pev. kód 2 - nepovinné číslo, vazba na FixedCode
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string FixedCode2 { get; set; }

        /// <summary>
        /// Pev. kód 3 - nepovinné číslo, vazba na FixedCode
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string FixedCode3 { get; set; }

        /// <summary>
        /// Pev. kód 4 - nepovinné číslo, vazba na FixedCode
        /// </summary>
        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public string FixedCode4 { get; set; }

        /// <summary>
        /// Pev. kód 5 - nepovinné číslo, vazba na FixedCode
        /// </summary>
        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string FixedCode5 { get; set; }

        /// <summary>
        /// Pev. kód 6 - nepovinné číslo, vazba na FixedCode
        /// </summary>
        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string FixedCode6 { get; set; }

        public string[] FixedCodes
        {
            get
            {
                return new[] { FixedCode1, FixedCode2, FixedCode3, FixedCode4, FixedCode5, FixedCode6 }
                .Where(v => !string.IsNullOrEmpty(v)).ToArray();
            }
        }

        /// <summary>
        /// Vrátí název zastávek sestavený z jeho částí. Odstraňuje dvojčárky a čárky a mezery na konci názvu
        /// </summary>
        public string StopName
        {
            get
            {
                var result = (NamePart1 + "," + NamePart2 + "," + NamePart3).Replace(",,", ",");
                while (result.EndsWith(" ") || result.EndsWith(","))
                {
                    result = result.Substring(0, result.Length - 1);
                }

                return result;
            }
        }

        public override string ToString()
        {
            return $"{StopName} ({StopId})";
        }
    }
}
