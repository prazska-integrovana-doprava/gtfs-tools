using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Metody pro práci s <see cref="IntegratedSystemsEnum"/>
    /// </summary>
    static class IntegratedSystemsExtensions
    {
        /// <summary>
        /// Seznam všech známých IDS. Neobsahuje položku <see cref="IntegratedSystemsEnum.None"/>.
        /// </summary>
        public static readonly IntegratedSystemsEnum[] IntegratedSystemsList 
            = Enum.GetValues(typeof(IntegratedSystemsEnum)).OfType<IntegratedSystemsEnum>().Where(intsys => intsys != IntegratedSystemsEnum.None).ToArray();

        /// <summary>
        /// Vrací true, pokud je zahrnut <paramref name="integratedSystem"/>.
        /// </summary>
        public static bool Contains(this IntegratedSystemsEnum integratedSystems, IntegratedSystemsEnum integratedSystem)
        {
            return (integratedSystems & integratedSystem) == integratedSystem;
        }

        /// <summary>
        /// Vrátí IDS, které jsou v seznamu <paramref name="stopsIntegratedSystems"/> zastoupeny alespoň dvakrát.
        /// </summary>
        public static IntegratedSystemsEnum GetIntegratedSystemsWithAtLeastTwoStops(IEnumerable<IntegratedSystemsEnum> stopsIntegratedSystems)
        {
            var onceIncluded = IntegratedSystemsEnum.None;
            var twiceIncluded = IntegratedSystemsEnum.None;

            foreach (var stopIntegratedSystems in stopsIntegratedSystems)
            {
                twiceIncluded |= (stopIntegratedSystems & onceIncluded);
                onceIncluded |= stopIntegratedSystems;
            }

            return twiceIncluded;
        }
    }
}
