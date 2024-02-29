using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLibrary
{
    // TODO deprekováno, používá se už jen ve StopTimeGenu pro vláčky (je to tam přesně k něčemu?)

    public class DaysOfWeekCalendars
    {
        /// <summary>
        /// Výjimky - výhledově importovat odněkud?
        /// </summary>
        private static readonly IDictionary<DateTime, DayOfWeek> DayExceptions = new Dictionary<DateTime, DayOfWeek>()
        {
            { new DateTime(2019, 10, 28), DayOfWeek.Sunday },
            { new DateTime(2019, 12, 24), DayOfWeek.Saturday },
            { new DateTime(2019, 12, 25), DayOfWeek.Sunday },
            { new DateTime(2019, 12, 26), DayOfWeek.Sunday },
            { new DateTime(2020, 1, 1), DayOfWeek.Sunday },
            { new DateTime(2020, 4, 9), DayOfWeek.Friday },
            { new DateTime(2020, 4, 10), DayOfWeek.Saturday },
            { new DateTime(2020, 4, 13), DayOfWeek.Sunday },
            { new DateTime(2020, 4, 30), DayOfWeek.Friday },
            { new DateTime(2020, 5, 1), DayOfWeek.Saturday },
            { new DateTime(2020, 5, 7), DayOfWeek.Friday },
            { new DateTime(2020, 5, 8), DayOfWeek.Saturday },
            { new DateTime(2020, 7, 5), DayOfWeek.Sunday },
            { new DateTime(2020, 7, 6), DayOfWeek.Sunday },
            { new DateTime(2020, 9, 28), DayOfWeek.Sunday },
            { new DateTime(2020, 10, 28), DayOfWeek.Sunday },
            { new DateTime(2020, 11, 17), DayOfWeek.Sunday },
            { new DateTime(2020, 12, 23), DayOfWeek.Friday },
            { new DateTime(2020, 12, 24), DayOfWeek.Saturday },
            { new DateTime(2020, 12, 25), DayOfWeek.Sunday },
            { new DateTime(2020, 12, 26), DayOfWeek.Sunday },
            { new DateTime(2021, 1, 1), DayOfWeek.Sunday },
            { new DateTime(2021, 4, 2), DayOfWeek.Sunday },
            { new DateTime(2021, 4, 5), DayOfWeek.Sunday },
            { new DateTime(2021, 7, 5), DayOfWeek.Sunday },
            { new DateTime(2021, 7, 6), DayOfWeek.Sunday },
            { new DateTime(2021, 9, 28), DayOfWeek.Sunday },
            { new DateTime(2021, 10, 28), DayOfWeek.Sunday },
            { new DateTime(2021, 11, 17), DayOfWeek.Sunday },
            { new DateTime(2021, 12, 24), DayOfWeek.Sunday },
            { new DateTime(2022, 1, 1), DayOfWeek.Sunday },
            { new DateTime(2022, 4, 15), DayOfWeek.Sunday },
            { new DateTime(2022, 4, 18), DayOfWeek.Sunday },
            { new DateTime(2022, 7, 5), DayOfWeek.Sunday },
            { new DateTime(2022, 7, 6), DayOfWeek.Sunday },
            { new DateTime(2022, 9, 28), DayOfWeek.Sunday },
            { new DateTime(2022, 10, 28), DayOfWeek.Sunday },
            { new DateTime(2022, 11, 17), DayOfWeek.Sunday },
            { new DateTime(2022, 12, 26), DayOfWeek.Sunday },
            { new DateTime(2023, 1, 1), DayOfWeek.Sunday },
            { new DateTime(2023, 4, 7), DayOfWeek.Sunday },
            { new DateTime(2023, 4, 10), DayOfWeek.Sunday },
            { new DateTime(2023, 5, 1), DayOfWeek.Sunday },
            { new DateTime(2023, 5, 8), DayOfWeek.Sunday },
            { new DateTime(2023, 7, 5), DayOfWeek.Sunday },
            { new DateTime(2023, 7, 6), DayOfWeek.Sunday },
            { new DateTime(2023, 9, 28), DayOfWeek.Sunday },
            { new DateTime(2023, 10, 28), DayOfWeek.Sunday },
            { new DateTime(2023, 11, 17), DayOfWeek.Sunday },
            { new DateTime(2023, 12, 25), DayOfWeek.Sunday },
            { new DateTime(2023, 12, 26), DayOfWeek.Sunday },
            { new DateTime(2024, 1, 1), DayOfWeek.Sunday },
            { new DateTime(2024, 3, 29), DayOfWeek.Sunday },
            { new DateTime(2024, 4, 1), DayOfWeek.Sunday },
            { new DateTime(2024, 5, 1), DayOfWeek.Sunday },
            { new DateTime(2024, 5, 8), DayOfWeek.Sunday },
            { new DateTime(2024, 7, 5), DayOfWeek.Sunday },
            { new DateTime(2024, 7, 6), DayOfWeek.Sunday },
            { new DateTime(2024, 9, 28), DayOfWeek.Sunday },
            { new DateTime(2024, 10, 28), DayOfWeek.Sunday },
            { new DateTime(2024, 11, 17), DayOfWeek.Sunday },
            { new DateTime(2024, 12, 26), DayOfWeek.Sunday },
            { new DateTime(2025, 1, 1), DayOfWeek.Sunday },
            { new DateTime(2025, 4, 18), DayOfWeek.Sunday },
            { new DateTime(2025, 4, 21), DayOfWeek.Sunday },
            { new DateTime(2025, 5, 1), DayOfWeek.Sunday },
            { new DateTime(2025, 5, 8), DayOfWeek.Sunday },
            { new DateTime(2025, 7, 5), DayOfWeek.Sunday },
            { new DateTime(2025, 7, 6), DayOfWeek.Sunday },
            { new DateTime(2025, 9, 28), DayOfWeek.Sunday },
            { new DateTime(2025, 10, 28), DayOfWeek.Sunday },
            { new DateTime(2025, 11, 17), DayOfWeek.Sunday },
            { new DateTime(2025, 12, 26), DayOfWeek.Sunday },
        };

        /// <summary>
        /// Po aktualizaci na nová data je potřeba posunout tuto konstantu
        /// </summary>
        private static DateTime MaxDate = new DateTime(2025, 12, 31);

        ///// <summary>
        ///// Instance pro MHD + příměstské busy
        ///// </summary>
        //public static DaysOfWeekCalendar PIDInstance = new DaysOfWeekCalendar(DayExceptions, MaxDate);

        /// <summary>
        /// U vlaků je trochu jiné zacházení, tam bereme jen ty výjimky, kde se z pracovního dne dělá víkend, na transformace typu
        /// čtvrtek => pátek nebo neděle => sobota se kašle, protože ty vlaky si stejně jezdí, jak chtějí, takže se to pak vyřeší konkrétními poznámkami.
        /// </summary>
        public static DaysOfWeekCalendar TrainsInstance = new DaysOfWeekCalendar(
            DayExceptions.Where(
                e => DaysOfWeekCalendar.IsWorkday(e.Key.DayOfWeek) && !DaysOfWeekCalendar.IsWorkday(e.Value)).ToDictionary(e => e.Key, e => e.Value),
            MaxDate);
    }

    /// <summary>
    /// Zajišťuje převod datumu do provozního dne se započítáním svátků dle kalendáře PID
    /// </summary>
    public class DaysOfWeekCalendar
    {
        /// <summary>
        /// Výjimky
        /// </summary>
        public IDictionary<DateTime, DayOfWeek> DayExceptions { get; private set; }

        /// <summary>
        /// Výjimky, které reálně přepisují nějaký den v týdnu (vynechává přepisy typu neděle => neděle)
        /// </summary>
        public IDictionary<DateTime, DayOfWeek> NonredundantDayExceptions
        {
            get { return DayExceptions.Where(e => e.Key.DayOfWeek != e.Value).ToDictionary(e => e.Key, e => e.Value); }
        }

        /// <summary>
        /// Do kdy je garantována přítomnost výjimek (od tohoto data už ověřování datumu nebude fungovat)
        /// </summary>
        public DateTime MaxDate { get; private set; }

        public DaysOfWeekCalendar(IDictionary<DateTime, DayOfWeek> dayExceptions, DateTime maxDate)
        {
            DayExceptions = dayExceptions;
            MaxDate = maxDate;
        }

        //public static string WeekendString = "24. až 26. 12. 2019, 1. 1., 10. 4., 13. 4., 1. 5., 8. 5., 6. 7., 28. 9., 28. 10. a 17. 11. 2020 jede podle víkendového jízdního řádu.";
        //public static string SaturdayString_ = "24. 12. 2019, 10. 4., 1. 5. a 8. 5. 2020 jede podle sobotního jízdního řádu.";
        //public static string SundayString_ = "25. 12. a 26. 12. 2019, 1. 1., 13. 4., 6. 7., 28. 9., 28. 10. a 17. 11. jede podle nedělního jízdního řádu.";

        
        /// <summary>
        /// Vrátí, podle kterého provozního dne se v dané datum má jezdit.
        /// </summary>
        /// <param name="date">Datum</param>
        /// <returns>Provozní den v týdnu</returns>
        public DayOfWeek GetDayOfWeekFor(DateTime date)
        {
            if (date > MaxDate)
            {
                throw new ArgumentException($"Date must be lower than MaxDate ({MaxDate})");
            }

            if (DayExceptions.ContainsKey(date))
            {
                return DayExceptions[date];
            }
            else
            {
                return date.DayOfWeek;
            }
        }

        /// <summary>
        /// Vrací true, pokud jde o pracovní den, a to i se započtením svátků.
        /// Funguje i pro vlaky vzhledem k tomu, jak jsou <see cref="DayExceptionsForTrains"/> konstruovány 
        /// (zachovávají se práv transformace pracovní den => víkend a transformace víkend => pracovní den by neměly existovat)
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
        
        /// <summary>
        /// Vrátí dny v daném rozmezí, které se mění na některý z uvedených vseznamu <paramref name="targetDaysOfWeek"/>
        /// </summary>
        /// <param name="targetDysOfWeek">Provozní den, na který hledáme změny</param>
        /// <param name="startDate">Počátek období</param>
        /// <param name="endDate">Konec období</param>
        public IEnumerable<DateTime> ListDatesWithChanges(DayOfWeek[] targetDysOfWeek, DateTime startDate, DateTime endDate)
        {
            if (endDate > MaxDate)
            {
                throw new ArgumentException($"Date must be lower than MaxDate ({MaxDate})");
            }

            return NonredundantDayExceptions.Where(e => e.Key >= startDate && e.Key <= endDate && targetDysOfWeek.Contains(e.Value)).Select(e => e.Key).ToArray();
        }
    }
}
