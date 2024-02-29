using GtfsLogging;
using System.IO;

namespace AswModel.Extended.Logging
{
    class IgnoredTripsLogger : BaseTextLogger, IIgnoredTripsLogger
    {        
        public IgnoredTripsLogger(TextWriter writer)
            : base(writer)
        {
        }
        
        public void Log(string reason, Trip obj)
        {
            Writer.WriteLine($"{obj}: {reason}");
        }
    }
}
