using CsvSerializer.Attributes;

namespace JdfModel
{
    public class Agency
    {
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string Id { get; set; }

        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public string Unused1 { get; set; }

        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string Name { get; set; }

        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public int Unused2 { get; set; }

        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public string Unused3 { get; set; }

        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string Address { get; set; }

        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public string PhoneToAddress { get; set; }

        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public string Unused4 { get; set; }

        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public string Unused5 { get; set; }

        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public string Unused6 { get; set; }

        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string Unused7 { get; set; }

        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public string Url { get; set; }
    }
}
