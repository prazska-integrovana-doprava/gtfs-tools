using GtfsLogging;
using System.IO;

namespace AswModel.Extended.Logging
{
    public static class Loggers
    {
        internal static IIgnoredTripsLogger IgnoredTripsLoggerInstance = new NullIgnoredTripsLogger();

        internal static ITrajectoryDbLogger TrajectoryDbLoggerInstance = new NullTrajectoryDbLogger();

        internal static ICommonLogger DataLoggerInstance = new NullCommonLogger();

        public static void InitLoggers(TextWriter ignoredTripsWriter, TextWriter trajectoryDbLogWriter, TextWriter dataLogWriter, int maxSimilarLogRecords = int.MaxValue, TextWriter dataLogErrorWriter = null)
        {
            IgnoredTripsLoggerInstance = new IgnoredTripsLogger(ignoredTripsWriter);
            TrajectoryDbLoggerInstance = new TrajectoryDbLogger(trajectoryDbLogWriter);
            DataLoggerInstance = new CommonLogger(dataLogWriter, maxSimilarLogRecords, dataLogErrorWriter);
        }

        public static void CloseLoggers()
        {
            IgnoredTripsLoggerInstance.Close();
            TrajectoryDbLoggerInstance.Close();
            DataLoggerInstance.Close();
        }
    }
}
