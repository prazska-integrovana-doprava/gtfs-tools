using CsvSerializer.Attributes;
using System;
using System.Runtime.InteropServices.ComTypes;

namespace JdfModel
{
    /// <summary>
    /// Caskody.txt
    /// </summary>
    public class TimeRemark
    {
        /// <summary>
        /// Číslo linky - povinné šestimístné číslo, vazba do Linky
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int RouteId { get; set; }

        /// <summary>
        /// Číslo spoje - povinné číslo
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        /// <summary>
        /// Číslo časového kódu - povinné číslo (rozlišení časového kódu)
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public int TimeRemarkNumber { get; set; }

        /// <summary>
        /// Označení časového kódu - povinný text, max. 2 znaky
        /// 
        /// Označení časového kódu slouží kdefinování jednoznačného údaje o tom kdy (v konkrétně datově 
        ///stanovených dnech, příp. intervalu konkrétně datově stanovených dnů) daný spoj bude nebo nebude
        /// provozován.Označení časového kódu musí být vyjádřeno jen číslem z intervalu od 10 do 99(dále
        /// jen Značka). V tiskovém výstupu jízdního řádu se Značka převádí na tzv. „negativní značku“ dle
        /// ustanovení bodu 1b přílohy č.2 k vyhlášce
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public string TimeRemarkName { get; set; }

        /// <summary>
        /// Typ časového kódu - musí být prvkem z <see cref="TimeRemarkTypes"/> nebo nevyplněn.
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public char TimeRemarkType { get; set; }

        /// <summary>
        /// Datum od - nepovinné datum
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Datum do - nepovinné datum (pro omezení na jeden den stačí vyplnit jen DateFrom)
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Poznámka - nepovinný text
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string Text { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo, vazba do Routes
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public int RouteVersion { get; set; }

        public override string ToString()
        {
            return $"{TimeRemarkNumber}";
        }
    }

    /// <summary>
    /// Možné hodnoty pro <see cref="TimeRemark.TimeRemarkType"/>
    /// </summary>
    public static class TimeRemarkTypes
    {
        /// <summary>
        /// Jede
        /// </summary>
        public const char OperatesOn = '1';

        /// <summary>
        /// Jede také
        /// 
        /// Nelze užít interval omezení; 
        /// přípustné pouze jednotlivé datově určené dny
        /// </summary>
        public const char OperatesAlso = '2';

        /// <summary>
        /// Jede jen
        /// 
        /// nelze užít interval omezení; 
        /// přípustné pouze jednotlivé datově určené dny; 
        /// nelze kombinovat s žádným jiným pevným kódem ani žádným jiným Typem časového kódu
        /// </summary>
        public const char OperatesOnly = '3';

        /// <summary>
        /// Nejede
        /// </summary>
        public const char DoesNotOperateOn = '4';

        /// <summary>
        /// Jede jen v lichých týdnech
        /// </summary>
        public const char OperatesOnOddWeekdaysOnly = '5';

        /// <summary>
        /// Jede jen v sudých týdnech
        /// </summary>
        public const char OperatesOnEvenWeekdaysOnly = '6';

        /// <summary>
        /// Jede jen v lichých týdnech od..do..
        /// </summary>
        public const char OperatesOnOddWeekdaysOnlyBetween = '7';

        /// <summary>
        /// Jede jen v sudých týdnech od..do..
        /// </summary>
        public const char OperatesOnEvenWeekdaysOnlyBetween = '8';

    }
}
