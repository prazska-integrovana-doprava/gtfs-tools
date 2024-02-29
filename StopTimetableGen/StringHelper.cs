using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StopTimetableGen
{
    static class StringHelper
    {
        /// <summary>
        /// Funguje obdobně jako string.Join s čárkou, akorát mezi poslední dvě položky umístí spojku 'a'.
        /// </summary>
        public static string JoinWithCommaAndAnd(IEnumerable<string> items)
        {
            if (!items.Any())
                return "";

            var itemsAsArray = items.ToArray();
            var result = new StringBuilder(itemsAsArray[0]);
            for (int i = 1; i < itemsAsArray.Length - 1; i++)
            {
                result.Append(", ");
                result.Append(itemsAsArray[i]);
            }

            if (itemsAsArray.Length >= 2)
            {
                result.Append(" a ");
                result.Append(itemsAsArray[itemsAsArray.Length - 1]);
            }

            return result.ToString();
        }


        /// <summary>
        /// Text informující, které dny jedou spoje navíc podle sobotního JŘ
        /// </summary>
        public static string GetSaturdayString(DaysOfWeekCalendar calendar, DateTime startDate, DateTime endDate)
        {
            return $"{JoinDates(calendar.ListDatesWithChanges(new[] { DayOfWeek.Saturday }, startDate, endDate))} jede podle sobotního jízdního řádu.";
        }

        /// <summary>
        /// Text informující, které dny jedou spoje navíc podle nedělního JŘ
        /// </summary>
        public static string GetSundayString(DaysOfWeekCalendar calendar, DateTime startDate, DateTime endDate)
        {
            return $"{JoinDates(calendar.ListDatesWithChanges(new[] { DayOfWeek.Sunday }, startDate, endDate))} jede podle nedělního jízdního řádu.";
        }


        /// <summary>
        /// Vytiskne seznam datumů tak, že vždy sloučí ty ve stejném roce
        /// 
        /// Příklad: 24. a 25. 12. 2019, 1. 1., 10. 4., 13. 4. a 5. 7. 2020
        /// </summary>
        /// <param name="dateList">Seznam datumů</param>
        public static string JoinDates(IEnumerable<DateTime> dates)
        {
            var dateList = dates.OrderBy(d => d).ToArray();
            if (!dateList.Any())
                return "";

            var resultStr = new StringBuilder();
            for (int year = dateList.First().Year; year <= dateList.Last().Year; year++)
            {
                var datesOfYear = dateList.Where(d => d.Year == year);
                if (!datesOfYear.Any())
                {
                    continue;
                }

                if (resultStr.Length > 0)
                    resultStr.Append(", ");

                // uvnitř datumu jsou pevné mezery
                resultStr.Append(JoinWithCommaAndAnd(datesOfYear.Select(d => d.ToString("d. M."))));
                resultStr.Append($" {year}");
            }

            return resultStr.ToString();
        }
    }
}
