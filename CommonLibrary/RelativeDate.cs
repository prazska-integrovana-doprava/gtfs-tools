using System;

namespace CommonLibrary
{
    /// <summary>
    /// Datum relativní k počátku exportu (něco jako index do <see cref="ServiceDaysBitmap"/>).
    /// </summary>
    public struct RelativeDate : IComparable<RelativeDate>, IEquatable<RelativeDate>
    {
        /// <summary>
        /// Index dne vhledem k počátku exportu. 0 = první den exportu, ...
        /// </summary>
        int DayIndex { get; set; }

        public RelativeDate(int dayIndex)
        {
            DayIndex = dayIndex;
        }

        public DateTime AsDateTime(DateTime globalStartDate)
        {
            return globalStartDate.AddDays(DayIndex);
        }

        public int CompareTo(RelativeDate other)
        {
            return DayIndex.CompareTo(other.DayIndex);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is RelativeDate))
                return false;

            return DayIndex == ((RelativeDate)obj).DayIndex;
        }

        public bool Equals(RelativeDate other)
        {
            return DayIndex.Equals(other.DayIndex);
        }

        public override int GetHashCode()
        {
            return DayIndex.GetHashCode();
        }

        public static RelativeDate operator + (RelativeDate first, int second)
        {
            return first.DayIndex + second;
        }

        public static RelativeDate operator - (RelativeDate first, int second)
        {
            return first.DayIndex - second;
        }

        public static RelativeDate operator ++ (RelativeDate value)
        {
            return value + 1;
        }

        public static RelativeDate operator -- (RelativeDate value)
        {
            return value - 1;
        }

        public static bool operator == (RelativeDate first, RelativeDate second)
        {
            return first.DayIndex == second.DayIndex;
        }

        public static bool operator != (RelativeDate first, RelativeDate second)
        {
            return first.DayIndex != second.DayIndex;
        }

        public static bool operator >(RelativeDate first, RelativeDate second)
        {
            return first.DayIndex > second.DayIndex;
        }

        public static bool operator <(RelativeDate first, RelativeDate second)
        {
            return first.DayIndex < second.DayIndex;
        }

        public static bool operator >=(RelativeDate first, RelativeDate second)
        {
            return first.DayIndex >= second.DayIndex;
        }

        public static bool operator <=(RelativeDate first, RelativeDate second)
        {
            return first.DayIndex <= second.DayIndex;
        }

        public static implicit operator RelativeDate (int value)
        {
            return new RelativeDate(value);
        }

        public static implicit operator int (RelativeDate value)
        {
            return value.DayIndex;
        }

        public override string ToString()
        {
            if (DayIndex >= 0)
                return $"D+{DayIndex}";
            else
                return $"D{DayIndex}";
        }
    }
}
