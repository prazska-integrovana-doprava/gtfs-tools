using CommonLibrary.DotNet48;
using GtfsModel.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Reprezentuje "prázdný" kalendář, tzn. ten, který nemá záznam v calendar.txt, ale pouze v calendar_dates.txt.
    /// 
    /// Obsahuje tedy v zásadě pouze seznam "výjimek", tj. dnů, kdy spoj jede.
    /// 
    /// Kalendáře, které mají i záznam v calendar.txt (tzn. jsou definovány počátkem, koncem a dny v týdnu) reprezentuje třída <see cref="CalendarRecord"/>.
    /// </summary>
    public class BaseCalendarRecord
    {
        /// <summary>
        /// ID v rámci GTFS feedu
        /// </summary>
        public string GtfsId { get; set; }

        /// <summary>
        /// Spoje, které jedou podle tohoto kalendáře
        /// </summary>
        public IList<Trip> Trips { get; set; }

        /// <summary>
        /// Výjimky v kalendáři (pevné nastavení datumu)
        /// </summary>
        public Dictionary<DateTime, CalendarExceptionRecord> Exceptions { get; set; }

        /// <summary>
        /// True, pokud není žádný aktivní den kalendáře, jinak false
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return !ListDates().Any();
            }
        }

        public BaseCalendarRecord()
        {
            Exceptions = new Dictionary<DateTime, CalendarExceptionRecord>();
            Trips = new List<Trip>();
        }

        /// <summary>
        /// Vrátí všechny dny, kdy spoj jede
        /// </summary>
        public virtual IEnumerable<DateTime> ListDates()
        {
            // nejdřív výjimky před začátkem platnosti kalendáře
            foreach (var exception in Exceptions.Values.Where(e => e.ExceptionType == CalendarExceptionType.Add))
            {
                yield return exception.Date;
            }
        }

        /// <summary>
        /// Vrátí první den, kdy spoj jede. Vrací null, pokud spoj nejede nikdy.
        /// </summary>
        public DateTime? GetFirstDayOfService()
        {
            return ListDates().OrderBy(d => d).FirstOrDefault();
        }

        /// <summary>
        /// Vrátí poslední den, kdy spoj jede. Vrací null, pokud spoj nejede nikdy.
        /// </summary>
        public DateTime? GetLastDayOfService()
        {
            return ListDates().OrderByDescending(d => d).FirstOrDefault();
        }

        /// <summary>
        /// Vrací true, pokud spoj jede v dané datum
        /// </summary>
        /// <param name="date">Datum</param>
        /// <returns>True, pokud jede, jinak false</returns>
        public virtual bool OperatesAt(DateTime date)
        {
            var exception = Exceptions.GetValueOrDefault(date);
            if (exception != null)
            {
                return exception.ExceptionType == CalendarExceptionType.Add;
            }

            return false;
        }

        /// <summary>
        /// Odstraní z kalendáře všechny dny v zadaném intervalu.
        /// </summary>
        public virtual void ShortenBy(DateTime dateFrom, DateTime dateTo)
        {
            for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
            {
                if (Exceptions.ContainsKey(date))
                {
                    Exceptions.Remove(date);
                }
            }
        }

        /// <summary>
        /// Vrátí všechny dny, které má společné s kalendářem <paramref name="other"/>
        /// </summary>
        public IEnumerable<DateTime> IntersectDatesWith(BaseCalendarRecord other)
        {
            var otherDates = new HashSet<DateTime>(other.ListDates());
            foreach (var day in ListDates())
            {
                if (otherDates.Contains(day))
                    yield return day;
            }
        }

        /// <summary>
        /// Přidá výjimku do kalendáře
        /// </summary>
        /// <param name="date">Datum</param>
        /// <param name="exceptionType">Typ výjimky (jede/nejede)</param>
        public void AddException(DateTime date, CalendarExceptionType exceptionType)
        {
            Exceptions.Add(date, new CalendarExceptionRecord()
            {
                Date = date,
                ExceptionType = exceptionType,
            });
        }

        /// <summary>
        /// Vrátí všechny GTFS záznamy výjimek v kalendáři
        /// </summary>
        public IEnumerable<GtfsCalendarDate> GetAllGtfsExceptions()
        {
            foreach (var exception in Exceptions.Values.OrderBy(v => v.Date))
            {
                yield return exception.ToGtfsCalendarDate(GtfsId);
            }
        }

        public override string ToString()
        {
            return GtfsId;
        }
    }
}
