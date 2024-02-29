using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Odjezdy v zadaný provozní den (např. pracovní den, sobota, neděle)
    /// </summary>
    class StopTimetableDay
    {
        /// <summary>
        /// Titulek (např. "Pracovní den", "Sobota", "Neděle")
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Znaky v písmu Timetable, které se přiloží za <see cref="Title"/>.
        /// </summary>
        public string TitleSymbols { get; set; }

        /// <summary>
        /// Odjezdy v jednotlivé hodiny (vždy 24 položek, indexováno hodinou, každá položka obsahuje seznam seřazených minutových položek)
        /// </summary>
        public List<Departure>[] Hours { get; private set; }

        public StopTimetableDay()
        {
            Hours = new List<Departure>[24];
            for(int i = 0; i < Hours.Length; i++)
            {
                Hours[i] = new List<Departure>();
            }
        }

        /// <summary>
        /// Vrací nejvyšší počet odjezdů v jedné hodině (ekvivalentně odpovídá počtu sloupečků potřebných v jízdním řádu,
        /// pokud chceme, aby se všechny odjezdy vešly).
        /// </summary>
        public int GetNumberOfMinuteColumnsNeeded()
        {
            return Hours.Max(h => h.Count);
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
