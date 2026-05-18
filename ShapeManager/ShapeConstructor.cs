using CommonLibrary;
using CommonLibrary.DotNet48;
using CsvSerializer;
using CsvSerializer.Attributes;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShapeManager
{
    /// <summary>
    /// Konstruuje trasy nad sítí na základě souřadnic stanic a dopravních bodů na trase.
    /// Vychází se tedy z toho, že jízdní řád vlaku jednoznačně určuje trasu (což díky slušné hustotě dopravních bodů vesměs platí)
    /// </summary>
    internal class ShapeConstructor
    {
        // Jeden bod sítě (transformuje se z CsvPoint) a jeho sousedé
        public class Point
        {
            public GpsCoordinates Gps { get; private set; }

            public List<Point> Neighbours { get; private set; }

            public Point(double lat, double lon)
            {
                Gps = new GpsCoordinates(lat, lon);
                Neighbours = new List<Point>();
            }

            public Point(GpsCoordinates gps)
            {
                Gps = gps;
                Neighbours = new List<Point>();
            }
        }

        public class Edge
        {
            public Point From { get; private set; }

            public Point To { get; private set; }

            public bool IsOneWay { get; private set; }

            public Edge(Point from, Point to, bool isOneWay) 
            {
                From = from;
                To = to; 
                IsOneWay = isOneWay;
            }
        }

        // Jeden bod sítě v CSV souboru, ze kterého se síť načítá
        private class CsvPoint
        {
            [CsvField("fid", 1)]
            public long LineId { get; set; }

            [CsvField("y", 2)]
            public double Latitude { get; set; }

            [CsvField("x", 3)]
            public double Longitude { get; set; }

            [CsvField("one_way", 4, CsvFieldPostProcess.None, false)]
            public bool IsOneWay { get;set; }
        }


        /// <summary>
        /// Všechny body sítě a seznam sousedů
        /// </summary>
        public List<Point> AllPoints { get; private set; }

        /// <summary>
        /// Index pomáhající hledat hrany v okolí
        /// </summary>
        public STRtree<Edge> SpatialIndex { get; private set; }


        /// <summary>
        /// Inicializuje instanci nahráním sítě z CSV
        /// </summary>
        /// <param name="fileName">CSV soubor s multilines</param>
        public void LoadPointData(string fileName)
        {
            var allLines = CsvFileSerializer.DeserializeFile<CsvPoint>(fileName);
            foreach (var line in allLines)
            {
                if (line.LineId <= 0)
                {
                    throw new Exception($"Neplatné ID čáry v {fileName}. Hodnota 'cis' musí být > 0.");
                }

                if (line.Latitude < 48 || line.Latitude > 52 || line.Longitude < 11 || line.Longitude > 20)
                {
                    throw new Exception($"Neplatná souřadnice ({line.Latitude}, {line.Longitude}) v {fileName}, mimo bounding box ČR.");
                }
            }

            AllPoints = new List<Point>();
            var allPointsByCoordinate = new Dictionary<double, Dictionary<double, Point>>();
            var allEdges = new List<Edge>();
            
            foreach (var multiline in allLines.GroupBy(l => l.LineId))
            {
                var isOneWay = false;
                if (multiline.All(p => p.IsOneWay))
                {
                    isOneWay = true;
                }
                else if (multiline.Any(p => p.IsOneWay)) {
                    throw new Exception($"Multiline {multiline.First().LineId} v souboru {fileName} obsahuje nekonzistentní hodnoty v atributu one_way (musí mít všechny body 0 nebo všechny body 1).");
                }

                var points = multiline.Select(p => new Point(p.Latitude, p.Longitude)).ToArray();
                for (int i = 0; i < points.Length; i++)
                {
                    var existingPoint = allPointsByCoordinate.GetValueOrDefault(points[i].Gps.GpsLatitude)?.GetValueOrDefault(points[i].Gps.GpsLongitude); ;
                    if (existingPoint != null)
                    {
                        points[i] = existingPoint;
                    }
                    else
                    {
                        AllPoints.Add(points[i]);
                        allPointsByCoordinate.GetValueAndAddIfMissing(points[i].Gps.GpsLatitude, new Dictionary<double, Point>()).Add(points[i].Gps.GpsLongitude, points[i]);
                    }
                }

                for (int i = 1; i < points.Length; i++)
                {
                    points[i - 1].Neighbours.Add(points[i]);
                    if (!isOneWay)
                    {
                        points[i].Neighbours.Add(points[i - 1]);
                    }

                    allEdges.Add(new Edge(points[i - 1], points[i], isOneWay));
                }
            }

            BuildSpatialIndex(allEdges);
        }
        
        /// <summary>
        /// Najde trasu mezi stanicemi nebo dopravními body
        /// Hledá se klasicky nejkratší cesta, v ideálním případě by měla existovat právě jedna přímá cesta mezi dvěma sousedními body
        /// </summary>
        /// <param name="pointFrom">Výchozí bod</param>
        /// <param name="pointTo">Cílový bod</param>
        /// <returns>Trasa mezi stanicemi nebo null, pokud nebyla nalezena</returns>
        public List<GpsCoordinates> FindPath(Point pointFrom, Point pointTo)
        {
            if (pointFrom == null || pointTo == null)
            {
                return null;
            }

            var distances = new Dictionary<Point, double>
            {
                [pointFrom] = 0
            };

            // Dijkstra
            var openVertices = new MinHeap<(Point, double)>();
            openVertices.Enqueue((pointFrom, 0), PointDistance(pointFrom, pointTo));
            var closedVertices = new HashSet<Point>();
            var predecessors = new Dictionary<Point, Point>() { { pointFrom, null } };
            while (openVertices.Any())
            {
                var (minPoint, minDistance) = openVertices.Dequeue();

                // už jsme zpracovali lepší variantu
                if (closedVertices.Contains(minPoint))
                    continue;

                closedVertices.Add(minPoint);

                if (minPoint == pointTo)
                {
                    // cesta nalezena, rekonstruujeme od konce
                    var iterator = minPoint;
                    var resultPath = new List<GpsCoordinates>();
                    do
                    {
                        resultPath.Add(iterator.Gps);
                        iterator = predecessors.GetValueOrDefault(iterator);
                    } while (iterator != null);

                    resultPath.Reverse();
                    return resultPath;
                }

                foreach (var neighbour in minPoint.Neighbours)
                {
                    if (closedVertices.Contains(neighbour))
                        continue;

                    var distance = minDistance + PointDistance(minPoint, neighbour); 
                    var heuristic = PointDistance(neighbour, pointTo);

                    if (!distances.TryGetValue(neighbour, out var oldDist) || distance < oldDist)
                    {
                        distances[neighbour] = distance;
                        predecessors[neighbour] = minPoint;
                        openVertices.Enqueue((neighbour, distance), distance + heuristic);
                    }
                }
            }

            return null; // trasa se nenašla
        }

        private double PointDistance(Point a, Point b)
        {
            return MapFunctions.DistanceMeters(
                a.Gps.GpsLatitude,
                a.Gps.GpsLongitude,
                b.Gps.GpsLatitude,
                b.Gps.GpsLongitude);
        }

        /// <summary>
        /// Rozdělí hranu podle zadaných bodů. Vrátí ke každé zadané souřadnici na hraně nově vytvořený bod
        /// </summary>
        /// <param name="edge">Hrana</param>
        /// <param name="splitPoints">Body</param>
        /// <returns></returns>
        public Dictionary<GpsCoordinates, Point> SplitEdgeByMultiplePoints(Edge edge, List<GpsCoordinates> splitPoints)
        {
            if (splitPoints == null || splitPoints.Count == 0)
                return new Dictionary<GpsCoordinates, Point>();

            Disconnect(edge.From, edge.To, edge.IsOneWay);

            // 1) spočítáme parametr t (0..1) pro každý bod na hraně
            var ordered = splitPoints
                .Select(p => new
                {
                    Point = p,
                    T = ProjectT(edge, p) // 0 = From, 1 = To
                })
                .OrderBy(x => x.T)
                .ToList();

            var result = new Dictionary<GpsCoordinates, Point>();

            // 2) start
            var currentFrom = edge.From;
            Point prevPoint = edge.From;

            foreach (var item in ordered)
            {
                var midPoint = new Point(item.Point);
                AllPoints.Add(midPoint);

                // uprava sousedů
                Connect(currentFrom, midPoint, edge.IsOneWay);

                result[item.Point] = midPoint;

                currentFrom = midPoint;
            }

            // 3) poslední segment
            Connect(currentFrom, edge.To, edge.IsOneWay);

            return result;
        }

        private double ProjectT(Edge edge, GpsCoordinates p)
        {
            var ax = edge.From.Gps.GpsLongitude;
            var ay = edge.From.Gps.GpsLatitude;

            var bx = edge.To.Gps.GpsLongitude;
            var by = edge.To.Gps.GpsLatitude;

            var px = p.GpsLongitude;
            var py = p.GpsLatitude;

            var abx = bx - ax;
            var aby = by - ay;

            var apx = px - ax;
            var apy = py - ay;

            var abLen2 = abx * abx + aby * aby;

            if (abLen2 == 0)
                return 0;

            return (apx * abx + apy * aby) / abLen2;
        }

        private void Connect(Point a, Point b, bool oneWay)
        {
            a.Neighbours.Add(b);

            if (!oneWay)
                b.Neighbours.Add(a);
        }

        private void Disconnect(Point a, Point b, bool oneWay)
        {
            a.Neighbours.Remove(b);

            if (!oneWay)
                b.Neighbours.Remove(a);
        }

        private void BuildSpatialIndex(List<Edge> allEdges)
        {
            SpatialIndex = new STRtree<Edge>();

            foreach (var edge in allEdges)
            {
                // obálka (bounding box) hrany
                var envelope = new Envelope(
                    Math.Min(edge.From.Gps.GpsLongitude, edge.To.Gps.GpsLongitude),
                    Math.Max(edge.From.Gps.GpsLongitude, edge.To.Gps.GpsLongitude),
                    Math.Min(edge.From.Gps.GpsLatitude, edge.To.Gps.GpsLatitude),
                    Math.Max(edge.From.Gps.GpsLatitude, edge.To.Gps.GpsLatitude)
                );

                SpatialIndex.Insert(envelope, edge);
            }

            // postaví interní strukturu stromu
            SpatialIndex.Build();
        }
    }
}
