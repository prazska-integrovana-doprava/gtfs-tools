using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using CommonLibrary.DotNet48;
using GtfsLogging;
using GtfsModel.Extended;
using static ShapeManager.ShapeConstructor;

namespace ShapeManager
{
    /// <summary>
    /// Databáze tras. Uchovává a generuje trasy pro vlakové spoje na základě souřadnic zastávek/dopravních bodů a sítě vlakových kolejí.
    /// </summary>
    public class ShapeDatabase
    {
        // vzdálenost, do které prohledáváme hrany kolem stanice - musí být trochu větší, protože prohledávám podle krajních bodů a když
        // je hrana dlouhá, může stanice spadnout mezi tyto dva body
        private const double MaxEdgeDistance = 1000; // metrů

        // vzdálenost, při které reportujeme, že je stanice moc daleko - vztahuje se ke vzdálenosti přímo ke trati (po splitu hran)
        private const double DistanceWarning = 30; // metrů

        private Dictionary<Stop, Dictionary<Stop, List<GpsCoordinates>>> paths = new Dictionary<Stop, Dictionary<Stop, List<GpsCoordinates>>>();

        /// <summary>
        /// Již zkonstruované trasy
        /// </summary>
        public List<Shape> Shapes { get; private set; }

        // již nalezené trasy nad sítí vlaků mezi dvojicemi sousedních zastávek
        private readonly ShapeConstructor shapeConstructor = new ShapeConstructor();

        // načtené body v síti reprezentující pozice stanic a waypointů nad sítí
        private Dictionary<GpsCoordinates, ShapeConstructor.Point> stopsToPointsMapping;

        private WaypointCollection waypointCollection;

        private ICommonLogger log;

        private Dictionary<Route, int> lastRouteShapeIndex = new Dictionary<Route, int>();

        protected ShapeDatabase(ICommonLogger log)
        {
            Shapes = new List<Shape>();
            this.shapeConstructor = new ShapeConstructor();
            this.log = log;
        }

        /// <summary>
        /// Načte data sítě a namapuje zastávky na síť
        /// </summary>
        /// <param name="networkFileName">Soubor popisující síť (viz <see cref="ShapeConstructor"/>)</param>
        /// <param name="stops">Zastávky nad sítí</param>
        /// <param name="log">Objekt pro logování</param>
        /// <param name="waypointsFileName">Soubor s waypointy, nemusí být zadán, pak se počítá s prázdnou množinou waypointů</param>
        public static ShapeDatabase Create(string networkFileName, IEnumerable<Stop> stops, ICommonLogger log, string waypointsFileName = null)
        {
            var result = new ShapeDatabase(log);
            result.shapeConstructor.LoadPointData(networkFileName);
            result.waypointCollection = waypointsFileName != null ? WaypointCollection.Load(waypointsFileName) : new WaypointCollection(); ;
            result.stopsToPointsMapping = result.MapStationsOnNetwork(stops, result.waypointCollection, log);
            return result;
        }

        /// <summary>
        /// Vygeneruje a nastaví všem spojům trasu a nastaví jim do stop times hodnoty shape_dist_traveled.
        /// </summary>
        /// <param name="trips">Všechny spoje</param>
        public void ProcessTrips(IEnumerable<Trip> trips)
        {
            foreach (var trip in trips)
            {
                SetShapeAndDistTraveled(trip);
            }
        }

        public Shape SetShapeAndDistTraveled(Trip trip)
        {
            var tripStops = trip.StopTimes.Select(st => st.Stop).ToArray();
            foreach (var shape in Shapes)
            {
                var shapeStops = shape.TripsOfThisShape.First().StopTimes.Select(st => st.Stop).ToArray();
                if (Enumerable.SequenceEqual(shapeStops, tripStops))
                {
                    CopyTraveledDistances(shape.TripsOfThisShape.First(), trip);
                    trip.Shape = shape;
                    shape.TripsOfThisShape.Add(trip);
                    return shape;
                }
            }

            // není žádný trip se stejnou trasou, vytvoříme novou
            var newShape = CreateShapeAndSetTraveledDistances(trip);
            trip.Shape = newShape;
            Shapes.Add(newShape);
            newShape.TripsOfThisShape.Add(trip);
            return newShape;
        }

        private List<GpsCoordinates> FindOrCreatePath(Stop from, Stop to)
        {
            var path = paths.GetValueOrDefault(from)?.GetValueOrDefault(to);
            if (path == null)
            {
                path = CreatePath(from, to);
                paths.GetValueAndAddIfMissing(from, new Dictionary<Stop, List<GpsCoordinates>>()).Add(to, path);
            }

            return path;
        }

        private List<GpsCoordinates> CreatePath(Stop from, Stop to)
        {
            var pointFrom = stopsToPointsMapping.GetValueOrDefault(from.Position);
            var waypoints = waypointCollection.FindWaypoints(from, to);
            var allPoints = new List<Point>() { pointFrom };
            foreach (var waypoint in waypoints)
            {
                var waypointPoint = stopsToPointsMapping.GetValueOrDefault(waypoint.ToGpsCoordinates());
                if (waypointPoint != null)
                {
                    allPoints.Add(waypointPoint);
                }
            }

            var pointTo = stopsToPointsMapping.GetValueOrDefault(to.Position);
            allPoints.Add(pointTo);

            // nyní máme v allPoints výchozí bod (zastávku), všechny waypointy a cílový bod (zastávku)
            var resultPath = new List<GpsCoordinates>();
            for (int i = 0; i < allPoints.Count - 1; i++)
            {
                var partialPath = shapeConstructor.FindPath(allPoints[i], allPoints[i + 1]);
                if (partialPath == null)
                {
                    if (i == 0)
                    {
                        log.Log(LogMessageType.WARNING_TRAIN_SHAPE_PATH_NOT_FOUND, $"Cesta ze stanice {from.Name} do stanice {to.Name} nebyla nalezena. Používám přímé propojení.");
                    }
                    else
                    {
                        log.Log(LogMessageType.WARNING_TRAIN_SHAPE_PATH_NOT_FOUND, $"Cesta ze stanice {from.Name} do stanice {to.Name} (přes waypoint {i}: {allPoints[i].Gps}) nebyla nalezena. Používám přímé propojení.");
                    }

                    return new List<GpsCoordinates>() { from.Position, to.Position };
                }

                resultPath.AddRange(i == 0 ? partialPath : partialPath.Skip(1));
            }

            return resultPath;
        }

        /// <summary>
        /// Namapuje stanice a dopravní body na síť. Nepáruje jen s body, ale též láme delší hrany na menší části a v případě potřeby vytvoří na hraně nový bod.
        /// </summary>
        /// <param name="stations">Seznam stanic</param>
        /// <param name="log">Log, kam se zapisují nestandardní události během mapování</param>
        private Dictionary<GpsCoordinates, ShapeConstructor.Point> MapStationsOnNetwork(IEnumerable<Stop> stations, WaypointCollection waypointCollection, ICommonLogger log)
        {
            var stationClosestPoints = new Dictionary<GpsCoordinates, ShapeConstructor.Point>();
            foreach (var station in stations)
            {
                var mappedStationPoint = MapPointOnNetwork(log, station.Position, $"stanice {station.Name}");
                if (mappedStationPoint != null)
                {
                    stationClosestPoints[station.Position] = mappedStationPoint;
                }
            }

            foreach (var waypointsForStop in waypointCollection.Waypoints)
            {
                foreach (var waypoint in waypointsForStop.Waypoints)
                {
                    var waypointCoordinates = waypoint.ToGpsCoordinates();

                    var mappedWaypoint = MapPointOnNetwork(log, waypointCoordinates, $"Waypoint {waypoint} ({waypointsForStop.From} - {waypointsForStop.To})");
                    if (mappedWaypoint != null)
                    {
                        stationClosestPoints[waypointCoordinates] = mappedWaypoint;
                    }
                }
            }

            return stationClosestPoints;
        }

        private Point MapPointOnNetwork(ICommonLogger log, GpsCoordinates positionToMap, string positionName)
        {
            // hrany s alespoň jedním krajním bodem nedaleko hledané stanice
            var closeEdges = shapeConstructor.AllEdges.Where(e => MapFunctions.DistanceMeters(e.From.Gps.GpsLatitude, e.From.Gps.GpsLongitude, positionToMap.GpsLatitude, positionToMap.GpsLongitude) < MaxEdgeDistance
                                              || MapFunctions.DistanceMeters(e.To.Gps.GpsLatitude, e.To.Gps.GpsLongitude, positionToMap.GpsLatitude, positionToMap.GpsLongitude) < MaxEdgeDistance);
            var closestCoordinate = new GpsCoordinates();
            ShapeConstructor.Edge closestEdge = null;
            double minDistance = double.MaxValue;
            foreach (var edge in closeEdges)
            {
                var point = GetClosestPointOnEdge(edge, positionToMap);
                var dist = MapFunctions.DistanceMeters(point.GpsLatitude, point.GpsLongitude, positionToMap.GpsLatitude, positionToMap.GpsLongitude);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestCoordinate = point;
                    closestEdge = edge;
                }
            }

            if (closestEdge == null)
            {
                log.Log(LogMessageType.WARNING_TRAIN_NO_SHAPE_AROUND, $"V okolí {MaxEdgeDistance} metrů stanice {positionName} nebyla nalezena žádná hrana trasy, ke které by stanice mohla být připnuta. Trasy z/do této stanice budou vždy vedeny vzdušnou čarou.");
                return null;
            }
            else if (minDistance > DistanceWarning)
            {
                log.Log(LogMessageType.WARNING_TRAIN_SHAPE_TOO_FAR_FROM_STOP, $"Stanice {positionName} je od nejbližšího bodu na trase vzdálená {minDistance} metrů.");
            }

            // vytvoříme uprostřed hrany nový bod
            ShapeConstructor.Point stationPoint;
            if (closestCoordinate.Equals(closestEdge.From.Gps))
            {
                stationPoint = closestEdge.From;
            }
            else if (closestCoordinate.Equals(closestEdge.To.Gps))
            {
                stationPoint = closestEdge.To;
            }
            else
            {
                stationPoint = shapeConstructor.CreateNewPointInEdge(closestCoordinate, closestEdge, out var newEdge1, out var newEdge2);
            }

            return stationPoint;
        }

        private static GpsCoordinates GetClosestPointOnEdge(Edge edge, GpsCoordinates point)
        {
            var A = edge.From.Gps;
            var B = edge.To.Gps;
            var P = point;

            // vektory
            double ax = A.GpsLatitude;
            double ay = A.GpsLongitude;

            double bx = B.GpsLatitude;
            double by = B.GpsLongitude;

            double px = P.GpsLatitude;
            double py = P.GpsLongitude;

            double abx = bx - ax;
            double aby = by - ay;

            double apx = px - ax;
            double apy = py - ay;

            double abSquared = abx * abx + aby * aby;

            // ochrana proti nulové délce
            if (abSquared == 0)
                return A;

            double t = (apx * abx + apy * aby) / abSquared;

            // clamp na úsečku
            t = Math.Max(0, Math.Min(1, t));

            // výsledný bod
            double closestX = ax + t * abx;
            double closestY = ay + t * aby;

            return new GpsCoordinates
            {
                GpsLatitude = closestX,
                GpsLongitude = closestY
            };
        }

        private void CopyTraveledDistances(Trip from, Trip to)
        {
            for (int i = 0; i < from.StopTimes.Count; i++)
            {
                to.StopTimes[i].ShapeDistanceTraveledMeters = from.StopTimes[i].ShapeDistanceTraveledMeters;
            }
        }

        private Shape CreateShapeAndSetTraveledDistances(Trip trip)
        {
            if (lastRouteShapeIndex.ContainsKey(trip.Route))
            {
                lastRouteShapeIndex[trip.Route]++;
            }
            else
            {
                lastRouteShapeIndex.Add(trip.Route, 1);
            }

            var resultShape = new Shape()
            {
                Id = $"{trip.Route.GtfsId}V{lastRouteShapeIndex[trip.Route]}",
            };

            trip.StopTimes[0].ShapeDistanceTraveledMeters = 0;

            int pointSequenceIndex = 1;
            double currentDistance = 0;
            for (int i = 1; i < trip.StopTimes.Count; i++)
            {
                var stopTimeFrom = trip.StopTimes[i - 1];
                var stopTimeTo = trip.StopTimes[i];
                var path = FindOrCreatePath(stopTimeFrom.Stop, stopTimeTo.Stop);

                foreach (var point in path)
                {
                    if (resultShape.Points.Any())
                    {
                        var prevPos = resultShape.Points.Last().Position;
                        if (point.Equals(prevPos))
                            continue; // může se stát na zlomu u zastávky (poslední bod předchozího úseku je shodný s tímto úsekem)

                        if (resultShape.Points.Count >= 2)
                        {
                            var prevPrevPos = resultShape.Points[resultShape.Points.Count - 2].Position;
                            if (IsSharpTurn(prevPrevPos, prevPos, point, out var angle))
                            {
                                log.Log(LogMessageType.WARNING_SHAPE_TOO_SHARP_TURN, $"Hodně ostrá zatáčka {angle:0}°, spoj {trip}, úsek {stopTimeFrom.Stop} - {stopTimeTo.Stop}, souřadnice {prevPrevPos}, {prevPos}, {point}.");
                            }
                        }

                        currentDistance += MapFunctions.DistanceMeters(prevPos.GpsLatitude, prevPos.GpsLongitude, point.GpsLatitude, point.GpsLongitude);
                    }

                    resultShape.Points.Add(new ShapePoint()
                    {
                        Position = point,
                        SequenceIndex = pointSequenceIndex++,
                        DistanceTraveledMeters = currentDistance,
                    });
                }

                trip.StopTimes[i].ShapeDistanceTraveledMeters = currentDistance;
            }

            return resultShape;
        }

        public static bool IsSharpTurn(GpsCoordinates A, GpsCoordinates B, GpsCoordinates C, out double angleDeg, double thresholdDegrees = 45)
        {
            double scale = Math.Cos(B.GpsLatitude * Math.PI / 180.0);

            // správně: AB a BC
            double abx = (B.GpsLatitude - A.GpsLatitude);
            double aby = (B.GpsLongitude - A.GpsLongitude) * scale;

            double bcx = (C.GpsLatitude - B.GpsLatitude);
            double bcy = (C.GpsLongitude - B.GpsLongitude) * scale;

            double magAB = Math.Sqrt(abx * abx + aby * aby);
            double magBC = Math.Sqrt(bcx * bcx + bcy * bcy);

            if (magAB == 0 || magBC == 0)
            {
                angleDeg = 0;
                return false;
            }

            double dot = abx * bcx + aby * bcy;

            double cosTheta = dot / (magAB * magBC);
            cosTheta = Math.Max(-1, Math.Min(1, cosTheta));

            double angleRad = Math.Acos(cosTheta);
            angleDeg = angleRad * 180.0 / Math.PI;

            return angleDeg > thresholdDegrees;
        }
    }
}
