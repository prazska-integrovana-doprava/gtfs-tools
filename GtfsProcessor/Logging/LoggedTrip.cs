using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor.Logging
{
    internal class LoggedTrip
    {
        public int RouteId { get; set; }

        public List<int> TripIds { get; set; }

        public string Calendar { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as LoggedTrip;
            if (other == null)
                return false;

            return Enumerable.SequenceEqual(TripIds, other.TripIds);
        }

        public override int GetHashCode()
        {
            int hash = 19;
            foreach (var foo in TripIds)
            {
                hash = hash * 31 + foo.GetHashCode();
            }

            return hash;
        }

        public override string ToString()
        {
            return $"L{RouteId}|{string.Join("+", TripIds)} cal {Calendar}";
        }
    }
}
