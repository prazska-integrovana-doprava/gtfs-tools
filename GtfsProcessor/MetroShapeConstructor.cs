using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using CsvSerializer;
using AswModel.Extended;
using GtfsProcessor.DataClasses;
using GtfsLogging;

namespace GtfsProcessor
{
    class MetroShapeConstructor
    {
        // vzdálenost, při které reportujeme, že je stanice moc daleko - vztahuje se ke vzdálenosti přímo ke trati (po splitu hran)
        private const double DistanceWarning = 30; // metrů

        private Dictionary<Stop, Dictionary<Stop, List<GpsCoordinates>>> paths = new Dictionary<Stop, Dictionary<Stop, List<GpsCoordinates>>>();

        // již nalezené trasy nad sítí vlaků mezi dvojicemi sousedních zastávek
        private readonly ShapeConstructor shapeConstructor = new ShapeConstructor();

        // načtené body v síti reprezentující pozice stanic nad sítí
        private Dictionary<Stop, ShapeConstructor.Point> stopsToPointsMapping;

        private ICommonLogger log;

        /// <summary>
        /// Vytvoří instanci shape databáze a inicializuje konstruktor tras.
        /// </summary>
        /// <param name="shapeConstructor">Instance, která má načtenou síť</param>
        /// <param name="stops">Všechny použité zastávky a dopravní body a jejich mapování na síť v <paramref name="shapeConstructor"/>.</param>
        public MetroShapeConstructor(ShapeConstructor shapeConstructor, IEnumerable<Stop> stations, ICommonLogger log)
        {
            this.shapeConstructor = shapeConstructor;
            this.stopsToPointsMapping = MapStationsOnNetwork(stations, log);
            this.log = log;
        }

        public ShapeEx CreateShapeForTrip(MergedTripGroup trip)
        {
            var result = new ShapeEx();
            result.Points = new List<GtfsModel.Extended.ShapePoint>();
            result.ReferenceTrip = trip;
            result.PointsForStopTimes = new GtfsModel.Extended.ShapePoint[trip.StopTimes.Length];
            result.ServiceAsBits = ServiceDaysBitmap.CreateAlwaysValidBitmap(trip.ServiceAsBits.Length);

            double currentDistance = 0;
            var stopTimeFrom = trip.StopTimes[0];

            for (int i = 1; i < trip.StopTimes.Length; i++)
            {
                var stopTimeTo = trip.StopTimes[i];
                if (!stopTimeTo.Stop.IsPublic)
                    continue;

                var path = FindOrCreatePath(stopTimeFrom.Stop, stopTimeTo.Stop);

                foreach (var point in path)
                {
                    if (result.Points.Any())
                    {
                        var prevPos = result.Points.Last().Position;
                        if (point.Equals(prevPos))
                            continue; // může se stát na zlomu u zastávky (poslední bod předchozího úseku je shodný s tímto úsekem)

                        currentDistance += MapFunctions.DistanceMeters(prevPos.GpsLatitude, prevPos.GpsLongitude, point.GpsLatitude, point.GpsLongitude);
                    }

                    result.Points.Add(new GtfsModel.Extended.ShapePoint()
                    {
                        Position = point,
                        DistanceTraveledMeters = currentDistance,
                    });
                }

                result.PointsForStopTimes[i] = result.Points.Last();
                stopTimeFrom = stopTimeTo;
            }

            result.PointsForStopTimes[0] = result.Points.First();
            return result;
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
                log.Log(LogMessageType.WARNING_TRAIN_SHAPE_PATH_NOT_FOUND, $"Cesta ze stanice {from.Name} {from.NodeId}/{from.StopId} do stanice {to.Name} {to.NodeId}/{to.StopId} nebyla nalezena. Používám přímé propojení.");
                return new List<GpsCoordinates>() 
                {
                    new GpsCoordinates(from.Position.GpsLatitude, from.Position.GpsLongitude),
                    new GpsCoordinates(to.Position.GpsLatitude, to.Position.GpsLongitude) 
                };
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
            var stationClosestPoints = new Dictionary<Stop, ShapeConstructor.Point>();
            foreach (var station in stations)
            {
                // hrany s alespoň jedním krajním bodem nedaleko hledané stanice
                ShapeConstructor.Point closestPoint = null;
                double minDistance = double.MaxValue;
                foreach (var point in shapeConstructor.AllPoints)
                {
                    var dist = MapFunctions.DistanceMeters(point.Gps.GpsLatitude, point.Gps.GpsLongitude, station.Position.GpsLatitude, station.Position.GpsLongitude);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPoint = point;
                    }
                }

                if (minDistance > DistanceWarning)
                {
                    log.Log(LogMessageType.WARNING_TRAIN_SHAPE_TOO_FAR_FROM_STOP, $"Stanice {station.Name} {station.NodeId}/{station.StopId} je od nejbližšího bodu na trase vzdálená {minDistance} metrů.");
                }

                stationClosestPoints.Add(station, closestPoint);
            }

            return stationClosestPoints;
        }


    }
}
