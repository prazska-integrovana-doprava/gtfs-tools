namespace GtfsProcessor.Logging
{
    /// <summary>
    /// Výsledek porovnání dvou spojů
    /// </summary>
    internal enum TripEqualityResult
    {
        /// <summary>
        /// Spoje jsou úplně stejné, mají i stejný kalendář a číslo oběhu, což je pravděpodobně chyba
        /// </summary>
        AreTotallySame,

        /// <summary>
        /// Spoje jsou úplně stejné, mají i stejný kalendář, ale alespoň číslo oběhu je jiné
        /// </summary>
        AreSameWithDifferentCircleNumber,

        /// <summary>
        /// Spoje mají stejné zastávky a odjezdy, liší se kalendářem, ovšem nejedou v disjunktní dny, což je podivné
        /// </summary>
        AreEqualButNotDisjunct,

        /// <summary>
        /// Spoje mají stejné zastávky a odjezdy a disjunktní kalendář a mohou být sloučeny
        /// </summary>
        AreEqualAndDisjunct,

        /// <summary>
        /// Spoje jsou odlišné
        /// </summary>
        AreDifferent
    }
}
