using GtfsLogging;
using System;

namespace GtfsProcessor.Logging
{
    static class Loggers
    {
        public static ICommonLogger CommonLoggerInstance;

        public static ISimpleLogger TransferLoggerInstance;

        public static ITrajectoryConnectorLogger TrajectoryConnectorLoggerInstance;

        public static IMergedTripsLogger MergedTripsLoggerInstance;

        public static ISimpleLogger CalendarLoggerInstance;

        public static void InitLoggers(string logFolder, int maxSimilarLogRecords)
        {
            var logFactory = new LogWriterFactory(logFolder);

            AswModel.Extended.Logging.Loggers.InitLoggers(
                logFactory.CreateWriterToFile("GtfsProcess_Asw_IgnoredTrips"),
                logFactory.CreateWriterToFile("GtfsProcess_Asw_TrajectoryDb"),
                logFactory.CreateWriterToFile("GtfsProcess_Asw_Common"),
                maxSimilarLogRecords,
                Console.Error
                );

            CommonLoggerInstance = new CommonLogger(logFactory.CreateWriterToFile("GtfsProcess_Common"), maxSimilarLogRecords, Console.Error);
            TransferLoggerInstance = new SimpleLogger(logFactory.CreateWriterToFile("GtfsProcess_Transfers"));
            TrajectoryConnectorLoggerInstance = new TrajectoryConnectorLogger(logFactory.CreateWriterToFile("GtfsProcess_TrajectoryDbConnect"));
            MergedTripsLoggerInstance = new MergedTripsLogger(logFactory.CreateWriterToFile("GtfsProcess_MergedTrips"));
            CalendarLoggerInstance = new SimpleLogger(logFactory.CreateWriterToFile("GtfsProcess_Calendar"));
        }

        public static void CloseLoggers()
        {
            CommonLoggerInstance.Close();
            TransferLoggerInstance.Close();
            TrajectoryConnectorLoggerInstance.Close();
            MergedTripsLoggerInstance.Close();
            CalendarLoggerInstance.Close();

            AswModel.Extended.Logging.Loggers.CloseLoggers();
        }
    }
}
