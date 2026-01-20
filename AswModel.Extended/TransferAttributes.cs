namespace AswModel.Extended
{
    /// <summary>
    /// Kolekce atributů přestupních ikonek
    /// </summary>
    public class TransferAttributes
    {

        /// <summary>
        /// Příznak přestup na metro A
        /// </summary>
        public bool IsTransferToMetroA { get; set; }

        /// <summary>
        /// Příznak přestup na metro B
        /// </summary>
        public bool IsTransferToMetroB { get; set; }

        /// <summary>
        /// Příznak přestup na metro C
        /// </summary>
        public bool IsTransferToMetroC { get; set; }

        /// <summary>
        /// Příznak přestup na metro D
        /// </summary>
        public bool IsTransferToMetroD { get; set; }

        /// <summary>
        /// Příznak přestup na linky S a další vlakové spoje
        /// </summary>
        public bool IsTransferToSbahn { get; set; }

        /// <summary>
        /// Příznak přestup na tramvaj
        /// </summary>
        public bool IsTransferToTram { get; set; }

        /// <summary>
        /// Příznak přestup na autobus
        /// </summary>
        public bool IsTransferToBus { get; set; }

        /// <summary>
        /// Příznak přestup na trolejbus
        /// </summary>
        public bool IsTransferToTrolleybus { get; set; }

        /// <summary>
        /// Příznak přestup na vlak
        /// </summary>
        public bool IsTransferToTrain { get; set; }

        /// <summary>
        /// Příznak přestup na přívoz
        /// </summary>
        public bool IsTransferToFerry { get; set; }

        /// <summary>
        /// Příznak přestup na letiště
        /// </summary>
        public bool IsTransferToAirport { get; set; }

        /// <summary>
        /// Příznak přestup na lanovou dráhu
        /// </summary>
        public bool IsTransferToFunicular { get; set; }
    }
}
