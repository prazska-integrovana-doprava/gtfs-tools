using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLibrary
{
    /// <summary>
    /// Databáze svátečních dnů
    /// </summary>
    public class PublicHolidaysCalendar
    {
        /// <summary>
        /// Všechny sváteční dny
        /// </summary>
        public List<DateTime> AllHolidays { get; private set; }

        /// <summary>
        /// Všechny sváteční dny, které nepřipadají na neděli
        /// </summary>
        public List<DateTime> NonSundayHolidays { get; private set; }

        public PublicHolidaysCalendar(int startYear, int endYear)
        {
            AllHolidays = new List<DateTime>();
            for (int year = startYear; year <= endYear; year++)
            {
                var (goodFriday, easterMonday) = GetEasterHolidays(year);
                AllHolidays.Add(new DateTime(year, 1, 1));
                AllHolidays.Add(goodFriday);
                AllHolidays.Add(easterMonday);
                AllHolidays.Add(new DateTime(year, 5, 1));
                AllHolidays.Add(new DateTime(year, 5, 8));
                AllHolidays.Add(new DateTime(year, 7, 5));
                AllHolidays.Add(new DateTime(year, 7, 6));
                AllHolidays.Add(new DateTime(year, 9, 28));
                AllHolidays.Add(new DateTime(year, 10, 28));
                AllHolidays.Add(new DateTime(year, 11, 17));
                AllHolidays.Add(new DateTime(year, 12, 24));
                AllHolidays.Add(new DateTime(year, 12, 25));
                AllHolidays.Add(new DateTime(year, 12, 26));
            }

            NonSundayHolidays = new List<DateTime>(AllHolidays.Where(d => d.DayOfWeek != DayOfWeek.Sunday));
        }

        public IEnumerable<DateTime> GetAllHolidaysBetween(DateTime startDate, DateTime endDate)
        {
            return AllHolidays.Where(dt => dt >= startDate && dt <= endDate);
        }

        /// <summary>
        /// Vrátí, podle kterého provozního dne se v dané datum má jezdit.
        /// </summary>
        /// <param name="date">Datum</param>
        /// <returns>Provozní den v týdnu</returns>
        public DayOfWeek GetDayOfWeekFor(DateTime date)
        {
            if (NonSundayHolidays.Contains(date))
            {
                return DayOfWeek.Sunday;
            }
            else
            {
                return date.DayOfWeek;
            }
        }

        /// <summary>
        /// Vrací true, pokud jde o pracovní den, a to i se započtením svátků.
        /// </summary>
        /// <param name="date">Datum</param>
        public bool IsWorkday(DateTime date)
        {
            var dayOfWeek = GetDayOfWeekFor(date);
            return IsWorkday(dayOfWeek);
        }

        /// <summary>
        /// Jednoduše na pondělí až pátek vrací true, jinak false
        /// </summary>
        /// <param name="dayOfWeek">Den v týdu</param>
        public static bool IsWorkday(DayOfWeek dayOfWeek)
        {
            return dayOfWeek == DayOfWeek.Monday || dayOfWeek == DayOfWeek.Tuesday || dayOfWeek == DayOfWeek.Wednesday
                || dayOfWeek == DayOfWeek.Thursday || dayOfWeek == DayOfWeek.Friday;
        }


        private static (DateTime GoodFriday, DateTime EasterMonday) GetEasterHolidays(int year)
        {
            // Výpočet Velikonoc (neděle) - Anonymous Gregorian Algorithm
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;

            int month = (h + l - 7 * m + 114) / 31;      // 3 = březen, 4 = duben
            int day = ((h + l - 7 * m + 114) % 31) + 1;   // Den v měsíci

            DateTime easterSunday = new DateTime(year, month, day);

            DateTime goodFriday = easterSunday.AddDays(-2);
            DateTime easterMonday = easterSunday.AddDays(1);

            return (goodFriday, easterMonday);
        }

    }
}
