using GtfsModel.Enumerations;
using GtfsModel.Extended;
using System.Drawing;
using System.Linq;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Vlaková linka
    /// </summary>
    class TrainRoute : Route
    {
        /// <summary>
        /// Nastaveno na true, pokud existuje nějaký spoj této linky
        /// </summary>
        public bool IsUsed { get { return Trips.Any(); } }

        public TrainRoute()
        {
            Color = Color.FromArgb(37, 30, 98);
            TextColor = Color.White;
            Type = TrafficType.Rail;
            IsRegional = true;
        }

        public override string ToString()
        {
            return $"{ShortName} ({AswId})";
        }
    }
}
