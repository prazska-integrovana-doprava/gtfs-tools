using GtfsModel.Extended;
using System.Collections.Generic;

namespace GtfsModel.Functions
{
    /// <summary>
    /// Porovnává dva kalendáře. Musí být úplně shodné z hlediska začátku, konce, dnů v týdnu i výjimek. Lišit se může pouze ID.
    /// 
    /// I když dva kalendáře efektivně reprezentují stejné množiny dnů, nebudou považovány za shodné, nejsou-li shodné všechny atributy výše.
    /// </summary>
    public class CalendarContentComparer : IEqualityComparer<BaseCalendarRecord>
    {
        public bool Equals(BaseCalendarRecord x, BaseCalendarRecord y)
        {
            var xCalendarRecord = x as CalendarRecord;
            var yCalendarRecord = y as CalendarRecord;

            if (xCalendarRecord != null && yCalendarRecord != null)
            {
                if (xCalendarRecord.StartDate != yCalendarRecord.StartDate) return false;
                if (xCalendarRecord.EndDate != yCalendarRecord.EndDate) return false;
                for (int i = 0; i < xCalendarRecord.InService.Length; i++)
                {
                    if (xCalendarRecord.InService[i] != yCalendarRecord.InService[i]) return false;
                }
            }
            else if (xCalendarRecord != null && yCalendarRecord == null || xCalendarRecord == null && yCalendarRecord != null)
            {
                return false;
            }

            if (x.Exceptions.Count != y.Exceptions.Count)
            {
                return false;
            }

            foreach (var ex in x.Exceptions)
            {
                if (!y.Exceptions.ContainsKey(ex.Key) || y.Exceptions[ex.Key].ExceptionType != ex.Value.ExceptionType)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(BaseCalendarRecord obj)
        {
            var result = obj.Exceptions.Count * 5984017;
            
            if (obj is CalendarRecord)
            {
                var calendarRecord = obj as CalendarRecord;
                result ^= calendarRecord.StartDate.GetHashCode();
                result ^= calendarRecord.EndDate.GetHashCode() * 137;
                for (int i = 0; i < calendarRecord.InService.Length; i++)
                {
                    result ^= calendarRecord.InService[i].GetHashCode() << i;
                }
            }

            return result;
        }
    }
}
