using GtfsModel.Extended;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Zastávka/Stanice vlaku načtená z číselníku ASW JŘ
    /// </summary>
    class TrainStop : Stop
    {
        /// <summary>
        /// True, pokud byla zastávka někde použita
        /// </summary>
        public bool IsUsed { get; set; }
        
        /// <summary>
        /// True, pokud se zastávka nachází i v číselníku ASW JŘ (tj. má pásmo a ASW ID)
        /// </summary>
        public bool IsFromAsw { get { return AswNodeId != 0; } }

        /// <summary>
        /// Všechna pásma všech tarifních systémů
        /// </summary>
        public AswModel.Extended.ZoneInfo[] ZoneIds { get; set; }

        /// <summary>
        /// ID zastávky v datech SŽ
        /// </summary>
        public int PrimaryLocationCode { get; set; }

        public override string ToString()
        {
            if (IsFromAsw)
                return $"{Name} [{ZoneId}]";
            else
                return $"{Name}";
        }
    }
}
