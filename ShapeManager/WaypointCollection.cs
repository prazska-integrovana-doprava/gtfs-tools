using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CommonLibrary;
using GtfsModel.Extended;

namespace ShapeManager
{
    [XmlRoot("waypointCollection")]
    public class WaypointCollection
    {
        [XmlElement("waypoints")]
        public List<WaypointsForStops> Waypoints { get; set; }

        public WaypointCollection()
        {
            Waypoints = new List<WaypointsForStops>();
        }

        public static WaypointCollection Load(string filePath)
        {
            var serializer = new XmlSerializer(typeof(WaypointCollection));

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                return (WaypointCollection)serializer.Deserialize(stream);
            }
        }

        public IEnumerable<Waypoint> FindWaypoints(Stop from, Stop to)
        {
            if (from == null || to == null)
                return Enumerable.Empty<Waypoint>();

            foreach (var waypoints in Waypoints)
            {
                var regexFrom = new Regex(waypoints.From);
                var regexTo = new Regex(waypoints.To);
                if (regexFrom.IsMatch("^" + from.GtfsId + "$") && regexTo.IsMatch("^" + to.GtfsId + "$"))
                {
                    return waypoints.Waypoints;
                }
            }

            return Enumerable.Empty<Waypoint>();
        }
    }

    public class WaypointsForStops
    {
        [XmlAttribute("from")]
        public string From { get; set; }

        [XmlAttribute("to")]
        public string To { get; set; }

        [XmlElement("waypoint")]
        public List<Waypoint> Waypoints { get; set; }
    }

    public class Waypoint
    {
        [XmlAttribute("lat")]
        public double Lat { get; set; }

        [XmlAttribute("lon")]
        public double Lon { get; set; }

        public GpsCoordinates ToGpsCoordinates()
        {
            return new GpsCoordinates()
            {
                GpsLatitude = Lat,
                GpsLongitude = Lon,
            };
        }
    }
}
