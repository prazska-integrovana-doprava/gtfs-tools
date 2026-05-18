using CsvSerializer.Attributes;

namespace JdfModel
{
    /// <summary>
    /// Dopravci.txt
    /// </summary>
    public class Agency
    {
        /// <summary>
        /// IČ - povinné osmimístné číslo
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string Id { get; set; }

        /// <summary>
        /// DIČ - nepovinný text
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public string Unused2 { get; set; }

        /// <summary>
        /// Obchodní jméno - povinný text
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string Name { get; set; }

        /// <summary>
        /// Druh firmy - povinné, musí být hodnota 1 nebo 2 (právnická / fyzická osoba)
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public int Unused4 { get; set; }

        /// <summary>
        /// Jméno fyz. osoby - povinný text v případě, že druh firmy = fyz. osoba
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string Unused5 { get; set; }

        /// <summary>
        /// Sídlo (adresa) - povinný text
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string Address { get; set; }

        /// <summary>
        /// Telefon sídla - povinný text
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string PhoneToAddress { get; set; }

        /// <summary>
        /// Telefon dispečink - nepovinný text
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string Unused8 { get; set; }

        /// <summary>
        /// Telefon informace - nepovinný text
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string Unused9 { get; set; }

        /// <summary>
        /// Fax - nepovinný text
        /// </summary>
        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public string Unused10 { get; set; }

        /// <summary>
        /// E-mail - nepovinný text
        /// </summary>
        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string Unused11 { get; set; }

        /// <summary>
        /// www - nepovinný text
        /// </summary>
        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string Url { get; set; }

        /// <summary>
        /// Rozlišení dopravce - povinné číslo
        /// </summary>
        [CsvField("", 13, CsvFieldPostProcess.Quote)]
        public int AgencyVersion { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }
}
