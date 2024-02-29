namespace CommonLibrary
{
    /// <summary>
    /// Kategorie umístění zastávky
    /// </summary>
    public enum ZoneRegionType
    {
        /// <summary>
        /// Nelze / není třeba definovat (např. pro body, které nejsou zastávkami)
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Zastávka se nachází na území Prahy
        /// </summary>
        StopInPrague = 1,

        /// <summary>
        /// Zastávka se nachází na území Středočeského kraje
        /// </summary>
        StopInCentralBohemia = 2,

        /// <summary>
        /// Zastávka se nachází na území jiného kraje, než Prahy a SČK, ale jízdenky PID lze využít bez omezení
        /// </summary>
        StopOutsideInPid = 3,

        /// <summary>
        /// Zastávka se nachází na území jiného kraje a jízdenku PID lze použít pouze pokud trasa vede přes alespoň jednu zastávku v kategoriích výše
        /// </summary>
        StopOutsidePidLimited = 4,

        /// <summary>
        /// Zastávka se nachází na území jiného kraje a jízdenku PID nelze použít vůbec (odpovídá zastávkám, které mají hodnotu zone_id = '-')
        /// </summary>
        StopOutsidePidNotAvailable = 5,
    }
}
