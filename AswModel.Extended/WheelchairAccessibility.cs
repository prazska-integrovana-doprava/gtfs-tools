namespace AswModel.Extended
{
    /// <summary>
    /// Všechny možné stavy bezbariérové přístupnosti (pro zastávky a spoje v ASW)
    /// </summary>
    public enum WheelchairAccessibility
    {
        /// <summary>
        /// Bezbariérovost neurčena
        /// </summary>
        Undefined,

        /// <summary>
        /// Bezbariérově přístupné
        /// </summary>
        Accessible,

        /// <summary>
        /// Částečně bezbariérově přístupné
        /// </summary>
        PartiallyAccessible,

        /// <summary>
        /// Bariérové
        /// </summary>
        NotAccessible,
    }
}
