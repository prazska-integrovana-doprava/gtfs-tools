using System.IO;

namespace GtfsLogging
{
    /// <summary>
    /// Základ pro každý logger (pouze ukládá <see cref="TextWriter"/> zadaný v konstruktoru)
    /// </summary>
    public abstract class BaseTextLogger : ILogger
    {
        protected TextWriter Writer { get; set; }

        public BaseTextLogger(TextWriter writer)
        {
            Writer = writer;
        }

        public virtual void Close()
        {
            Writer.Close();
        }
    }
}
