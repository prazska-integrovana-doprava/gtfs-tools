using System.IO;

namespace GtfsLogging
{
    /// <summary>
    /// Nejjednodušší logger, prostě píše do souboru řádky tak jak přicházejí
    /// </summary>
    public class SimpleLogger : BaseTextLogger, ISimpleLogger
    {
        public SimpleLogger(TextWriter writer)
            : base(writer)
        {
        }

        public void Log(string text)
        {
            Writer.WriteLine(text);
        }
    }
}
