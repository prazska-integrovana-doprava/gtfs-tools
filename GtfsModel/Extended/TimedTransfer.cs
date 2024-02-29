using GtfsModel.Enumerations;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Záznam o garantovaném přestupu mezi dvěma spoji (<see cref="GtfsTransfer.TransferType"/> = <see cref="TransferType.TimedTransfer"/>) z transfers.txt
    /// (rozšíření <see cref="GtfsTransfer"/>)
    /// </summary>
    public class TimedTransfer : BaseTransfer
    {
        /// <summary>
        /// Spoj, ze kterého se přestupuje v zastávce <see cref="FromStop"/>.
        /// </summary>
        public Trip FromTrip { get; set; }

        /// <summary>
        /// Spoj, na který se nastupuje v zastávce <see cref="ToStop"/>.
        /// </summary>
        public Trip ToTrip { get; set; }

        /// <summary>
        /// Maximální čas v sekundách, kolik navazující spoj může počkat na zpožděný.
        /// </summary>
        public int MaxWaitingTimeSeconds { get; set; }

        public override GtfsTransfer ToGtfsTransfer()
        {
            return new GtfsTransfer()
            {
                FromStopId = FromStop.GtfsId,
                ToStopId = ToStop.GtfsId,
                TransferType = TransferType.TimedTransfer,
                FromTripId = FromTrip.GtfsId,
                ToTripId = ToTrip.GtfsId,
                MaxWaitingTime = MaxWaitingTimeSeconds,
            };
        }

        public override bool Equals(object obj)
        {
            var other = obj as TimedTransfer;
            if (other == null)
                return false;

            return Equals(FromStop, other.FromStop) && Equals(ToStop, other.ToStop) && Equals(FromTrip, other.FromTrip) && Equals(ToTrip, other.ToTrip) && Equals(MaxWaitingTimeSeconds, other.MaxWaitingTimeSeconds);
        }

        public override int GetHashCode()
        {
            return FromStop.GetHashCode() ^ ToStop.GetHashCode() * 231 + FromTrip.GetHashCode() ^ ToTrip.GetHashCode() * 171 ^ MaxWaitingTimeSeconds * 6927;
        }

        public override string ToString()
        {
            return $"{FromStop.GtfsId} -> {ToStop.GtfsId} ({FromTrip.GtfsId} -> {ToTrip.GtfsId})";
        }
    }
}
