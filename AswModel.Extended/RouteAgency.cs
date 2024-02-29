namespace AswModel.Extended
{
    /// <summary>
    /// Reprezentuje dvojici (linka, dopravce). Jedna linka může mít více dopravců.
    /// </summary>
    public class RouteAgency
    {
        /// <summary>
        /// CIS číslo linky (šestimístné)
        /// </summary>
        public int CisLineNumber { get; set; }

        /// <summary>
        /// Info o dopravci
        /// </summary>
        public AswAgency Agency { get; set; }

        public override string ToString()
        {
            return $"{CisLineNumber} {Agency}";
        }
    }
}
