using GtfsModel.Extended;
using System.Collections.Generic;
using System.Linq;

namespace GtfsModel.Functions
{
    /// <summary>
    /// Správa tvorby a parsování identifikátorů v PID GTFS
    /// </summary>
    public static class IdentifierManagement
    {
        // Načte číselný identifikátor zakódovaný v GTFS ID (hledá písmeno 'before' a číslice za ním jsou hledané ID), v případě neúspěchu vrací 0.
        private static int ParseId(string gtfsId, char before)
        {
            if (!gtfsId.Contains(before))
            {
                return 0;
            }

            var substr = new string(gtfsId.SkipWhile(ch => ch != before).Skip(1).TakeWhile(ch => char.IsDigit(ch)).ToArray());
            int result;
            int.TryParse(substr, out result);
            return result;
        }

        /// <summary>
        /// Vygeneruje GTFS ID pro zastávku jako UuzelZzastávka_suffix
        /// </summary>
        /// <param name="stop">Zastávka</param>
        /// <param name="suffix">připojení za ID</param>
        public static string GenerateStopId(Stop stop)
        {
            return $"U{stop.AswNodeId}Z{stop.AswStopId}";
        }

        /// <summary>
        /// Načte <see cref="Stop.AswNodeId"/> a <see cref="Stop.AswStopId"/> z ID zastávky
        /// </summary>
        /// <param name="stop">Zastávka</param>
        public static void ParseStopId(Stop stop)
        {
            stop.AswNodeId = ParseId(stop.GtfsId, 'U');
            stop.AswStopId = ParseId(stop.GtfsId, 'Z');        
        }

        /// <summary>
        /// Generuje GTFS ID pro linku jako Lčíslo
        /// </summary>
        /// <param name="route">Linka</param>
        public static string GenerateRouteId(Route route)
        {
            return $"L{route.AswId}";
        }

        /// <summary>
        /// Načte číslo linky <see cref="Route.AswId"/> z GTFS ID.
        /// </summary>
        /// <param name="route">Linka</param>
        public static void ParseRouteId(Route route)
        {
            route.AswId = ParseId(route.GtfsId, 'L');
        }

        /// <summary>
        /// Vygeneruje ID bloku, do kterého spoj patří. Pokud je neobsahuje přejezd, je ID bloku prázdný string.
        /// Pokud spoj je přejezdem, nebo přejíždí, pak patří do bloku a jeho ID je rovno ID prvního spoje v tomto bloku.
        /// </summary>
        /// <param name="trip">Spoj</param>
        public static string GenerateBlockId(Trip trip)
        {
            if (trip.PreviousTripInBlock == null && trip.NextTripInBlock == null)
            {
                return "";
            }
            else
            {
                return trip.FirstTripInBlock.GtfsId;
            }
        }
    }
}
