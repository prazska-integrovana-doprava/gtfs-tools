using System.Collections.Generic;

namespace AswModel.Extended
{
    /// <summary>
    /// Identifikátor trasy mezi dvěma zastavkámi (zastávka odkud + zastávka kam + varianta; viz chronometráže ASW JŘ).
    /// Nepopisuje samotnou trasu, ta se podle tohoto pouze vyhledává (proto musí být instance této třídy porovnatelné)
    /// </summary>
    public class ShapeFragmentDescriptor
    {
        /// <summary>
        /// Porovnává zastávky čistě podle jejich čísla uzlu + čísla sloupku
        /// </summary>
        private class StopEqualityComparer : IEqualityComparer<Stop>
        {
            public bool Equals(Stop x, Stop y)
            {
                if (x == y)
                    return true;
                else if (x == null || y == null)
                    return false;

                return x.NodeId == y.NodeId && x.StopId == y.StopId;
            }

            public int GetHashCode(Stop obj)
            {
                if (obj == null)
                    return 0;

                return obj.NodeId ^ obj.StopId;
            }
        }

        /// <summary>
        /// Číslo závodu
        /// </summary>
        public int CompanyId { get; private set; }

        /// <summary>
        /// Výchozí zastávka
        /// </summary>
        public Stop Source { get; private set; }

        /// <summary>
        /// Cílová zastávka
        /// </summary>
        public Stop Destination { get; private set; }

        /// <summary>
        /// Varianta trasy
        /// </summary>
        public int Variant { get; private set; }

        private static StopEqualityComparer stopEqualityComparer = new StopEqualityComparer();

        public ShapeFragmentDescriptor(int companyId, Stop source, Stop destination, int variant)
        {
            CompanyId = companyId;
            Source = source;
            Destination = destination;
            Variant = variant;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ShapeFragmentDescriptor;
            if (other == null)
                return false;

            return stopEqualityComparer.Equals(Source, other.Source) && stopEqualityComparer.Equals(Destination, other.Destination)
                && Variant == other.Variant && CompanyId == other.CompanyId;
        }

        public override int GetHashCode()
        {
            return stopEqualityComparer.GetHashCode(Source) ^ stopEqualityComparer.GetHashCode(Destination)
                ^ Variant.GetHashCode() ^ CompanyId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Source} -> {Destination} var {Variant} zav {CompanyId}";
        }
    }
}
