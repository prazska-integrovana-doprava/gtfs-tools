using System.Collections.Generic;
using System.Linq;
using GtfsModel;
using GtfsModel.Enumerations;
using GtfsModel.Extended;

namespace GtfsProcessor
{
    class RouteStopsGenerator
    {
        public IEnumerable<RouteStop> Generate(IEnumerable<Route> routes)
        {
            foreach (var route in routes)
            {
                var stopsOrderedInbound = GenerateStopListForRouteDirection(route, Direction.Inbound);
                foreach (var stop in stopsOrderedInbound)
                    yield return stop;

                var stopsOrderedOutbound = GenerateStopListForRouteDirection(route, Direction.Outbound);
                foreach (var stop in stopsOrderedOutbound)
                    yield return stop;
            }
        }

        private IEnumerable<RouteStop> GenerateStopListForRouteDirection(Route route, Direction direction)
        {
            // TODO
            // vytvořit seznam zastávek pro linku (v daném směru) agregací z tras spojů
            //  - seznam spojů v daném směru vyjma výjimečných => route.Trips.Where(t => t.DirectionId == direction && !t.IsExceptional)
            //  - veřejné zastávky každého tripu => trip.StopTimes.Where(st => st.IsPublic)
            //
            // .. výsledný sled zastávek by měl popisovat trasu linky
            //    možno použít topologické uspořádání, je však nutné vyrovnat se s cykly, kdy jedem bus může jet A - B - C - D a druhý A - C - B - D
            //      (buď zastávky opakovat, anebo zvolit jedno pořadí)


            // následuje dočasná implementace, která vrátí posloupnost zastávek podle nejdelšího spoje na trase
            var longestTrip = route.Trips.Where(t => t.DirectionId == direction && !t.IsExceptional).OrderByDescending(t => t.PublicStopTimes.Count()).FirstOrDefault();
            if (longestTrip == null)
                return Enumerable.Empty<RouteStop>();

            return longestTrip.PublicStopTimes.Select((st, i) => new RouteStop()
            {
                RouteId = route.GtfsId,
                DirectionId = direction,
                StopId = st.Stop.GtfsId,
                StopSequence = i + 1,
            });
        }
    }
}
