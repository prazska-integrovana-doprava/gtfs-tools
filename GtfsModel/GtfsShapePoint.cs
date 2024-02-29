using CsvSerializer.Attributes;

namespace GtfsModel
{
    /// <summary>
    /// Jeden bod trasy
    /// </summary>
    public class GtfsShapePoint
    {
        /// <summary>
        /// Identifikátor trasy, do které bod patří.
        /// </summary>
        [CsvField("shape_id", 1)]
        public string ShapeId { get; set; }

        /// <summary>
        /// Zeměpisná šířka bodu
        /// </summary>
        [CsvField("shape_pt_lat", 2)]
        public double Latitude { get; set; }

        /// <summary>
        /// Zeměpisná délka bodu
        /// </summary>
        [CsvField("shape_pt_lon", 3)]
        public double Longitude { get; set; }

        /// <summary>
        /// Sekvence (pro body na trase musí být vzestupné)
        /// </summary>
        [CsvField("shape_pt_sequence", 4)]
        public int Sequence { get; set; }

        /// <summary>
        /// Ujetá vzdálenost po trase
        /// </summary>
        [CsvField("shape_dist_traveled", 5)]
        public double DistanceTraveled { get; set; }
    }
}
