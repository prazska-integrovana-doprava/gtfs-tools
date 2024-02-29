using System;

namespace CommonLibrary
{
    /// <summary>
    /// GPS souřadnice.
    /// </summary>
    public struct GpsCoordinates
    {
        public double GpsLatitude { get; set; }
        public double GpsLongitude { get; set; }

        public GpsCoordinates(double lat, double lon)
        {
            GpsLatitude = lat;
            GpsLongitude = lon;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GpsCoordinates))
                return false;

            return Equals((GpsCoordinates) obj);
        }

        public bool Equals(GpsCoordinates other)
        {
            return Math.Abs(GpsLatitude - other.GpsLatitude) < 0.00001 && Math.Abs(GpsLongitude - other.GpsLongitude) < 0.000019;
        }

        public override int GetHashCode()
        {
            return GpsLatitude.GetHashCode() ^ GpsLongitude.GetHashCode();
        }

        public static bool operator == (GpsCoordinates gps1, GpsCoordinates gps2)
        {
            return gps1.Equals(gps2);
        }

        public static bool operator != (GpsCoordinates gps1, GpsCoordinates gps2)
        {
            return !gps1.Equals(gps2);
        }

        public override string ToString()
        {
            return $"[{GpsLatitude:0.000000}, {GpsLongitude:0.000000}]";
        }
    }
}
