using GtfsLogging;

namespace TrainsEditor.GtfsExport
{
    static class Loggers
    {
        public static ICommonLogger AswDataLoaderLoggerInstance;

        public static ICommonLogger TrainsLoaderLoggerInstance;

        public static ISimpleLogger TrainsProcessLoggerInstance;

        public static ISimpleLogger TrainsOutputLoggerInstance;

        public static void InitAswDataLoggers(string logFolder)
        {
            var logFactory = new LogWriterFactory(logFolder);
            AswDataLoaderLoggerInstance = new CommonLogger(logFactory.CreateWriterToFile("TrainsEditor_AswData"));
        }

        public static void ClLoseAswDataLoggers()
        {
            AswDataLoaderLoggerInstance.Close();
        }

        public static void InitExportModuleLoggers(string logFolder)
        {
            var logFactory = new LogWriterFactory(logFolder);

            TrainsLoaderLoggerInstance = new CommonLogger(logFactory.CreateWriterToFile("TrainsEditor_Loader"));
            TrainsProcessLoggerInstance = new SimpleLogger(logFactory.CreateWriterToFile("TrainsEditor_Process"));
            TrainsOutputLoggerInstance = new SimpleLogger(logFactory.CreateWriterToFile("TrainsEditor_Output"));
        }

        public static void CloseExportModuleLoggers()
        {
            TrainsLoaderLoggerInstance.Close();
            TrainsProcessLoggerInstance.Close();
            TrainsOutputLoggerInstance.Close();
        }
    }
}
