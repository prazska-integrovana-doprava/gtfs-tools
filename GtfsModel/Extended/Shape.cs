using System.Collections.Generic;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Lomenou čárou reprezentuje trasu spoje - množina bodů z shapes.txt se stejným shapeId.
    /// Obálka nad body jedné trasy z <see cref="GtfsShapePoint"/>.
    /// </summary>
    public class Shape
    {
        /// <summary>
        /// Jednoznačný identifikátor trasy. Skládá se z linky a pořadového čísla varianty.
        /// Teoreticky se může stát, že stejnou trasu má více spojů různých linek, pak
        /// tedy to číslo linky nemusí odpovídat.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Seznam bodů trasy
        /// </summary>
        public List<ShapePoint> Points { get; set; }

        /// <summary>
        /// Spoje, které jedou po této trase
        /// </summary>
        public List<Trip> TripsOfThisShape { get; set; }

        public Shape()
        {
            Points = new List<ShapePoint>();
            TripsOfThisShape = new List<Trip>();
        }

        /// <summary>
        /// Vytvoří GTFS záznamy jednotlivých bodů
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GtfsShapePoint> ToGtfsShape()
        {
            int i = 1;
            foreach (var shapePoint in Points)
            {
                shapePoint.SequenceIndex = i++;
                yield return shapePoint.ToGtfsShapePoint(Id);
            }
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
