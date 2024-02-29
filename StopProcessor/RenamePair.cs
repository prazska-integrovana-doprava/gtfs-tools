using CsvSerializer.Attributes;

namespace StopProcessor
{
    internal class RenamePair
    {
        [CsvField("short_name", 1)]
        public string ShortVersion { get; set; }

        [CsvField("long_name", 2)]
        public string LongVersion { get; set; }
    }
}
