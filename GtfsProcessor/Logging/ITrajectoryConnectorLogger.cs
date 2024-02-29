using AswModel.Extended;
using GtfsLogging;

namespace GtfsProcessor.Logging
{
    interface ITrajectoryConnectorLogger : ILogger
    {
        /// <summary>
        /// Zaznamená chybu v propojování dvou mezizastávkových úseků (a trasu, na které se to stalo).
        /// </summary>
        /// <param name="from">První úsek</param>
        /// <param name="to">Druhý úsek</param>
        /// <param name="shapeId">ID trasy, na které došlo k chybě</param>
        void LogUnconnected(ShapeFragmentDescriptor fragment, double distanceMeters, Trip referingTrip);
    }
}
