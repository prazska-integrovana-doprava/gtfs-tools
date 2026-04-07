namespace JdfToGtfsProcessor.Stops
{
    /// <summary>
    /// Kolekce GTFS zastávek pro jedno dané CIS číslo
    /// </summary>
    internal class StopCollectionForCisNumber
    {
        /// <summary>
        /// Název zastávky
        /// </summary>
        public string StopName { get; set; }

        /// <summary>
        /// GTFS zastávky pro jednotlivá nástupiště
        /// </summary>
        public Dictionary<string, StopPlatform> AllPlatforms { get; private set; }

        /// <summary>
        /// GTFS zastávka obecná pro celý "CIS uzel" - použije se jako fallback, když se nedohledá konkrétní nástupiště
        /// </summary>
        public StopPlatform UniversalPlatform { get; private set; }

        /// <summary>
        /// True, pokud se použila zastávka <see cref="UniversalPlatform"/>. Nenastavuje se automaticky, musí se nastavit z vnějšku.
        /// </summary>
        public bool UniversalPlatformWasUsed { get; set; }

        public StopCollectionForCisNumber(string stopName, StopPlatform universalPlatform) 
        {
            StopName = stopName;
            AllPlatforms = new Dictionary<string, StopPlatform>();
            UniversalPlatform = universalPlatform;
        }
    }
}
