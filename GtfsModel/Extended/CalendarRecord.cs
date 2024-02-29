using CommonLibrary;
using GtfsModel.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Kalendář jízd spoje - od kdy do kdy a které dny v týdnu. Záznam v calendar.txt (rozšíření <see cref="GtfsCalendarRecord"/>)
    /// </summary>
    public class CalendarRecord
    {
        /// <summary>
        /// ID v rámci GTFS feedu
        /// </summary>
        public string GtfsId { get; set; }

        /// <summary>
        /// Indexováno pomocí <see cref="DayOfWeek"/>. 7 položek, pro každý den v týdnu určujících,
        /// jestli spoj v tento den v týdnu jezdí.
        /// </summary>
        public bool[] InService { get; private set; } // indexed by System.DayOfWeek

        /// <summary>
        /// První den platnosti JŘ spoje.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Poslední den platnosti JŘ spoje.
        /// </summary>
        public DateTime EndDate { get; set; }

        public bool Monday { get { return OperatesOn(DayOfWeek.Monday); } set { SetOperatesOn(DayOfWeek.Monday, value); } }

        public bool Tuesday { get { return OperatesOn(DayOfWeek.Tuesday); } set { SetOperatesOn(DayOfWeek.Tuesday, value); } }

        public bool Wednesday { get { return OperatesOn(DayOfWeek.Wednesday); } set { SetOperatesOn(DayOfWeek.Wednesday, value); } }

        public bool Thursday { get { return OperatesOn(DayOfWeek.Thursday); } set { SetOperatesOn(DayOfWeek.Thursday, value); } }

        public bool Friday { get { return OperatesOn(DayOfWeek.Friday); } set { SetOperatesOn(DayOfWeek.Friday, value); } }

        public bool Saturday { get { return OperatesOn(DayOfWeek.Saturday); } set { SetOperatesOn(DayOfWeek.Saturday, value); } }

        public bool Sunday { get { return OperatesOn(DayOfWeek.Sunday); } set { SetOperatesOn(DayOfWeek.Sunday, value); } }

        /// <summary>
        /// Počet dní, které kalendář pokrývá (<see cref="EndDate"/> - <see cref="StartDate"/> + 1).
        /// </summary>
        public int DaysLength
        {
            get
            {
                return (EndDate - StartDate).Days + 1;
            }
        }

        /// <summary>
        /// Vrátí všechny dny, kdy spoj jede
        /// </summary>
        public IEnumerable<DateTime> ListDates()
        {
            for (var dt = StartDate; dt <= EndDate; dt = dt.AddDays(1))
            {
                if (OperatesAt(dt))
                    yield return dt;
            }
        }

        /// <summary>
        /// Spoje, které jedou podle tohoto kalendáře
        /// </summary>
        public IList<Trip> Trips { get; set; }

        // indexováno indexem dne od začátku exportu
        public Dictionary<DateTime, CalendarExceptionRecord> Exceptions { get; set; }

        public IEnumerable<CalendarExceptionRecord> ExceptionsOrdered { get { return Exceptions.Values.OrderBy(e => e.Date); } }

        public CalendarRecord()
        {
            InService = new bool[7];
            Exceptions = new Dictionary<DateTime, CalendarExceptionRecord>();
            Trips = new List<Trip>();
        }

        /// <summary>
        /// Vrací true, pokud spoj jede v dané datum
        /// </summary>
        /// <param name="date">Datum</param>
        /// <param name="dayOfWeek">Den v týdnu</param>
        /// <returns>True, pokud jede, jinak false</returns>
        public bool OperatesAt(DateTime date)
        {
            if (date < StartDate || date > EndDate)
            {
                return false;
            }

            return OperatesAtIgnoreStartEnd(date);
        }

        // Vrací true, pokud spoj jede v dané datum bez ohledu na interval platnosti kalendáře
        private bool OperatesAtIgnoreStartEnd(DateTime date)
        {
            var exception = Exceptions.GetValueOrDefault(date);
            if (exception != null)
            {
                return exception.ExceptionType == CalendarExceptionType.Add;
            }

            return OperatesOn(date.DayOfWeek);
        }

        /// <summary>
        /// Vrátí <see cref="InService"/> jako desítkové sedmimístné číslo, kde budou jen 0 a 1.
        /// První cifra pondělí, poslední neděle.
        /// </summary>
        public int ServiceAsBinaryString
        {
            get
            {
                return (Monday ? 1 : 0) * 1000000
                        + (Tuesday ? 1 : 0) * 100000
                        + (Wednesday ? 1 : 0) * 10000
                        + (Thursday ? 1 : 0) * 1000
                        + (Friday ? 1 : 0) * 100
                        + (Saturday ? 1 : 0) * 10
                        + (Sunday ? 1 : 0) * 1;
            }
        }

        /// <summary>
        /// Vrátí <see cref="InService"/> jako binární flagy
        /// </summary>
        public int ServiceAsFlags
        {
            get
            {
                var flags = 0;
                for (int i = 0; i < 7; i++)
                {
                    flags |= (InService[i] ? 1 : 0) << i;
                }

                return flags;
            }
        }

        /// <summary>
        /// Vrací true, pokud spoj obvykle jezdí v daný den v týdnu
        /// </summary>
        /// <param name="dayOfWeek">Den v týdnu</param>
        /// <returns></returns>
        public bool OperatesOn(DayOfWeek dayOfWeek)
        {
            return InService[(int)dayOfWeek];
        }

        /// <summary>
        /// Nastaví hodnotu, zda spoj v daný den v týdnu obvykle jede
        /// </summary>
        /// <param name="dayOfWeek">Den v týdnu</param>
        /// <param name="value">True, pokud jede, jinak false</param>
        public void SetOperatesOn(DayOfWeek dayOfWeek, bool value)
        {
            InService[(int)dayOfWeek] = value;
        }

        /// <summary>
        /// Vrátí true, pokud kalendář zahrnuje tento den v týdnu (den v týdnu se vyskytuje mezi <see cref="StartDate"/> a <see cref="EndDate"/>).
        /// Započítává i sváteční dny dle <see cref="DaysOfWeekCalendar"/>.
        /// 
        /// Návrat false v zásadě říká, že z kalendáře nejde poznat, jestli spoj v daný den jede, nebo ne.
        /// </summary>
        public bool IsDefinedOn(DayOfWeek dayOfWeek, DaysOfWeekCalendar daysOfWeekCalendar)
        {
            for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
            {
                var day = daysOfWeekCalendar.GetDayOfWeekFor(date);
                if (day == dayOfWeek)
                    return true;
            }

            return false;
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
        /// Vrátí bitmapu, kdy spoj jede
        /// </summary>
        public ServiceDaysBitmap AsServiceBitmap()
        {
            return AsServiceBitmap(StartDate);
        }

        /// <summary>
        /// Vrátí bitmapu, kdy spoj jede
        /// </summary>
        /// <param name="startDate">Počáteční datum, ke kterému se má bitmapa vztahovat</param>
        public ServiceDaysBitmap AsServiceBitmap(DateTime startDate)
        {
            var length = Math.Max((EndDate - startDate).Days + 1, 0);
            var result = new bool[length];
            for (int i = 0; i < length; i++)
            {
                var date = startDate.AddDays(i);
                result[i] = OperatesAt(date);
            }

            return new ServiceDaysBitmap(result);
        }

        /// <summary>
        /// Porovná reálné dny, kdy spoj jede, s tím, co je v kalendáři a podle toho nastaví výjimky. Počítá se s tím, že bitmapa pokrývá
        /// dny počínaje StartDate a konče EndDate
        /// </summary>
        /// <param name="bitmap">Bitmapa kdy spoj jede (od StartDate do EndDate)</param>
        public void IncorporateExceptions(ServiceDaysBitmap bitmap)
        {
            if (bitmap.Length != DaysLength)
            {
                throw new ArgumentException("Bitmap must have proper size according to EndDate - StartDate.");
            }

            Exceptions.Clear();
            for (int i = 0; i < bitmap.Length; i++)
            {
                var date = StartDate.AddDays(i);
                var isInService = OperatesOn(date.DayOfWeek);
                if (isInService && !bitmap[i])
                {
                    AddException(date, CalendarExceptionType.Remove);
                }
                else if (!isInService && bitmap[i])
                {
                    AddException(date, CalendarExceptionType.Add);
                }
            }
        }

        /// <summary>
        /// Vytvoří instanci kalendáře z GTFS záznamu.
        /// </summary>
        /// <param name="gtfsCalendar">GTFS záznam</param>
        public static CalendarRecord Construct(GtfsCalendarRecord gtfsCalendar)
        {
            var result = new CalendarRecord()
            {
                StartDate = gtfsCalendar.StartDate,
                EndDate = gtfsCalendar.EndDate,
                GtfsId = gtfsCalendar.Id,
            };

            result.InService[(int)DayOfWeek.Monday] = gtfsCalendar.Monday;
            result.InService[(int)DayOfWeek.Tuesday] = gtfsCalendar.Tuesday;
            result.InService[(int)DayOfWeek.Wednesday] = gtfsCalendar.Wednesday;
            result.InService[(int)DayOfWeek.Thursday] = gtfsCalendar.Thursday;
            result.InService[(int)DayOfWeek.Friday] = gtfsCalendar.Friday;
            result.InService[(int)DayOfWeek.Saturday] = gtfsCalendar.Saturday;
            result.InService[(int)DayOfWeek.Sunday] = gtfsCalendar.Sunday;
            return result;
        }

        /// <summary>
        /// Převede na GTFS záznam
        /// </summary>
        public GtfsCalendarRecord ToGtfsCalendar()
        {
            return new GtfsCalendarRecord()
            {
                Id = GtfsId,
                Monday = Monday,
                Tuesday = Tuesday,
                Wednesday = Wednesday,
                Thursday = Thursday,
                Friday = Friday,
                Saturday = Saturday,
                Sunday = Sunday,
                StartDate = StartDate,
                EndDate = EndDate,
            };
        }

        /// <summary>
        /// Vrátí všechny GTFS záznamy výjimek v kalendáři
        /// </summary>
        public IEnumerable<GtfsCalendarDate> GetAllGtfsExceptions()
        {
            foreach (var exception in Exceptions.Values)
            {
                yield return exception.ToGtfsCalendarDate(GtfsId);
            }
        }

        public override string ToString()
        {
            return $"{ServiceAsBinaryString}: {StartDate:d.M.yyyy}-{EndDate:d.M.yyyy}";
        }
    }
}
