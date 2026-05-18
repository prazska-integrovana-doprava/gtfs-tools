using System.Collections.Generic;
using System.Linq;

namespace GtfsModel.Enumerations
{
    /// <summary>
    /// Přestupní ikonky k zastávkám (použité u zastavení a headsignů)
    /// </summary>
    public enum TransferIcons
    {
        MetroA,
        MetroB,
        MetroC,
        MetroD,
        Train,
        Sbahn,
        Funicular,
        Ferry,
        Airport,
        Tramway,
        Trolleybus,
        Bus
    }

    /// <summary>
    /// Třída mapující přestupní ikonky na dvoupísmenné kódy, které používáme v GTFS
    /// </summary>
    public static class TransferIconCodes
    {
        public static readonly Dictionary<TransferIcons, string> Map = new Dictionary<TransferIcons, string>()
        {
            {TransferIcons.MetroA, "Ma" },
            {TransferIcons.MetroB, "Mb" },
            {TransferIcons.MetroC, "Mc" },
            {TransferIcons.MetroD, "Md" },
            {TransferIcons.Train, "Ra" },
            {TransferIcons.Sbahn, "Sb" },
            {TransferIcons.Funicular, "Fu" },
            {TransferIcons.Ferry, "Fe" },
            {TransferIcons.Airport, "Ap" },
            {TransferIcons.Tramway, "Tw" },
            {TransferIcons.Trolleybus, "Tb" },
            {TransferIcons.Bus, "Bu" }
        };

        public static readonly Dictionary<string, TransferIcons> ReverseMap = Map.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public static string Transform(IEnumerable<TransferIcons> icons)
        {
            if (icons == null)
            {
                return null;
            }

            return string.Join("", icons.Select(i => Map[i]));
        }

        public static IEnumerable<TransferIcons> ReverseTransform(string icons)
        {
            if (icons == null)
            {
                yield break;
            }

            for (int i = 0; i < icons.Length - 1; i += 2)
            {
                var code = new string(new char[] { icons[i], icons[i + 1] });
                if (ReverseMap.TryGetValue(code, out var icon))
                {
                    yield return icon;
                }
            }
        }
    }
}
