using CsvSerializer.Attributes;

namespace JdfModel
{
    public class FixedCode
    {
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string CodeId { get; set; }

        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public char CodeChar { get; set; }
    }
}
