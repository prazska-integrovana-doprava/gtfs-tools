using CsvSerializer.Attributes;
using System;

namespace JdfModel
{
    public class Route
    {
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string RouteId { get; set; }

        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public string RouteName { get; set; }

        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string OperatorId { get; set; }

        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public char RouteType { get; set; }

        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public char TrafficType { get; set; }

        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public bool IsExceptional { get; set; }

        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public bool Unused1 { get; set; }

        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public bool Unused2 { get; set; }

        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public bool Unused3 { get; set; }

        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public string Unused4 { get; set; }

        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string Unused5 { get; set; }

        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string Unused6 { get; set; }

        [CsvField("", 13, CsvFieldPostProcess.Quote)]
        public string Unused7 { get; set; }

        [CsvField("", 14, CsvFieldPostProcess.Quote)]
        public DateTime ValidFrom { get; set; }

        [CsvField("", 15, CsvFieldPostProcess.Quote)]
        public DateTime ValidTo { get; set; }
    }
}
