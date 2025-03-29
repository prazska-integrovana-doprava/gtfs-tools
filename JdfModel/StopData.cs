using CsvSerializer.Attributes;

namespace JdfModel
{
    public class StopData
    {
        [CsvField("InterniCislo", 1)]
        public int StopId { get; set; }

        [CsvField("Zeměpisná šířka", 21)]
        public int LatitudeTimes10000 { get; set; }

        [CsvField("Zeměpisná délka", 22)]
        public int LongitudeTimes10000 { get; set; }
    }
}
