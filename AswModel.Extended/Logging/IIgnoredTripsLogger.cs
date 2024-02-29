using GtfsLogging;

namespace AswModel.Extended.Logging
{
    /// <summary>
    /// Logování neveřejných spojů
    /// </summary>
    interface IIgnoredTripsLogger : ILogger
    {
        void Log(string reason, Trip obj);
    }
}
