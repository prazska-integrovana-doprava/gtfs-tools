using CommonLibrary;
using GtfsModel.Extended;

namespace GtfsProcessor.DataClasses
{
    /// <summary>
    /// Doplnění GTFS Extended třídy <see cref="Shape"/> o dodatečné informace
    /// </summary>
    class ShapeEx : Shape
    {
        /// <summary>
        /// Vzorový trip, podle kterého jsme Shape vytvořili (jen pro info)
        /// </summary>
        public MergedTripGroup ReferenceTrip { get; set; }

        /// <summary>
        /// Vyznačuje body na trase, které odpovídají zastávkám spoje
        /// Délka odpovídá <see cref="ReferenceTrip.StopTimes.Length"/>.
        /// 
        /// Hodí se pro nastavení kilometráže spojům se shodnou trasou
        /// </summary>
        public ShapePoint[] PointsForStopTimes { get; set; }

        /// <summary>
        /// Platnost trasy
        /// TODO používá se na něco?
        /// </summary>
        public ServiceDaysBitmap ServiceAsBits { get; set; }
    }
}
