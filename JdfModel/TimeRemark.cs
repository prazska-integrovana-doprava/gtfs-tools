using CsvSerializer.Attributes;
using System;

namespace JdfModel
{
    public static class TimeRemarkTypes
    {
        public const string OperatesOn = "1";

        public const string DoesNotOperateOn = "4";
    }

    public class TimeRemark
    {
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string RouteId { get; set; }

        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int TripNumber { get; set; }

        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public int TimeRemarkNumber { get; set; }

        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public string TimeRemarkName { get; set; }

        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string TimeRemarkType { get; set; }

        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public DateTime? DateFrom { get; set; }

        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public DateTime? DateTo { get; set; }

        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string Text { get; set; }
    }
}
