using CommonLibrary;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Jeden bod trasy z <see cref="Shape"/>
    /// </summary>
    public class ShapePoint
    {
        /// <summary>
        /// Souřadnice bodu
        /// </summary>
        public GpsCoordinates Position { get; set; }

        /// <summary>
        /// Pořadí na trase (musí tvořit rostoucí posloupnost)
        /// </summary>
        public int SequenceIndex { get; set; }
        
        /// <summary>
        /// Vzdálenost bodu od počátku trasy
        /// </summary>
        public double DistanceTraveledMeters { get; set; }

        /// <summary>
        /// Vytvoří data o jednom bodu na trase z GTFS záznamu.
        /// </summary>
        /// <param name="gtfsShapePoint">GTFS záznam</param>
        public static ShapePoint Construct(GtfsShapePoint gtfsShapePoint)
        {
            return new ShapePoint()
            {
                DistanceTraveledMeters = gtfsShapePoint.DistanceTraveled * 1000,
                Position = new GpsCoordinates(gtfsShapePoint.Latitude, gtfsShapePoint.Longitude),
                SequenceIndex = gtfsShapePoint.Sequence,
            };
        }

        /// <summary>
        /// Vytvoří GTFS záznam s bodem v shape
        /// </summary>
        /// <param name="shapeId">ID shapu</param>
        /// <returns></returns>
        public GtfsShapePoint ToGtfsShapePoint(string shapeId)
        {
            return new GtfsShapePoint()
            {
                ShapeId = shapeId,
                Latitude = Position.GpsLatitude,
                Longitude = Position.GpsLongitude,
                Sequence = SequenceIndex,
                DistanceTraveled = DistanceTraveledMeters / 1000.0,
            };
        }

        public override string ToString()
        {
            return $"{Position} ({DistanceTraveledMeters} m)";
        }
    }
}
