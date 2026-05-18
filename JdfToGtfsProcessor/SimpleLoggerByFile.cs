using GtfsLogging;

namespace JdfToGtfsProcessor
{
    /// <summary>
    /// Nadstavba nad SimpleLoggerem, která navíc prefixuje záznamy názvem souboru
    /// </summary>
    internal class SimpleLoggerByFile : ISimpleLogger
    {
        private readonly string path;

        private readonly ISimpleLogger log;

        public SimpleLoggerByFile(string path, ISimpleLogger simpleLogger)
        {
            this.path = path;
            log = simpleLogger;
        }

        public void Log(string text)
        {
            log.Log($"{path}: {text}");
        }

        public void Close()
        {
            log.Close();
        }
    }
}
