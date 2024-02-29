using System;

namespace AswModel.Extended
{
    /// <summary>
    /// Souřadnice v GPS a JTSK. Používá se pro zastávky a trasy.
    /// </summary>
    public struct Coordinates
    {
        /// <summary>
        /// Zeměpisná šířka (ČR kolem 50°)
        /// </summary>
        public double GpsLatitude { get; set; }

        /// <summary>
        /// Zeměpisná délka (ČR kolem 15°)
        /// </summary>
        public double GpsLongitude { get; set; }

        /// <summary>
        /// X-ová souřadnice v křovákovi
        /// </summary>
        public double JtskX { get; set; }

        /// <summary>
        /// Y-ová souřadnice v křovákovi
        /// </summary>
        public double JtskY { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is Coordinates))
                return false;

            return Equals((Coordinates) obj);
        }

        public double DistanceTo(Coordinates other)
        {
            var dx = JtskX - other.JtskX;
            var dy = JtskY - other.JtskY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool Equals(Coordinates other)
        {
            return Math.Abs(GpsLatitude - other.GpsLatitude) < 0.0001 && Math.Abs(GpsLongitude - other.GpsLongitude) < 0.00019;
        }

        public override int GetHashCode()
        {
            return GpsLatitude.GetHashCode() ^ GpsLongitude.GetHashCode();
        }

        public static bool operator == (Coordinates gps1, Coordinates gps2)
        {
            return gps1.Equals(gps2);
        }

        public static bool operator != (Coordinates gps1, Coordinates gps2)
        {
            return !gps1.Equals(gps2);
        }

        public override string ToString()
        {
            return $"[{GpsLatitude:0.00000}, {GpsLongitude:0.00000}]";
        }
    }
}
