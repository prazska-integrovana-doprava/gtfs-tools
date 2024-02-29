using GtfsModel.Extended;
using System.Collections.Generic;

namespace GtfsModel.Functions
{
    /// <summary>
    /// Nástroj na tvorbu IDček kalendářů. Postupně generuje unikátní IDčka
    /// </summary>
    public class CalendarIdManager
    {
        private Dictionary<string, int> idCounts = new Dictionary<string, int>();

        /// <summary>
        /// Vytvoří ID pro kalendář obsahující dny v týdnu, jak jede + index, kolikátý kalendář pro tyto dny v týdnu.
        /// </summary>
        /// <param name="calendarRecord">Kalendář, kterému chceme vygenerovat ID</param>
        /// <param name="suffix">Připojí se k pořadovému číslu</param>
        /// <returns>ID pro kalendář</returns>
        public string CreateCalendarId(CalendarRecord calendarRecord, string suffix = "")
        {
            var idBase = calendarRecord.ServiceAsBinaryString.ToString("0000000");
            int numOfExisting;
            if (idCounts.TryGetValue(idBase, out numOfExisting))
            {
                idCounts[idBase]++;
            }
            else
            {
                numOfExisting = 0;
                idCounts.Add(idBase, 1);
            }

            return $"{idBase}_{suffix}{numOfExisting + 1}";
        }
    }
}
