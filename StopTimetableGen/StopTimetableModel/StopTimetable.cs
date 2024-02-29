using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Zastávkový jízdní řád (z jedné zastávky v jednom směru)
    /// </summary>
    class StopTimetable
    {
        /// <summary>
        /// Linka (sdružuje ostatní zastávkové JŘ)
        /// </summary>
        public LineTimetables OwnerLine { get; set; }

        /// <summary>
        /// Odkaz na záznam o zastávce z GTFS
        /// </summary>
        public Stop Stop { get; set; }

        /// <summary>
        /// Název zastávky
        /// </summary>
        public string SrcStopName { get { return Stop.Name; } }

        /// <summary>
        /// Směr (BÚNO)
        /// </summary>
        public int Direction { get; set; }

        /// <summary>
        /// Seznam minulých, této a budoucích zastávek řazených v pořadí, ve kterém jsou obsluhovány linkou
        /// </summary>
        public List<StopOnLine> Stops { get; set; }

        /// <summary>
        /// Poznámky k jízdnímu řádu
        /// </summary>
        public List<IRemark> Remarks { get; set; }

        /// <summary>
        /// Hodina, kterou JŘ začíná
        /// </summary>
        public int FirstHour { get; set; }

        /// <summary>
        /// Hodina, kterou JŘ končí
        /// </summary>
        public int LastHour { get; set; }

        /// <summary>
        /// Sloupečky s provozními dny (pracovní den, sobota, neděle apod.)
        /// </summary>
        public List<StopTimetableDay> DayColumns { get; set; }

        public StopTimetable()
        {
            Stops = new List<StopOnLine>();
            Remarks = new List<IRemark>();
            DayColumns = new List<StopTimetableDay>();
        }

        public override string ToString()
        {
            return $"Line {OwnerLine.LineNumber} from {SrcStopName}, direction {Direction}";
        }

        /// <summary>
        /// Vrací true, pokud jsou všechny spoje v jízdním řádu bezbariérové.
        /// </summary>
        public bool AreAllTripsWheelchairAccessible()
        {
            foreach (var dayColumn in DayColumns)
            {
                foreach (var hour in dayColumn.Hours)
                {
                    foreach (var departure in hour)
                    {
                        if (!departure.IsWheelchairAccessible)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Vrací true, pokud je alespoň jeden spoj v jízdním řádu bezbariérový
        /// </summary>
        public bool IsAnyTripWheelchairAccessible()
        {
            foreach (var dayColumn in DayColumns)
            {
                foreach (var hour in dayColumn.Hours)
                {
                    foreach (var departure in hour)
                    {
                        if (departure.IsWheelchairAccessible)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
