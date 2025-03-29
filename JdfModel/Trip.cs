using CsvSerializer.Attributes;
using System.Linq;

namespace JdfModel
{
    public class Trip
    {
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string RouteId { get; set; }

        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string FixedCode1 { get; set; }

        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public string FixedCode2 { get; set; }

        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string FixedCode3 { get; set; }

        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string FixedCode4 { get; set; }

        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string FixedCode5 { get; set; }

        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string FixedCode6 { get; set; }

        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string FixedCode7 { get; set; }

        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public string FixedCode8 { get; set; }

        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string FixedCode9 { get; set; }

        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string FixedCode10 { get; set; }

        public string[] FixedCodes
        {
            get
            {
                return new[] { FixedCode1, FixedCode2, FixedCode3, FixedCode4, FixedCode5, FixedCode6, FixedCode7, FixedCode8, FixedCode9, FixedCode10 }
                .Where(v => !string.IsNullOrEmpty(v)).ToArray();
            }
        }
    }
}
