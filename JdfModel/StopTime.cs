using CsvSerializer.Attributes;
using System.Linq;

namespace JdfModel
{
    public class StopTime
    {
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string RouteId { get; set; }

        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public int StopIndex { get; set; }

        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public int StopId { get; set; }

        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string Unused1 { get; set; }

        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string PlatformCode { get; set; }

        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string FixedCode1 { get; set; }

        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string FixedCode2 { get; set; }

        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string FixedCode3 { get; set; }

        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public float? DistanceFromStartKm { get; set; }

        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string ArrivalTime { get; set; }

        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string DepartureTime { get; set; }

        public string[] FixedCodes
        {
            get
            {
                return new[] { FixedCode1, FixedCode2, FixedCode3 }
                .Where(v => !string.IsNullOrEmpty(v)).ToArray();
            }
        }
    }
}
