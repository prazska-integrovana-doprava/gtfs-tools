using CommonLibrary;
using System;

namespace AswModel.Extended
{
    public struct GraphIdAndCompany : IEquatable<GraphIdAndCompany>
    {
        /// <summary>
        /// Unikátní číslo grafikonu (pouze v rámci závodu)
        /// </summary>
        public int GraphId { get; set; }

        /// <summary>
        /// Číslo závodu
        /// </summary>
        public int CompanyId { get; set; }

        public bool Equals(GraphIdAndCompany other)
        {
            return GraphId == other.GraphId && CompanyId == other.CompanyId;
        }

        public override string ToString()
        {
            return $"{GraphId} zav {CompanyId}";
        }
    }

    /// <summary>
    /// Grafikon z ASW JŘ (resp. jeden záznam o něm)
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Unikátní identifikace grafikonu (číslo závodu + id v rámci závodu)
        /// </summary>
        public GraphIdAndCompany Id { get; set; }
        
        /// <summary>
        /// Rozsah od kdy do kdy grafikon platí. Pozor, neurčuje provozní dny, pouze datum počátku a datum konce platnosti grafikonu.
        /// </summary>
        public ServiceDaysBitmap ValidityRange { get; set; }

        /// <summary>
        /// Ve které dny v týdnu spoj jede (indexováno <see cref="DayOfWeek"/>)
        /// </summary>
        public bool[] DaysInWeek { get; private set; }

        public Graph()
        {
            DaysInWeek = new bool[7];
        }
        
        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
