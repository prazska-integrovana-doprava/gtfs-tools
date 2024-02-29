using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Časová poznámka (např. "nejede v pátek a od x.y.")
    /// </summary>
    class TimeRemark : IRemark
    {
        /// <summary>
        /// Obyčejný interval od-do
        /// </summary>
        public class DateInterval
        {
            public DateTime From { get; set; }

            public DateTime RealFrom { get { return IsNight ? From.AddDays(1) : From; } }

            public DateTime To { get; set; }

            public DateTime RealTo { get { return IsNight ? To.AddDays(1) : To; } }

            public bool IsNight { get; protected set; }

            public bool IsMorning { get; protected set; }

            public virtual bool IsFromForever { get { return From == DateTime.MinValue; } }

            public virtual bool IsToForever { get { return To == DateTime.MaxValue; } }

            public DateInterval(bool isNight, bool isMorning)
            {
                IsNight = isNight;
                IsMorning = isMorning;
            }

            public override bool Equals(object obj)
            {
                var other = obj as DateInterval;
                if (other == null)
                    return false;

                return From == other.From && To == other.To;
            }

            public override int GetHashCode()
            {
                return From.GetHashCode() + To.GetHashCode() * 19;
            }

            public override string ToString()
            {
                var from = RealFrom;
                var to = RealTo;
                if (IsNight || IsMorning)
                {
                    return ToStringAsNight(from, to);
                }

                // uvnitř datumů se používají non-breakable mezery, aby se poznámky nelámaly uprostřed
                if (from == to)
                {
                    return $"{from:d. M.}";
                }
                else if (from.Month == to.Month && from.Year == to.Year)
                {
                    return $"od {from:d.} do {to:d. M.}";
                }
                else if (from.Year == to.Year)
                {
                    return $"od {from:d. M.} do {to:d. M.}";
                }
                else
                {
                    return $"od {from:d. M. yyyy} do {to:d. M. yyyy}";
                }
            }

            protected virtual string ToStringAsNight(DateTime from, DateTime to)
            {
                // uvnitř datumů se používají non-breakable mezery, aby se poznámky nelámaly uprostřed
                if (from == to)
                {
                    return $"v noci na {from:d. M.}";
                }
                else if (from.Month == to.Month && from.Year == to.Year)
                {
                    return $"od noci na {from:d.} do noci na {to:d. M.}";
                }
                else if (from.Year == to.Year)
                {
                    return $"od noci na {from:d. M.} do noci na {to:d. M.}";
                }
                else
                {
                    return $"od noci na {from:d. M. yyyy} do noci na {to:d. M. yyyy}";
                }
            }
        }

        /// <summary>
        /// Jeden interval, kdy spoj není v provozu
        /// </summary>
        public class AdjustableDateInterval : DateInterval
        {
            /// <summary>
            /// Nejmenší datum, od kdy lze říci, že spoj nejezdí (jinými slovy den následující po posledním dnu, kdy spoj ještě jede).
            /// Toto logicky nesmí být vyšší než <see cref="From"/>, ale v případě spojů, které nejezdí celotýdenně, může být nižší.
            /// (např. spoj jedoucí o víkendu nejede od soboty 3.9., tedy se v případě potřeby dá říct, že nejede už od pondělí 29.8.,
            /// pokud bychom chtěli stejnou poznámku použít třeba pro všednodenní spoje).
            /// 
            /// Tuto hodnotu potřebujeme znát kvůli merge poznámek, pro výpis však používáme hodnotu <see cref="From"/>, která vyjadřuje první den, kdy spoj nejede.
            /// </summary>
            public DateTime MinimumFrom { get; set; }

            /// <summary>
            /// Nejvyšší datum, do kdy lze říci, že spoj nejezdí (analogie <see cref="MinimumFrom"/>).
            /// </summary>
            public DateTime MaximumTo { get; set; }

            public override bool IsFromForever { get { return MinimumFrom == DateTime.MinValue; } }

            public override bool IsToForever { get { return MaximumTo == DateTime.MaxValue; } }

            public AdjustableDateInterval(bool isNight, bool isMorning)
                : base(isNight, isMorning)
            {
            }

            /// <summary>
            /// Intervaly jsou identické, pokud lze jeden vnořit do druhého, tj. lze prodloužit <see cref="From"/> - <see cref="To"/> tak, že ještě nepřesáhnou
            /// <see cref="MinimumFrom"/> - <see cref="MaximumTo"/>, které pevně ohraničují, kdy ještě spoj nejede.
            /// 
            /// x.IsIdenticalTo(y) vrací stejný výsledek jako y.IsIdentical(x)
            /// </summary>
            /// <param name="other">Druhý interval</param>
            public bool IsIdenticalTo(AdjustableDateInterval other)
            {
                return (other.From == From
                    || other.From < From && other.From >= MinimumFrom
                    || From < other.From && From >= other.MinimumFrom)
                    &&
                    (other.To == To
                    || other.To > To && other.To <= MaximumTo
                    || To > other.To && To <= other.MaximumTo);
            }

            public override string ToString()
            {
                var from = From;
                var to = To;
                if (IsNight)
                {
                    from = from.AddDays(1);
                    to = to.AddDays(1);
                }

                if (IsNight || IsMorning)
                {
                    return ToStringAsNight(from, to);
                }

                if (!IsFromForever && !IsToForever)
                {
                    return base.ToString();
                }
                else if (!IsFromForever && IsToForever)
                {
                    return $"od {from:d. M.}";
                }
                else if (IsFromForever && !IsToForever)
                {
                    return $"do {to:d. M.}";
                }
                else
                {
                    // nejede vůbec? není to divný?
                    return "vůbec";
                }
            }

            protected override string ToStringAsNight(DateTime from, DateTime to)
            {
                if (!IsFromForever && !IsToForever)
                {
                    return base.ToStringAsNight(from, to);
                }
                else if (!IsFromForever && IsToForever)
                {
                    return $"od noci na {from:d. M.}";
                }
                else if (IsFromForever && !IsToForever)
                {
                    return $"do noci na {to:d. M.}";
                }
                else
                {
                    // nejede vůbec? není to divný?
                    return "vůbec";
                }
            }
        }

        /// <summary>
        /// Dny v týdnu, kdy spoj nejede. Nejsou zahrnuty dny v týdnu, které daný provozní den vůbec nereprezentuje.
        /// </summary>
        public List<DayOfWeek> DaysOfWeekNoService { get; private set; }
        
        /// <summary>
        /// Dny v týdnu, kdy spoj jede.
        /// </summary>
        public List<DayOfWeek> DaysOfWeekWithService { get; private set; }

        /// <summary>
        /// Intervaly, kdy spoj není v provozu v rámci dnů <see cref="DaysOfWeekWithService"/>.
        /// </summary>
        public List<AdjustableDateInterval> DaysNoService { get; private set; }
        
        /// <summary>
        /// Všechny intervaly, kdy spoj je v provozu.
        /// </summary>
        public List<DateInterval> DaysWithService { get; private set; }

        /// <summary>
        /// Dny, kdy spoj také jede, které nejsou pokryty pomocí <see cref="DaysOfWeekWithService"/>.
        /// </summary>
        public List<DateTime> DaysWithExtraService { get; private set; }

        private Dictionary<DayOfWeek, string> weekdayNames = new Dictionary<DayOfWeek, string>()
        {
            { DayOfWeek.Monday, "pondělí" },
            { DayOfWeek.Tuesday, "úterý" },
            { DayOfWeek.Wednesday, "středu" },
            { DayOfWeek.Thursday, "čtvrtek" },
            { DayOfWeek.Friday, "pátek" },
            { DayOfWeek.Saturday, "sobotu" },
            { DayOfWeek.Sunday, "neděli" },
        };

        private Dictionary<DayOfWeek, string> weekdayPrepositions = new Dictionary<DayOfWeek, string>()
        {
            { DayOfWeek.Monday, "v" },
            { DayOfWeek.Tuesday, "v" },
            { DayOfWeek.Wednesday, "ve" },
            { DayOfWeek.Thursday, "ve" },
            { DayOfWeek.Friday, "v" },
            { DayOfWeek.Saturday, "v" },
            { DayOfWeek.Sunday, "v" },
        };

        public TimeRemark()
        {
            DaysOfWeekNoService = new List<DayOfWeek>();
            DaysOfWeekWithService = new List<DayOfWeek>();
            DaysNoService = new List<AdjustableDateInterval>();
            DaysWithService = new List<DateInterval>();
            DaysWithExtraService = new List<DateTime>();
        }

        /// <summary>
        /// Označení poznámky v JŘ
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// True, pokud poznámka reálně neobsahuje žádné datumové ani časové omezení
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return !DaysOfWeekNoService.Any() && !DaysNoService.Any() && !IsEarlyMorning;
            }
        }

        /// <summary>
        /// True, pokud poznámka vzešla sloučením více různých poznámek, u nichž se lišila
        /// část <see cref="DaysOfWeekWithService"/> a <see cref="DaysWithService"/>.
        /// Říká jen, že musím poznámku vypsat v negativní formě, protože na tu pozitivní není spoleh
        /// </summary>
        public bool IsPositivePartMismatched { get; set; }

        /// <summary>
        /// Jde o poznámku k nočnímu spoji (nemůže se mixovat s denními) - noční spoje jsou ty, co vyjíždí od 0:00 do 0:50
        /// </summary>
        public bool IsNight { get; set; }

        /// <summary>
        /// Jde o poznámku k brzce rannímu spoji - to jsou ty, co jedou do 3:00 (ale nejsou noční)
        /// </summary>
        public bool IsEarlyMorning { get; set; }

        public bool HasNegativeDaysOfWeek { get { return DaysOfWeekNoService.Any(); } }

        public bool HasNegativeDays { get { return DaysNoService.Any(); } }

        public bool IsPositiveDaysOfWeek { get { return HasNegativeDaysOfWeek && DaysOfWeekWithService.Count <= DaysOfWeekNoService.Count && !IsPositivePartMismatched; } }

        public bool IsPositiveDays { get { return HasNegativeDays && DaysWithService.Count < DaysNoService.Count && !IsPositivePartMismatched; } }

        /// <summary>
        /// Text poznámky ve vysvětlivkách
        /// </summary>
        public string Text
        {
            get
            {
                var daysOfWeekNoStr = PrintDaysOfWeekList(DaysOfWeekNoService);
                var daysOfWeekYesStr = PrintDaysOfWeekList(DaysOfWeekWithService);
                var daysNoStr = PrintDaysList(DaysNoService);
                var daysYesStr = PrintDaysList(DaysWithService);
                var daysExtraStr = DaysWithExtraService.Any() ? $" Jede také {PrintDaysList(DaysWithExtraService)}." : "";
                var earlyMorningStr = IsEarlyMorning ? "Jede o nocích před sobotou, nedělí a svátkem. " : "";

                if (HasNegativeDaysOfWeek && HasNegativeDays)
                {
                    // omezení na dny v týdnu i datum
                    if (IsPositiveDaysOfWeek && IsPositiveDays)
                    {
                        return $"{earlyMorningStr}Jede jen {daysYesStr}.";
                    }
                    else if (IsPositiveDaysOfWeek)
                    {
                        var daysNoStrSimpl = PrintDaysList(DaysNoService.Where(d => d.From != d.To || DaysOfWeekWithService.Contains(d.From.DayOfWeek)));
                        if (!string.IsNullOrEmpty(daysNoStrSimpl))
                            return $"{earlyMorningStr}Jede jen {daysOfWeekYesStr}, nejede {daysNoStrSimpl}.{daysExtraStr}";
                        else
                            return $"{earlyMorningStr}Jede jen {daysOfWeekYesStr}.{daysExtraStr}";
                    }
                    else if (IsPositiveDays)
                    {
                        return $"{earlyMorningStr}Jede jen {daysYesStr}, nejede {daysOfWeekNoStr}.{daysExtraStr}";
                    }
                    else
                    {
                        return $"{earlyMorningStr}Nejede {daysOfWeekNoStr}, {daysNoStr}.{daysExtraStr}";
                    }
                }
                else if (HasNegativeDaysOfWeek)
                {
                    // omezení jen na dny v týdnu
                    if (IsPositiveDaysOfWeek)
                    {
                        return $"{earlyMorningStr}Jede jen {daysOfWeekYesStr}.{daysExtraStr}";
                    }
                    else
                    {
                        return $"{earlyMorningStr}Nejede {daysOfWeekNoStr}.{daysExtraStr}";
                    }
                }
                else if (HasNegativeDays)
                {
                    // omezení jen na datum
                    if (IsPositiveDays)
                    {
                        return $"{earlyMorningStr}Jede jen {daysYesStr}.";
                    }
                    else
                    {
                        return $"{earlyMorningStr}Nejede {daysNoStr}.{daysExtraStr}";
                    }
                }
                else
                {
                    return $"{earlyMorningStr}.{daysExtraStr}";
                }
            }
        }

        private string PrintDaysOfWeekList(IEnumerable<DayOfWeek> dayOfWeekList)
        {
            if (!dayOfWeekList.Any())
                return "";

            if (!IsNight)
            {
                return $"{weekdayPrepositions[dayOfWeekList.First()]} {StringHelper.JoinWithCommaAndAnd(dayOfWeekList.Select(wd => weekdayNames[wd]))}";
            }
            else
            {
                var dayOfWeekListPlus1 = dayOfWeekList.Select(d => (DayOfWeek)((int)(d + 1) % 7));
                return $"v noci na {StringHelper.JoinWithCommaAndAnd(dayOfWeekListPlus1.Select(wd => weekdayNames[wd]))}";
            }
        }

        private string PrintDaysList(IEnumerable<DateInterval> dateIntervals)
        {
            if (!dateIntervals.Any())
                return "";

            var dateIntervalsArr = dateIntervals.ToArray();
            var result = new StringBuilder();
            int currentYear = dateIntervalsArr.First().RealFrom.Year;
            for (int i = 0; i < dateIntervalsArr.Length; i++)
            {
                var currentInterval = dateIntervalsArr[i];
                var nextInterval = i + 1 < dateIntervalsArr.Length ? dateIntervalsArr[i + 1] : null;
                if (nextInterval == null || currentYear != nextInterval.RealFrom.Year)
                {
                    if (i > 0)
                        result.Append(" a ");

                    currentYear = currentInterval.IsToForever ? currentInterval.RealFrom.Year : currentInterval.RealTo.Year;
                    if (currentInterval.RealFrom.Year == currentInterval.RealTo.Year || currentInterval.IsFromForever || currentInterval.IsToForever)
                    {
                        result.Append($"{currentInterval} {currentYear}");
                    }
                    else
                    {
                        result.Append($"{currentInterval}");
                    }

                    if (nextInterval != null)
                        currentYear = nextInterval.RealFrom.Year;
                }
                else
                {
                    if (i > 0)
                        result.Append(", ");
                    result.Append(currentInterval.ToString());
                }
            }

            return result.ToString();
        }

        private string PrintDaysList(IEnumerable<DateTime> dates)
        {
            if (!IsNight)
            {
                return StringHelper.JoinDates(dates);
            }
            else
            {
                return $"o nocích na {StringHelper.JoinDates(dates.Select(d => d.AddDays(1)))}";
            }
        }

        /// <summary>
        /// Poznámky mají identický obsah, pokud reprezentují stejnou negativní množinu dnů v týdnu a pokud intervaly, kdy
        /// spoje nejedou, si 1:1 odpovídají
        /// 
        /// x.IsIdenticalTo(y) vrací stejný výsledek jako y.IsIdenticalTo(x)
        /// </summary>
        /// <param name="other">Druhá poznámka</param>
        public bool IsIdenticalTo(TimeRemark other)
        {
            if (IsNight != other.IsNight)
            {
                return false;
            }

            if (!Enumerable.SequenceEqual(DaysOfWeekNoService, other.DaysOfWeekNoService))
            {
                return false;
            }

            if (DaysNoService.Count != other.DaysNoService.Count)
            {
                return false;
            }

            for (int i = 0; i < DaysNoService.Count; i++)
            {
                if (!DaysNoService[i].IsIdenticalTo(other.DaysNoService[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Zahrne poznámku <paramref name="other"/> do této poznámky. Nutnou podmínkou je, že musí být poznámky identické.
        /// Pokud nutno, tak pouze opravuje hodnoty From a To jednotlivých intervalů.
        /// </summary>
        /// <param name="other">Poznámka, kterou chceme integrovat do této</param>
        public void Include(TimeRemark other)
        {
            if (DaysNoService.Count != other.DaysNoService.Count)
            {
                throw new InvalidOperationException("Sjednocení dvou nestejných poznámek");
            }

            if (other.IsPositivePartMismatched
                || !Enumerable.SequenceEqual(DaysOfWeekWithService, other.DaysOfWeekWithService)
                || !Enumerable.SequenceEqual(DaysWithService, other.DaysWithService))
            {
                IsPositivePartMismatched = true;
            }
            
            for (int i = 0; i < DaysNoService.Count; i++)
            {
                if (other.DaysNoService[i].From < DaysNoService[i].From)
                {
                    DaysNoService[i].From = other.DaysNoService[i].From;
                    if (DaysNoService[i].From < DaysNoService[i].MinimumFrom) throw new InvalidOperationException("Sjednocení From Date nestejných poznámek");
                }

                if (other.DaysNoService[i].To > DaysNoService[i].To)
                {
                    DaysNoService[i].To = other.DaysNoService[i].To;
                    if (DaysNoService[i].To > DaysNoService[i].MaximumTo) throw new InvalidOperationException("Sjednocení To Date nestejných poznámek");
                }
            }
        }
    }
}
