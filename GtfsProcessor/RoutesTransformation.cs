using AswModel.Extended;
using GtfsLogging;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Generuje GTFS data o linkách z ASW dat. Doplňuje GTFS ID a barvičky (ty jsou hardcodované zde).
    /// 
    /// Každá linka z ASW má primárně jeden záznam v GTFS. Pouze pokud se v čase liší klíčový parametr (název) a linka
    /// má v ASW více verzí, je ke každé verzi vyrobena extra linka v GTFS.
    /// </summary>
    class RoutesTransformation
    {
        private TheAswDatabase db;
        private ICommonLogger log;

        public RoutesTransformation(TheAswDatabase db, ICommonLogger log)
        {
            this.db = db;
            this.log = log;
        }

        /// <summary>
        /// Převede linky do GTFS reprezentace
        /// </summary>
        /// <returns></returns>
        public IDictionary<Route, GtfsModel.Extended.Route> TransformRoutesToGtfs()
        {
            var result = new Dictionary<Route, GtfsModel.Extended.Route>();
            
            foreach (var routeAllVersions in db.Lines)
            {
                bool firstVersion = true;
                foreach (var routeVersion in routeAllVersions.AllVersions())
                {
                    if (!routeVersion.PublicTrips.Any())
                    {
                        log.Log(LogMessageType.INFO_ROUTE_NO_TRIPS, $"TrafficTypeSetterOperation: Linka {routeVersion} v {routeVersion.ServiceAsBits} nemá žádné spoje, bude ignorována.");
                        continue;
                    }

                    result.Add(routeVersion, TransformRoute(routeVersion, firstVersion));
                    firstVersion = false;
                }
            }

            return result;
        }

        // Převede ASW reprezentaci do GTFS reprezentace. Parametr 'validSinceAlways' říká, zda jde o první veřejnou verzi linky. Pokud je, GTFS ID bude pouze číslo linky. Pokud má
        // linka více verzí, druhá a další budou obsahovat za podtržítkem datum počátku platnosti
        private GtfsModel.Extended.Route TransformRoute(Route route, bool validSinceAlways)
        {
            var gtfsId = $"L{route.LineNumber}";
            if (!validSinceAlways)
                gtfsId = $"{gtfsId}_{route.ValidityStartDate.AsDateTime(db.GlobalStartDate):yyMMdd}";

            var trafficType = TransformTrafficType(route.TrafficType);
            if (trafficType == GtfsModel.Enumerations.TrafficType.Undefined)
            {
                log.Log(LogMessageType.ERROR_UNKNOWN_TRAFFIC_TYPE, $"Linka {route} má špatně definovaný druh dopravy.");
            }

            var isSubstituteTransport = route.IdosRouteCategory == IdosRouteCategory.SubstituteBusTransport || route.IdosRouteCategory == IdosRouteCategory.SubstituteForTrain
                || route.IdosRouteCategory == IdosRouteCategory.SubstituteTramTransport;

            return new GtfsModel.Extended.Route()
            {
                AswId = route.LineNumber,
                Color = GetRouteColor(route),
                GtfsId = gtfsId,
                IsNight = route.IsNight,
                IsRegional = route.IdosRouteCategory == IdosRouteCategory.BusRegional || route.IdosRouteCategory == IdosRouteCategory.BusRegionalNight
                          || route.IdosRouteCategory == IdosRouteCategory.SubstituteForTrain || route.IdosRouteCategory == IdosRouteCategory.Train,
                IsSubstituteTransport = isSubstituteTransport,
                LongName = route.RouteDescription,
                ShortName = route.LineName,
                TextColor = GetRouteTextColor(route, isSubstituteTransport),
                Type = trafficType,
                SubAgencies = route.RouteAgencies.Select(a => new GtfsModel.RouteSubAgency()
                {
                    LicenceNumber = a.CisLineNumber,
                    RouteId = gtfsId,
                    SubAgencyId = a.Agency.Id,
                    SubAgencyName = a.Agency.Name
                }).ToList(),
                // Trips vyplníme v TripsTransformation (teď ještě nejsou hotové)
            };
        }

        /// <summary>
        /// Barva čáry linky
        /// </summary>
        public static Color GetRouteColor(Route route)
        { 
            switch (route.TrafficType)
            {
                case AswTrafficType.Metro:
                    if (route.LineNumber == Route.LineANumber)
                    {
                        return Color.FromArgb(0, 165, 98); //"00A562";
                    }
                    else if (route.LineNumber == Route.LineBNumber)
                    {
                        return Color.FromArgb(248, 179, 34); //"F8B322";
                    }
                    else if (route.LineNumber == Route.LineCNumber)
                    {
                        return Color.FromArgb(207, 0, 61); //"CF003D";
                    }
                    else
                    {
                        return Color.FromArgb(128, 128, 128); //"808080";
                    }

                case AswTrafficType.Tram:
                    return Color.FromArgb(122, 6, 3); //"7A0603";

                case AswTrafficType.Bus:
                    return Color.FromArgb(0, 125, 168); //"007DA8";

                case AswTrafficType.Funicular:
                    return Color.FromArgb(201, 208, 34); //"C9D022";

                case AswTrafficType.Ferry:
                    return Color.FromArgb(0, 179, 203); //"00B3CB";

                case AswTrafficType.Rail:
                    return Color.FromArgb(37, 30, 98); //"251E62";

                case AswTrafficType.Trolleybus:
                    return Color.FromArgb(128, 22, 111); //"80166F";

                default:
                    return Color.FromArgb(128, 128, 128); //"808080";
            }
        }

        /// <summary>
        /// Barva písma pro linku
        /// </summary>
        public static Color GetRouteTextColor(Route route, bool isSubstituteTransport)
        {
            switch (route.TrafficType)
            {
                case AswTrafficType.Bus:
                case AswTrafficType.Tram:
                case AswTrafficType.Rail:
                case AswTrafficType.Trolleybus:
                    if (!isSubstituteTransport)
                        return Color.White;
                    else
                        return Color.FromArgb(237, 146, 46); // oranžová výluková

                case AswTrafficType.Metro:
                    if (route.LineNumber == Route.LineBNumber)
                    {
                        return Color.Black;
                    }
                    else
                    {
                        return Color.White;
                    }

                case AswTrafficType.Funicular:
                case AswTrafficType.Ferry:
                default: return Color.Black;
            }
        }

        private GtfsModel.Enumerations.TrafficType TransformTrafficType(AswTrafficType trafficType)
        {
            switch (trafficType)
            {
                case AswTrafficType.Metro:
                    return GtfsModel.Enumerations.TrafficType.Metro;

                case AswTrafficType.Tram:
                    return GtfsModel.Enumerations.TrafficType.Tram;

                case AswTrafficType.Bus:
                    return GtfsModel.Enumerations.TrafficType.Bus;

                case AswTrafficType.Funicular:
                    return GtfsModel.Enumerations.TrafficType.Funicular;

                case AswTrafficType.Rail:
                    return GtfsModel.Enumerations.TrafficType.Rail;

                case AswTrafficType.Ferry:
                    return GtfsModel.Enumerations.TrafficType.Ferry;

                case AswTrafficType.Trolleybus:
                    return GtfsModel.Enumerations.TrafficType.Trolleybus;

                default:
                    return GtfsModel.Enumerations.TrafficType.Undefined;
            }
        }
    }
}
