namespace CzpttModel.Kango
{
    /// <summary>
    /// Rozhraní pro parametry <see cref="NetworkSpecificParameterBase"/>, které se vážou na subsekvenci stanic.
    /// </summary>
    public interface IStationRangeParam
    {
        /// <summary>
        /// BodOd = ISO státu + Primary location code, (např. CZ12345)
        /// Identifikuje bod v trase vlaku, ODKUD je poznámka platná.
        /// Je-li poznámka platná pouze pro jeden bod, bude Bod OD a Bod DO shodný.
        /// </summary>
        string FromStationCode { get; set; }

        /// <summary>
        /// PoradiBodOd = Pořadové číslo opakovaného výskytu dopravního bodu BodOd v cestě žádosti/CZPTT: nic – první výskyt, 1 – druhý výskyt, 2 – třetí výskyt.
        /// </summary>
        int FromStationCodeOccurence { get; set; }

        /// <summary>
        /// BodDo = ISO státu + Primary location code - např. CZ12345
        /// Identifikuje bod v trase vlaku, KAM je poznámka platná.
        /// </summary>
        string ToStationCode { get; set; }

        /// <summary>
        /// PoradiBodDo = Pořadové číslo opakovaného výskytu dopravního bodu BodDo v cestě žádosti/CZPTT: nic – první výskyt, 1 – druhý výskyt, 2 – třetí výskyt.
        /// </summary>
        int ToStationCodeOccurence { get; set; }
    }
}
