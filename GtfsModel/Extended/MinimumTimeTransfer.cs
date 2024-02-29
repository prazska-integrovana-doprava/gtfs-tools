using GtfsModel.Enumerations;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Záznam o přestupu mezi zastávkami (<see cref="GtfsTransfer.TransferType"/> = <see cref="TransferType.MinTimeTransfer"/>) z transfers.txt
    /// (rozšíření <see cref="GtfsTransfer"/>)
    /// </summary>
    public class MinimumTimeTransfer : BaseTransfer
    {
        /// <summary>
        /// Minimální čas na přestup v sekundách
        /// </summary>
        public int MinTransferTime { get; set; }

        public override GtfsTransfer ToGtfsTransfer()
        {
            return new GtfsTransfer()
            {
                FromStopId = FromStop.GtfsId,
                ToStopId = ToStop.GtfsId,
                TransferType = TransferType.MinTimeTransfer,
                MinTransferTime = MinTransferTime,
            };
        }

        public override string ToString()
        {
            return $"{FromStop} -> {ToStop} = {MinTransferTime / 60:0.0} min.";
        }
    }
}
