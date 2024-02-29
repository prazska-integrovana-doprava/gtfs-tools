using CommonLibrary;
using GtfsLogging;

namespace AswModel.Extended.Logging
{
    /// <summary>
    /// Logování chyb s trasami v mezizastávkových úsecích (chybějící varianty, duplicitní varianty)
    /// </summary>
    public interface ITrajectoryDbLogger : ILogger
    {
        /// <summary>
        /// Zaznamená duplicitní mezizastávkový úsek.
        /// </summary>
        /// <param name="descriptor">Popis mezizastávkového úseku</param>
        void LogDuplicate(ShapeFragmentDescriptor descriptor, ServiceDaysBitmap serviceAsBits);

        /// <summary>
        /// Zaznamená chybějící mezizastávkový úsek (a trasu, na které chybí).
        /// </summary>
        /// <param name="descriptor">Popis mezizastávkového úseku</param>
        /// <param name="shapeId">ID trasy, na které chybí</param>
        void LogMissing(ShapeFragmentDescriptor descriptor, Trip referingTrip);

        /// <summary>
        /// Zaznamená mezizastávkový úsek, který je přítomen, ale nebyl nalezen v požadované verzi.
        /// Tj. že chybí dané verze anebo je rozštěpeno do více verzí a prozatím to neumíme složit.
        /// </summary>
        /// <param name="descriptor">Popis mezizastávkového úseku</param>
        /// <param name="referingTrip">ID trasy, na které chybí</param>
        void LogPartiallyMissing(ShapeFragmentDescriptor descriptor, Trip referingTrip);
    }
}
