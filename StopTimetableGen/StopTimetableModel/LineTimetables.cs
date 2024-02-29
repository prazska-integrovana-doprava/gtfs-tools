using System;
using System.Collections.Generic;

namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Reprezentuje soubor zastávkových jízdních řádů obou směrů jedné linky
    /// </summary>
    class LineTimetables
    {
        /// <summary>
        /// Veřejné označení linky (např "9", "B" nebo "S49")
        /// </summary>
        public string LineNumber { get; set;}

        /// <summary>
        /// ID linky v ASW JŘ (pokud v ASW neexistuje, pak 0)
        /// </summary>
        public int LineId { get; set; }

        /// <summary>
        /// Datum počátku platnosti jízdního řádu
        /// </summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// ID dopravce v ASW JŘ (pokud v ASW neexistuje, pak 0)
        /// </summary>
        public int OperatorId { get; set; }

        /// <summary>
        /// Název dopravce
        /// </summary>
        public string OperatorName { get; set; }

        // TODO 
        // (zatím neznáme - není v GTFS)
        ///// <summary>
        ///// Adresa sídla dopravce
        ///// </summary>
        //public string OperatorAddress { get; set; }

        ///// <summary>
        ///// Telefonní číslo na dopravce
        ///// </summary>
        //public string OperatorPhoneNumber { get; set; }

        /// <summary>
        /// Jízdní řády pro jednotlivé směry a zastávky
        /// </summary>
        public List<StopTimetable> StopTimetables { get; set; }

        public LineTimetables()
        {
            StopTimetables = new List<StopTimetable>();
        }

        public override string ToString()
        {
            return $"{LineNumber} from {ValidFrom} ({StopTimetables.Count} records)";
        }
    }
}
