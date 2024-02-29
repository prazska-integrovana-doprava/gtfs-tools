namespace CommonLibrary
{
    /// <summary>
    /// Typ výkonu dle číselníku ASW JŘ.
    /// 
    /// Poznámka:
    /// Do Common Library to samozřejmě absolutně nepatří, jenže protože to sdílí
    /// ASW model s GTFS modelem (kam jsem to bez skrupulí zkopíroval), tak jiné vhodné místo není, chci-li se vyhnout
    /// přímé závislosti GTFS modelu na ASW modelu.
    /// </summary>
    public enum TripOperationType
    {
        /// <summary>
        /// Obyčejný linkový spoj
        /// </summary>
        Regular = 1,

        /// <summary>
        /// Výjezd
        /// </summary>
        FromDepo = 7,

        /// <summary>
        /// Zátah
        /// </summary>
        ToDepo = 8,

        /// <summary>
        /// Přejezd na lince
        /// </summary>
        TransferWithinLine = 9,

        /// <summary>
        /// Přejezd na jinou linku
        /// </summary>
        TransferBetweenLines = 10,
    }
}
