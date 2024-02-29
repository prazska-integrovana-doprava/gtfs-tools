namespace StopProcessor
{
    /// <summary>
    /// Identifikace skupiny zastávek stejného názvu
    /// </summary>
    public class StopCollectionId
    {
        /// <summary>
        /// Název
        /// </summary>
        public string Name2 { get; private set; }

        private string name2ToLower;

        /// <summary>
        /// Kód okresu (SPZ)
        /// </summary>
        public string DistrictCode { get; private set; }

        /// <summary>
        /// Kategorie IDOS (rozlišuje BUS a VLAK)
        /// </summary>
        public bool IsTrain { get; private set; }

        public StopCollectionId(string name2, string districtCode, bool isTrain)
        {
            Name2 = name2;
            name2ToLower = Name2.ToLower();
            DistrictCode = districtCode ?? "";
            IsTrain = isTrain;
        }

        /// <summary>
        /// Vygeneruje identifikaci skupiny zastávek podle zastávky
        /// </summary>
        /// <param name="stop">Zastávka</param>
        /// <returns>Identifikace skupiny, do které zastávka patří</returns>
        public static StopCollectionId FromStop(Stop stop)
        {
            return new StopCollectionId(stop.Name2, stop.DistrictCode, stop.IsTrain);
        }

        public override int GetHashCode()
        {
            return name2ToLower.GetHashCode() ^ DistrictCode.GetHashCode() ^ IsTrain.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as StopCollectionId;
            if (other == null)
                return false;

            return name2ToLower == other.name2ToLower && DistrictCode == other.DistrictCode && IsTrain == other.IsTrain;
        }

        public override string ToString()
        {
            if (!IsTrain)
                return $"{Name2} ({DistrictCode})";
            else
                return $"{Name2} ({DistrictCode}, vlak)";
        }
    }
}
