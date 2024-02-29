using CommonLibrary;
using CsvSerializer;
using GtfsLogging;
using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainsEditor.ExportModel
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

        // načtené body v síti reprezentující pozice stanic nad sítí
        private Dictionary<Stop, ShapeConstructor.Point> stopsToPointsMapping;

        private ICommonLogger log;

        private Dictionary<Route, int> lastRouteShapeIndex = new Dictionary<Route, int>();

        /// <summary>
        /// Vytvoří instanci shape databáze a inicializuje konstruktor tras.
        /// </summary>
        /// <param name="shapeConstructor">Instance, která má načtenou síť</param>
        /// <param name="stops">Všechny použité zastávky a dopravní body a jejich mapování na síť v <paramref name="shapeConstructor"/>.</param>
        public ShapeDatabase(ShapeConstructor shapeConstructor, IEnumerable<Stop> stops, ICommonLogger log)
        {
            Shapes = new List<Shape>();
            this.shapeConstructor = shapeConstructor;
            this.stopsToPointsMapping = MapStationsOnNetwork(stops, log);
            this.log = log;
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
            var pointFrom = stopsToPointsMapping.GetValueOrDefault(from);
            var pointTo = stopsToPointsMapping.GetValueOrDefault(to);

            var resultPath = shapeConstructor.FindPath(pointFrom, pointTo);
            if (resultPath == null)
            {
                log.Log(LogMessageType.WARNING_TRAIN_SHAPE_PATH_NOT_FOUND, $"Cesta ze stanice {from.Name} do stanice {to.Name} nebyla nalezena. Používám přímé propojení.");
                return new List<GpsCoordinates>() { from.Position, to.Position };
            }

            return resultPath;
        }

        /// <summary>
        /// Namapuje stanice a dopravní body na síť. Nepáruje jen s body, ale též láme delší hrany na menší části a v případě potřeby vytvoří na hraně nový bod.
        /// </summary>
        /// <param name="stations">Seznam stanic</param>
        /// <param name="log">Log, kam se zapisují nestandardní události během mapování</param>
        private Dictionary<Stop, ShapeConstructor.Point> MapStationsOnNetwork(IEnumerable<Stop> stations, ICommonLogger log)
        {
            var edgeSplit = shapeConstructor.AllEdges.ToDictionary(e => e, e => SplitEdgeToParts(e));
            var stationClosestPoints = new Dictionary<Stop, ShapeConstructor.Point>();
            foreach (var station in stations)
            {
                // hrany s alespoň jedním krajním bodem nedaleko hledané stanice
                var closeEdges = shapeConstructor.AllEdges.Where(e => MapFunctions.DistanceMeters(e.Item1.Gps.GpsLatitude, e.Item1.Gps.GpsLongitude, station.Position.GpsLatitude, station.Position.GpsLongitude) < MaxEdgeDistance
                                                  || MapFunctions.DistanceMeters(e.Item2.Gps.GpsLatitude, e.Item2.Gps.GpsLongitude, station.Position.GpsLatitude, station.Position.GpsLongitude) < MaxEdgeDistance);
                var closestCoordinate = new GpsCoordinates();
                Tuple<ShapeConstructor.Point, ShapeConstructor.Point> closestEdge = null;
                double minDistance = double.MaxValue;
                foreach (var edge in closeEdges)
                {
                    foreach (var point in edgeSplit[edge])
                    {
                        var dist = MapFunctions.DistanceMeters(point.GpsLatitude, point.GpsLongitude, station.Position.GpsLatitude, station.Position.GpsLongitude);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestCoordinate = point;
                            closestEdge = edge;
                        }
                    }
                }

                if (closestEdge == null)
                {
                    log.Log(LogMessageType.WARNING_TRAIN_NO_SHAPE_AROUND, $"V okolí {MaxEdgeDistance} metrů stanice {station.Name} nebyla nalezena žádná hrana trasy, ke které by stanice mohla být připnuta. Trasy z/do této stanice budou vždy vedeny vzdušnou čarou.");
                    continue;
                }
                else if (minDistance > DistanceWarning)
                {
                    log.Log(LogMessageType.WARNING_TRAIN_SHAPE_TOO_FAR_FROM_STOP, $"Stanice {station.Name} je od nejbližšího bodu na trase vzdálená {minDistance} metrů.");
                }

                // vytvoříme uprostřed hrany nový bod
                var stationPoint = shapeConstructor.CreateNewPointInEdge(closestCoordinate, closestEdge, out var newEdge1, out var newEdge2);

                // ještě smažeme hranu a přidáme místo ní dvě
                edgeSplit.Add(newEdge1, SplitEdgeToParts(newEdge1));
                edgeSplit.Add(newEdge2, SplitEdgeToParts(newEdge2));

                stationClosestPoints.Add(station, stationPoint);
            }

            return stationClosestPoints;
        }

        private static List<GpsCoordinates> SplitEdgeToParts(Tuple<ShapeConstructor.Point, ShapeConstructor.Point> edge)
        {
            // rozdělíme po metrech
            var nPoints = 2 + Math.Round(MapFunctions.DistanceMeters(edge.Item1.Gps.GpsLatitude, edge.Item1.Gps.GpsLongitude, edge.Item2.Gps.GpsLatitude, edge.Item2.Gps.GpsLongitude));
            var result = new List<GpsCoordinates>() { edge.Item1.Gps };
            for (int i = 1; i < nPoints; i++)
            {
                var distancePercentage = (double)i / nPoints;
                var intermediatePoint = new GpsCoordinates()
                {
                    GpsLatitude = edge.Item1.Gps.GpsLatitude * (1 - distancePercentage) + edge.Item2.Gps.GpsLatitude * distancePercentage,
                    GpsLongitude = edge.Item1.Gps.GpsLongitude * (1 - distancePercentage) + edge.Item2.Gps.GpsLongitude * distancePercentage,
                };

                result.Add(intermediatePoint);
            }

            result.Add(edge.Item2.Gps);
            return result;
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

    }
}
