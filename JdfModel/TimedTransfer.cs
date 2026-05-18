using CsvSerializer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace JdfModel
{
    /// <summary>
    /// Navaznosti.txt
    /// </summary>
    public class TimedTransfer
    {
        /// <summary>
        /// Typ návaznosti - povinný znak m/M z <see cref="TimedTransferTypes"/>
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public char TimedTransferType { get; set; }

        /// <summary>
        /// Číslo linky - povinné šestimístné číslo, vazba do Linky
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int RouteId { get; set; }

        /// <summary>
        /// Číslo spoje - povinné číslo, vazba do Spoje
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        /// <summary>
        /// Číslo tarifní - povinné číslo, vzaba do Zaslinky
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public int StopIndex { get; set; }

        /// <summary>
        /// Číslo přestupní linky - nepovinné číslo
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public int? TransferRouteId { get; set; }

        /// <summary>
        /// Číslo přestupní zastávky - nepovinné číslo z registru zastávek CIS JŘ
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public int? TransferStopId { get; set; }

        /// <summary>
        /// Kód označníku přestupní linky - nepovinné číslo z registru zastávek CIS JŘ
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public int? TransferPlatformCode { get; set; }

        /// <summary>
        /// Číslo výchozí/koncové zastávky spoje přestupní linky - nepovinné číslo z registru zastávek CIS JŘ
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public int? TransferEndStopId { get; set; }

        /// <summary>
        /// Kód výchozího/koncového označníku spoje přestupní linky - nepovinné číslo z registru zastávek CIS JŘ
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public int? TransferEndStopPlatformCode { get; set; }

        /// <summary>
        /// Doba čekání - nepovinné číslo, údaj v minutách
        /// </summary>
        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public int? WaitingTime { get; set; }

        /// <summary>
        /// Poznámka - nepovinný text
        /// </summary>
        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string RemarkText { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo, vazba do Linky
        /// </summary>
        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public int RouteVersion { get; set; }

        public override string ToString()
        {
            return RemarkText;
        }
    }

    public static class TimedTransferTypes
    {
        /// <summary>
        /// Spoj (...) vyčká (v zastávce ...) na příjezd spoje ... linky ... / vlaku ... /
        /// </summary>
        public const char IamWaiting = 'm';

        /// <summary>
        /// Na spoj (...) navazuje (v zastávce ...) spoj ... linky ... / vlak .../ lodní doprava do...
        /// </summary>
        public const char TheyAreWaitingForMe = 'M';
    }
}
