using GtfsLogging;

namespace GtfsProcessor.Logging
{
    internal interface IMergedTripsLogger : ILogger
    {
        /// <summary>
        /// Zaloguje spoje, které byly sloučeny do jednoho
        /// </summary>
        /// <param name="result">Výsledný spoj</param>
        /// <param name="firstOriginalCalendar">Původní kalendář prvního spoje</param>
        /// <param name="second">Původní druhý spoj</param>
        void LogMerged(LoggedTrip result, LoggedTrip first, LoggedTrip second);

        /// <summary>
        /// Zaloguje spoje, jejichž porovnání skončilo nestandardně
        /// </summary>
        /// <param name="first">První spoj</param>
        /// <param name="second">Druhý spoj</param>
        /// <param name="equalityResult">Výsledek porovnání</param>
        void LogComment(LoggedTrip first, LoggedTrip second, TripEqualityResult equalityResult);
    }
}
