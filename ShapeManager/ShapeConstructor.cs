using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using CsvSerializer;
using CsvSerializer.Attributes;

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
            [CsvField("cis", 1)]
            public long LineId { get; set; }

            [CsvField("lat", 2)]
            public double Latitude { get; set; }

            [CsvField("lon", 3)]
            public double Longitude { get; set; }

            [CsvField("one_way", 4, CsvFieldPostProcess.None, false)]
            public bool IsOneWay { get;set; }
        }


        /// <summary>
        /// Všechny body sítě a seznam sousedů
        /// </summary>
        public List<Point> AllPoints { get; private set; }

        /// <summary>
        /// všechny hrany sítě, obousměrné (tedy za oba směry jedna hrana)
        /// </summary>
        public List<Edge> AllEdges { get; private set; }
        
        // indexováno x a y, vychází z toho, že ve vstupním souboru sítě jsou definované vícebodové sekvence (multihrany), které
        // se vzájemně potkávají právě v těchto border points, skrz které je musíme propojit
        private Dictionary<double, Dictionary<double, Point>> borderPoints;
                
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
            borderPoints = new Dictionary<double, Dictionary<double, Point>>();
            AllEdges = new List<Edge>();
            
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
                AllPoints.AddRange(points.Skip(1).Take(points.Length - 2)); // bez prvního a posledního, ty až podmínečně

                var firstPointAlreadyLoaded = FindBorderPointByPosition(points.First().Gps);
                if (firstPointAlreadyLoaded != null)
                {
                    points[0] = firstPointAlreadyLoaded;
                }
                else
                {
                    AddBorderPoint(points.First());
                }

                var lastPointAlreadyLoaded = FindBorderPointByPosition(points.Last().Gps);
                if (lastPointAlreadyLoaded != null)
                {
                    points[points.Length - 1] = lastPointAlreadyLoaded;
                }
                else
                {
                    AddBorderPoint(points.Last());
                }

                for (int i = 1; i < points.Length; i++)
                {
                    points[i - 1].Neighbours.Add(points[i]);
                    if (!isOneWay)
                    {
                        points[i].Neighbours.Add(points[i - 1]);
                    }

                    AllEdges.Add(new Edge(points[i - 1], points[i], isOneWay));
                }
            }
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

            // Dijkstra
            var openVertices = new Dictionary<Point, double>() { { pointFrom, 0 } };
            var closedVertices = new HashSet<Point>();
            var predecessors = new Dictionary<Point, Point>() { { pointFrom, null } };
            while (openVertices.Any())
            {
                var minDistance = openVertices.Values.Min();
                var minPoint = openVertices.First(v => v.Value == minDistance).Key;
                openVertices.Remove(minPoint);
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
                    var distance = minDistance + MapFunctions.DistanceMeters(minPoint.Gps.GpsLatitude, minPoint.Gps.GpsLongitude, neighbour.Gps.GpsLatitude, neighbour.Gps.GpsLongitude);
                    if (closedVertices.Contains(neighbour))
                        continue;

                    if (!openVertices.ContainsKey(neighbour))
                    {
                        openVertices.Add(neighbour, distance);
                        predecessors.Add(neighbour, minPoint);
                    }
                    else if (openVertices[neighbour] > distance)
                    {
                        openVertices[neighbour] = distance;
                        predecessors[neighbour] = minPoint;
                    }
                }
            }

            return null; // trasa se nenašla
        }

        /// <summary>
        /// Rozdělí hranu na dvě zadaným bodem a vše propojí. Původní hranu smaže a vloží dvě nové i nový bod.
        /// </summary>
        /// <param name="gpsCoordinate">Souřadnice na hraně</param>
        /// <param name="edge">Hrana k rozdělení</param>
        /// <param name="newEdge1">Nová hrana 1</param>
        /// <param name="newEdge2">Nová hrana 2</param>
        /// <returns>Nově vytvořený bod</returns>
        public Point CreateNewPointInEdge(GpsCoordinates gpsCoordinate, Edge edge, out Edge newEdge1, out Edge newEdge2)
        {
            var resultPoint = new Point(gpsCoordinate);
            edge.From.Neighbours.Remove(edge.To);
            edge.From.Neighbours.Add(resultPoint);
            resultPoint.Neighbours.Add(edge.To);
            if (!edge.IsOneWay)
            {
                edge.To.Neighbours.Remove(edge.From);
                edge.To.Neighbours.Add(resultPoint);
                resultPoint.Neighbours.Add(edge.From);
            }

            // ještě smažeme hranu a přidáme místo ní dvě
            AllEdges.Remove(edge);
            newEdge1 = new Edge(edge.From, resultPoint, edge.IsOneWay);
            AllEdges.Add(newEdge1);
            newEdge2 = new Edge(resultPoint, edge.To, edge.IsOneWay);
            AllEdges.Add(newEdge2);

            AllPoints.Add(resultPoint);
            return resultPoint;
        }

        private Point FindBorderPointByPosition(GpsCoordinates gps)
        {
            return borderPoints.GetValueOrDefault(gps.GpsLatitude)?.GetValueOrDefault(gps.GpsLongitude);
        }

        private void AddBorderPoint(Point p)
        {
            AllPoints.Add(p);
            borderPoints.GetValueAndAddIfMissing(p.Gps.GpsLatitude, new Dictionary<double, Point>()).Add(p.Gps.GpsLongitude, p);
        }
    }
}
