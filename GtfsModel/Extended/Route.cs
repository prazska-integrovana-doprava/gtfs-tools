using GtfsModel.Enumerations;
using GtfsModel.Functions;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Linka - záznam z routes.txt (rozšíření <see cref="GtfsRoute"/>).
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Pokud jde o linku z ASW JŘ, tato položka obsahuje číslo linky v číselníku, jinak 0.
        /// </summary>
        public int AswId { get; set; }

        /// <summary>
        /// ID linky pro GTFS. Vyplněno až po provedení GtfsIdsSetterOperation.
        /// </summary>
        public string GtfsId { get; set; }

        /// <summary>
        /// Označení linky pro veřejnost
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// Trasa linky
        /// </summary>
        public string LongName { get; set; }

        /// <summary>
        /// Druh dopravy dle číselníku GTFS.
        /// </summary>
        public TrafficType Type { get; set; }

        /// <summary>
        /// Barva čáry linky
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Barva písma pro linku
        /// </summary>
        public Color TextColor { get; set; }

        /// <summary>
        /// Indikátor, zda jde o noční linku
        /// </summary>
        public bool IsNight { get; set; }

        /// <summary>
        /// Indikátor, zda jde o regionální linku
        /// </summary>
        public bool IsRegional { get; set; }

        /// <summary>
        /// Indikátor, zda jde o linku náhradní dopravy
        /// </summary>
        public bool IsSubstituteTransport { get; set; }

        /// <summary>
        /// Spoje dané linky
        /// </summary>
        public List<Trip> Trips { get; set; }

        /// <summary>
        /// V PID GTFS feedu používáme jen jednoho společného dopravce "Pražská integrovaná doprava".
        /// Pokud by někoho zajímalo, kdo linku jezdí, dočte se to zde.
        /// </summary>
        public List<RouteSubAgency> SubAgencies { get; set; }

        public Route()
        {
            Type = TrafficType.Undefined;
            Trips = new List<Trip>();
            SubAgencies = new List<RouteSubAgency>();
        }

        /// <summary>
        /// Načte informace o lince na základě záznamu z GTFS
        /// </summary>
        /// <param name="gtfsRoute">Záznam z GTFS</param>
        public static Route Construct(GtfsRoute gtfsRoute, List<RouteSubAgency> subAgencies)
        {
            var route = new Route()
            {
                GtfsId = gtfsRoute.Id,
                ShortName = gtfsRoute.ShortName,
                LongName = gtfsRoute.LongName,
                Color = ParseColorCode(gtfsRoute.Color),
                TextColor = ParseColorCode(gtfsRoute.TextColor),
                IsNight = gtfsRoute.IsNight,
                IsRegional = gtfsRoute.IsRegional,
                IsSubstituteTransport = gtfsRoute.IsSubstituteTransport,
                Type = gtfsRoute.Type,
                SubAgencies = subAgencies.Where(a => a.RouteId == gtfsRoute.Id).ToList(),
            };

            IdentifierManagement.ParseRouteId(route);
            return route;
        }

        private static Color ParseColorCode(string colorCode)
        {
            var r = colorCode.Substring(0, 2);
            var g = colorCode.Substring(2, 2);
            var b = colorCode.Substring(4, 2);
            return Color.FromArgb(int.Parse(r, NumberStyles.HexNumber), int.Parse(g, NumberStyles.HexNumber), int.Parse(b, NumberStyles.HexNumber));
        }

        /// <summary>
        /// Vrátí GTFS záznam linky
        /// </summary>
        /// <returns>GTFS záznam linky</returns>
        public GtfsRoute ToGtfsRoute(int agencyId)
        {
            return new GtfsRoute()
            {
                Id = GtfsId,
                AgencyId = agencyId,
                ShortName = ShortName,
                LongName = LongName,
                Type = Type,
                Url = AswId != 0 && ShortName.All(ch => ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') ? $"https://pid.cz/linka/{ShortName}" : null,
                Color = $"{Color.R:X2}{Color.G:X2}{Color.B:X2}",
                TextColor = $"{TextColor.R:X2}{TextColor.G:X2}{TextColor.B:X2}",
                IsNight = IsNight,
                IsRegional = IsRegional,
                IsSubstituteTransport = IsSubstituteTransport,
            };
        }
        
        public override string ToString()
        {            
            return $"{GtfsId} {ShortName} {LongName}";
        }
    }
}
