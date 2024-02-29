using System;

namespace CommonLibrary
{
    /// <summary>
    /// Čas (příjezdu nebo odjezdu).
    /// Podporuje i tvary pro > 24h (dle specifikace GTFS, pro přespůlnoční spoje).
    /// 
    /// Při posunu času je nutné čas specifikovat vzhledem k předchozímu dni (26:30 je normálně 2:30, při přechodu na SELČ 3:30, při přechodu na SEČ 1:30).
    /// </summary>
    public struct Time : IComparable<Time>, ICsvSerializable
    {
        /// <summary>
        /// Počet vteřin od půlnoci. Neřeší žádné posuny časů, prostě 86400 je vždy 24:00:00.
        /// </summary>
        public int TotalSeconds { get; private set; }
        
        /// <summary>
        /// Hodina
        /// </summary>
        public int Hours
        {
            get { return TotalSeconds / 3600; }
            set { TotalSeconds = (TotalSeconds % 3600) + value * 3600; }
        }

        /// <summary>
        /// Minuta
        /// </summary>
        public int Minutes
        {
            get { return TotalSeconds % 3600 / 60; }
            set { TotalSeconds = (TotalSeconds / 3600) * 3600 + value * 60 + TotalSeconds % 60; }
        }

        /// <summary>
        /// Sekunda
        /// </summary>
        public int Seconds
        {
            get { return TotalSeconds % 60; }
            set { TotalSeconds = (TotalSeconds / 60) * 60 + value; }
        }

        /// <summary>
        /// Kolikrát jsme "přesáhli" 24 hodin
        /// </summary>
        public int DaysOffset
        {
            get { return TotalSeconds / 86400; }
            set { TotalSeconds = (TotalSeconds % 86400) + value * 86400; }
        }

        public Time (int hours, int minutes, int seconds)
        {
            TotalSeconds = hours * 3600 + minutes * 60 + seconds;
        }

        public Time (int totalSeconds)
        {
            TotalSeconds = totalSeconds;
        }

        public Time AddDay()
        {
            return new Time(TotalSeconds + 86400);
        }

        public Time AddMinutes(int minutes)
        {
            return new Time(TotalSeconds + minutes * 60);
        }

        public Time AddSeconds(int seconds)
        {
            return new Time(TotalSeconds + seconds);
        }

        public Time ModuloDay()
        {
            return new Time(TotalSeconds % 86400);
        }

        public override string ToString()
        {
            return $"{Hours}:{Minutes:00}:{Seconds:00}";
        }

        public string ToStringWithoutSeconds()
        {
            return $"{Hours}:{Minutes:00}";
        }

        // musí být kvůli implementaci ICsvSerializable
        public void LoadFromString(string str)
        {
            LoadFromString(str, true);
        }

        /// <summary>
        /// Načte řetězec ve formátu H:M:S. Pokud je formát nějakým způsobem chybný, vyhodí výjimku.
        /// </summary>
        /// <param name="str">Čas jako string</param>
        /// <param name="requireSeconds">Pokud je false, akceptuje též formát H:M</param>
        public void LoadFromString(string str, bool requireSeconds)
        {
            var parts = str.Split(':');
            if (parts.Length == 3 || (parts.Length == 2 && !requireSeconds))
            {
                try
                {
                    var hrs = int.Parse(parts[0]);
                    var mins = int.Parse(parts[1]);
                    var secs = parts.Length > 2 ? int.Parse(parts[2]) : 0;
                    TotalSeconds = hrs * 3600 + mins * 60 + secs;
                }
                catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                {
                    throw new FormatException($"Time {str} is not in correct format", ex);
                }
            }
            else
            {
                throw new FormatException($"Time {str} is not in correct format");
            }
        }

        /// <summary>
        /// Vytvoří čas z řetězce ve formátu H:M:S. Pokud je formát nějakým způsobem chybný, vyhodí výjimku
        /// </summary>
        /// <param name="str">Čas jako string</param>
        public static Time Parse(string str)
        {
            var result = new Time();
            result.LoadFromString(str);
            return result;
        }

        /// <summary>
        /// Načte pouze časovou složku z instance <see cref="DateTime"/>
        /// </summary>
        /// <param name="dateTime">Čas</param>
        public static Time FromDateTime(DateTime dateTime, int dateOffset)
        {
            return new Time(dateTime.Hour + dateOffset * 24, dateTime.Minute, dateTime.Second);
        }

        public int CompareTo(Time other)
        {
            return TotalSeconds.CompareTo(other.TotalSeconds);
        }

        public override bool Equals(object obj)
        {
            var other = (Time)obj;
            return TotalSeconds.Equals(other.TotalSeconds);
        }

        public override int GetHashCode()
        {
            return TotalSeconds.GetHashCode();
        }

        public DateTime ToDateTime(DateTime referenceDate)
        {
            return referenceDate.AddSeconds(TotalSeconds);
        }

        public DateTime ToDateTime()
        {
            return ToDateTime(new DateTime(0));
        }

        public static readonly Time MinValue = new Time(0);
        public static readonly Time MaxValue = new Time(int.MaxValue);

        public static bool operator > (Time time1, Time time2)
        {
            return time1.CompareTo(time2) > 0;
        }

        public static bool operator < (Time time1, Time time2)
        {
            return time1.CompareTo(time2) < 0;
        }

        public static bool operator == (Time time1, Time time2)
        {
            return time1.Equals(time2);
        }

        public static bool operator != (Time time1, Time time2)
        {
            return !time1.Equals(time2);
        }

        public static bool operator >= (Time time1, Time time2)
        {
            return time1.CompareTo(time2) >= 0;
        }

        public static bool operator <= (Time time1, Time time2)
        {
            return time1.CompareTo(time2) <= 0;
        }

        public static int operator - (Time time1, Time time2)
        {
            return time1.TotalSeconds - time2.TotalSeconds;
        }
    }
}
