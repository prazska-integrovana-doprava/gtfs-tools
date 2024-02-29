using CzpttModel;
using System.Collections.Generic;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Manuální korekce pro načítání dat
    /// </summary>
    static class CorrectionConfig
    {
        // stanice, které se vyskytují v datech SŽDC, ale u nás z nějakého důvodu být nemusí
        // bude to fungovat i ve chvíli, kdy je u nás ta zastávka v JŘ zařazena
        public static readonly int[] IgnoredStations = new int[]
        {
            58083, // Nymburk předj.n. - prý tam nabírá nějaký dělníky, či co, ale ve veřejném JŘ to není
        };

        // typy vlaků, které nás zajímají (zastavení s jiným typem ignorujeme)
        public static readonly TrafficTypeEnum[] AcceptedTrafficTypes = new TrafficTypeEnum[]
        {
            TrafficTypeEnum.Os,
            TrafficTypeEnum.Ex,
            TrafficTypeEnum.R,
            TrafficTypeEnum.Sp
        };

        // někdy SŽDC používá jiné označení stanic, než my, takže jim to přepíšeme na ty naše
        // TODO ideálně udělat nějakou vlastní strukturu a nepoužívat dictionary
        public static readonly Dictionary<int, int> RewriteStations = new Dictionary<int, int>()
        {
            { 58117, 58486 }, // Praha-Smíchov sev.n.
            { 58000, 54044 } // Čáslav (místní nádraží)
        };
    }
}
