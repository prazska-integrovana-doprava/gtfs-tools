using System.Collections.Generic;

namespace CzpttModel
{
    /// <summary>
    /// Definuje hodnoty pro <see cref="CZPTTLocation.CommercialTrafficType"/>
    /// </summary>
    public class CommercialTrafficType
    {
        /// <summary>
        /// Plný název
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Zkratka (používaná před číslem vlaku)
        /// </summary>
        public string Abbr { get; private set; }

        public CommercialTrafficType(string name, string abbr)
        {
            Name = name;
            Abbr = abbr;
        }

        public override string ToString()
        {
            return Abbr;
        }

        /// <summary>
        /// Druhy vlaků pro <see cref="CZPTTLocation.CommercialTrafficType"/>. Indexováno hodnotou z XML.
        /// </summary>
        public static readonly IDictionary<int, CommercialTrafficType> CommercialTrafficTypes = new Dictionary<int, CommercialTrafficType>()
        {
            {50, new CommercialTrafficType("EuroCity", "EC") },
            {63, new CommercialTrafficType("InterCity", "IC") },
            {69, new CommercialTrafficType("Express", "Ex") },
            {70, new CommercialTrafficType("Euro Night", "EN") },
            {84, new CommercialTrafficType("Regional", "Os") },
            {94, new CommercialTrafficType("SuperCity", "SC") },
            {122, new CommercialTrafficType("Rapid", "Sp") },
            {157, new CommercialTrafficType("Fast train", "R") },
            {209, new CommercialTrafficType("RailJet", "rj") },
            {9000, new CommercialTrafficType("Rex", "rj") },
            {9001, new CommercialTrafficType("Trilex-expres", "TLX") },
            {9002, new CommercialTrafficType("Trilex", "TL") },
            {9003, new CommercialTrafficType("LEO Expres", "LE") },
            {9004, new CommercialTrafficType("Regiojet", "RJ") },
            {9005, new CommercialTrafficType("Arriva Expres", "AEx") },
            {9007, new CommercialTrafficType("LeoExpress Tenders", "LET") },
        };
    }
}
