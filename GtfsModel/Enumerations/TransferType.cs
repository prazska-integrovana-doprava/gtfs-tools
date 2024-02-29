namespace GtfsModel.Enumerations
{
    /// <summary>
    /// Typ přestupu
    /// </summary>
    public enum TransferType
    {
        /// <summary>
        /// Doporučený bod pro přestup mezi linkami
        /// </summary>
        RecommendedTransferPoint = 0,

        /// <summary>
        /// Garantovaný přestup s vyčkáváním
        /// </summary>
        TimedTransfer = 1,

        /// <summary>
        /// Specifikace minimálního času na přestup
        /// </summary>
        MinTimeTransfer = 2,

        /// <summary>
        /// Mezi zastávkami není možný přestup
        /// </summary>
        NoTransfer = 3
    }
}
