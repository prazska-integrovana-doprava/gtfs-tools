using System.Xml.Serialization;

namespace JdfToGtfsProcessor.Stops
{
    [XmlRoot("PIDSL")]
    public class StopDataCollection
    {
        [XmlAttribute("dt")]
        public DateTime DateTimeCreated { get; set; }

        [XmlElement("z")]
        public required List<StopData> Stops { get; set; }

        public Dictionary<int, Dictionary<string, StopData>> GetStopsByNumbers()
        {
            var result = new Dictionary<int, Dictionary<string, StopData>>();

            if (Stops == null)
                return result;

            foreach (var stop in Stops)
            {
                if (!result.TryGetValue(stop.CisNumber, out var platformDict))
                {
                    platformDict = new Dictionary<string, StopData>();
                    result[stop.CisNumber] = platformDict;
                }

                // Pokud existuje stejný PlatformCode, přepíše se
                platformDict[stop.PlatformCode] = stop;
            }

            return result;
        }
    }

    public class StopData
    {
        [XmlAttribute("cis")]
        public int CisNumber { get; set; }

        [XmlAttribute("st")]
        public required string PlatformCode { get; set; }

        [XmlElement("sl")]
        public required StopCoordinates Coordinates { get; set; }
    }

    public class StopCoordinates
    {
        [XmlAttribute("lat")]
        public double GpsLatitude { get; set; }

        [XmlAttribute("lng")]
        public double GpsLongitude { get; set; }

        [XmlAttribute("r")]
        public int Radius { get; set; }
    }
}