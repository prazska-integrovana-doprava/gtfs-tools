using System;

namespace TrainsEditor.GtfsExport
{
    /// <summary>
    /// Definuje vlak, který má být zástupcem pro danou linku.
    /// </summary>
    [Serializable]
    public class TripDirectionSpec
    {
        /// <summary>
        /// Označení linky (např. "S1")
        /// </summary>
        public string LineName { get; set; }

        /// <summary>
        /// Číslo vlaku, který reprezentuje linku
        /// </summary>
        public int TrainNumber { get; set; }
    }
}
