namespace AswModel.Extended
{
    /// <summary>
    /// Typ linky ASW JŘ
    /// </summary>
    public enum AswLineType
    {
        /// <summary>
        /// Městská linka (typ "A")
        /// </summary>
        PraguePublicTransport,

        /// <summary>
        /// Příměstská linka ("třístovka", typ "B" nebo "Z")
        /// </summary>
        SuburbanTransport, 

        /// <summary>
        /// Mimopražská linka ("čtyřstovka", typ "V")
        /// </summary>
        RegionalTransport,

        /// <summary>
        /// Linka (spíše spoje) se zvláštním tarifem (typicky bezplatná). Nemá přímo odraz v typu linky, přiřazuje se až jednotlivým spojům.
        /// </summary>
        SpecialTransport,

        /// <summary>
        /// Vlaková linka (typ "R") nebo náhradní doprava za vlak
        /// </summary>
        RailTransport,

        /// <summary>
        /// Linka neurčeného typu (typ "-"), chybová linka apod. Spoje těchto linek se zahazují.
        /// </summary>
        UndefinedTransport,
    }
}
