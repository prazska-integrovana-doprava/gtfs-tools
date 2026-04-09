using GtfsLogging;
using JdfModel;
using System.Drawing;

namespace JdfToGtfsProcessor
{
    internal class RouteMapping
    {
        /// <summary>
        /// Seznam linek indexovaný licenčním číslem CIS (může obsahovat stejnou linku vícekrát, pokud je složena z více licenčních linek)
        /// </summary>
        public Dictionary<int, GtfsModel.Extended.Route> JdfRoutesToGtfs { get; private set; }

        /// <summary>
        /// seznam linek indexovaný jejich ID
        /// </summary>
        public Dictionary<string, GtfsModel.Extended.Route> GtfsRoutes { get; private set; }

        // potřebujeme hlavně kvůli logování, pro každou GTFS linku tam bude první JDF linka, ze které vznikla
        private Dictionary<GtfsModel.Extended.Route, Route> reverseMappingFromGtfsToJdf;

        public RouteMapping()
        {
            GtfsRoutes = new Dictionary<string, GtfsModel.Extended.Route>();

            JdfRoutesToGtfs = new Dictionary<int, GtfsModel.Extended.Route>();

            reverseMappingFromGtfsToJdf = new Dictionary<GtfsModel.Extended.Route, Route>();
        }

        public void TransformJdfRoutesToGtfs(IEnumerable<Route> jdfRoutes, Dictionary<int, List<RouteExt>> jdfRoutesExtendedData, GtfsModel.GtfsAgency gtfsAgency, Dictionary<string, Agency> jdfAgencies, ISimpleLogger log) 
        {
            // tohle je tu jen pro kontrolu, že každá linka je v souboru jen jednou
            jdfRoutes.ToDictionary(r => r.RouteId);

            foreach (var route in jdfRoutes)
            {
                var routeExts = jdfRoutesExtendedData.GetValueOrDefault(route.RouteId);
                if (routeExts != null && routeExts.Count > 1)
                {
                    log.Log($"Existuje více záznamů pro linku {route} v souboru RouteExt.txt. Používám první uvedený.");
                }

                var routeExt = routeExts?.FirstOrDefault();
                string routeShortName;
                if (routeExt != null && !string.IsNullOrEmpty(routeExt.RouteName))
                {
                    routeShortName = routeExt.RouteName;
                }
                else
                {
                    routeShortName = (route.RouteId % 1000).ToString();
                    log.Log($"Linka {route} nemá záznam v LinExt.txt nebo v něm nemá definovaný alias, používám automaticky poslední tři číslice z licenčního čísla: {routeShortName}");
                }

                var gtfsRouteId = route.RouteId.ToString();
                var gtfsRoute = GtfsRoutes.GetValueOrDefault(gtfsRouteId);
                if (gtfsRoute != null)
                {
                    var otherOriginalRoute = reverseMappingFromGtfsToJdf[gtfsRoute];
                    if (otherOriginalRoute.RouteDescription != route.RouteDescription)
                    {
                        log.Log($"Linky {otherOriginalRoute} a {route} budou sloučeny jako linka {routeShortName}, mají ovšem odlišné názvy v JDF: {otherOriginalRoute.RouteDescription} x {route.RouteDescription}. Použije se první hodnota.");
                    }
                    else if (otherOriginalRoute.TrafficType != route.TrafficType)
                    {
                        log.Log($"Linky {otherOriginalRoute} a {route} budou sloučeny jako linka {routeShortName}, jsou ovšem odlišného druhu dopravy: {otherOriginalRoute.TrafficType} x {route.TrafficType}. Použije se první hodnota, takže spoje linky {route} budou nejspíše vedeny pod chybným druhem dopravy.");
                    }
                    else if ((otherOriginalRoute.RouteType == RouteTypes.Urban) != (route.RouteType == RouteTypes.Urban))
                    {
                        log.Log($"Linky {otherOriginalRoute} a {route} budou sloučeny jako linka {routeShortName}, mají ovšem odlišný typ: {otherOriginalRoute.RouteType} x {route.RouteType}. Použije se {otherOriginalRoute.RouteType}.");
                    }
                }
                else
                {
                    gtfsRoute = new GtfsModel.Extended.Route()
                    {
                        GtfsId = gtfsRouteId,
                        Agency = gtfsAgency,
                        LongName = route.RouteDescription,
                        ShortName = routeShortName,
                        Type = TranslateTrafficType(route, log),
                        IsRegional = route.RouteType != RouteTypes.Urban,
                    };

                    gtfsRoute.Color = GetRouteColorOdis(gtfsRoute);
                    gtfsRoute.TextColor = GetTextColorOdis(gtfsRoute);

                    GtfsRoutes.Add(gtfsRouteId, gtfsRoute);
                    reverseMappingFromGtfsToJdf.Add(gtfsRoute, route);
                }

                if (!JdfRoutesToGtfs.ContainsKey(route.RouteId))
                {
                    JdfRoutesToGtfs.Add(route.RouteId, gtfsRoute);

                    var jdfAgency = jdfAgencies.GetValueOrDefault(route.AgencyId);
                    if (jdfAgency == null)
                    {
                        log.Log($"Dopravce {route.AgencyId} linky {route} nenalezen. Nebude vyplněn název dopravce.");
                    }

                    gtfsRoute.SubAgencies.Add(new GtfsModel.RouteSubAgency()
                    {
                        LicenceNumber = route.RouteId,
                        RouteId = gtfsRoute.GtfsId,
                        SubAgencyId = route.AgencyId,
                        SubAgencyName = jdfAgency?.Name,
                    });
                }
                else if (JdfRoutesToGtfs[route.RouteId] != gtfsRoute)
                {
                    log.Log($"Linka {route} už je nasměrována na GTFS {JdfRoutesToGtfs[route.RouteId]}, ale teď má být najednou přesměrována na {gtfsRoute}. Ignoruji.");
                }
            }
        }

        private GtfsModel.Enumerations.TrafficType TranslateTrafficType(Route route, ISimpleLogger log)
        {
            switch (route.TrafficType)
            {
                case RouteTrafficTypes.Bus: return GtfsModel.Enumerations.TrafficType.Bus;
                case RouteTrafficTypes.Ferry: return GtfsModel.Enumerations.TrafficType.Ferry;
                case RouteTrafficTypes.Tram: return GtfsModel.Enumerations.TrafficType.Tram;
                case RouteTrafficTypes.Trolleybus: return GtfsModel.Enumerations.TrafficType.Trolleybus;
                case RouteTrafficTypes.Metro: return GtfsModel.Enumerations.TrafficType.Metro;
                case RouteTrafficTypes.FunicularOrCable:
                    log.Log($"Druh dopravy {RouteTrafficTypes.FunicularOrCable} linky {route} nelze jednoznačně přeložit do GTFS. Překládám jako Funicular (ale mohlo by být i CableCar).");
                    return GtfsModel.Enumerations.TrafficType.Funicular;
                default:
                    log.Log($"Druh dopravy {RouteTrafficTypes.FunicularOrCable} linky {route} nelze jednoznačně přeložit do GTFS. Překládám jako Bus.");
                    return GtfsModel.Enumerations.TrafficType.Bus;
            }
        }

        private static Color GetRouteColorOdis(GtfsModel.Extended.Route route)
        {
            switch (route.Type)
            {
                case GtfsModel.Enumerations.TrafficType.Bus:
                    if (route.IsRegional!.Value) return ColorTranslator.FromHtml("#AE4A84"); // linkám nastavujeme IsRegional vždy
                    else return ColorTranslator.FromHtml("#0078BF");
                case GtfsModel.Enumerations.TrafficType.Tram:
                    return ColorTranslator.FromHtml("#E31E24");
                case GtfsModel.Enumerations.TrafficType.Trolleybus:
                    return ColorTranslator.FromHtml("#009846");
                case GtfsModel.Enumerations.TrafficType.Ferry:
                    return ColorTranslator.FromHtml("#FEA13B");
                default:
                    return Color.DarkGray;
            }
        }

        private static Color GetTextColorOdis(GtfsModel.Extended.Route route)
        {
            return Color.White;
        }
    }
}
