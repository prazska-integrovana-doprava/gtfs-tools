using GtfsModel.Enumerations;
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
        public bool IsIntegrated { get; set; }

        /// <summary>
        /// Všechny přestupní ikonky pro zastávku. Protože jde o atribut k zastávce, je potřeba je ještě profiltrovat podle času (třeba přestup na metro ve 2:00 by se neměl aplikovat).
        /// </summary>
        public TransferIcons[] AllTransferIcons { get; set; }

        /// <summary>
        /// ID zastávky v datech SŽ
        /// </summary>
        public int PrimaryLocationCode { get; set; }

        public override string ToString()
        {
            if (IsIntegrated)
                return $"{Name} [{ZoneId}]";
            else
                return $"{Name}";
        }
    }
}
