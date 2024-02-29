using CsvSerializer.Attributes;
using GtfsModel.Enumerations;

namespace GtfsModel
{
    /// <summary>
    /// Data o přestupu (zatím se používá pouze k evidenci garantovaných přestupů)
    /// </summary>
    public class GtfsTransfer
    {
        /// <summary>
        /// Výchozí zastávka (výstup) - ID ze stops.txt
        /// </summary>
        [CsvField("from_stop_id", 1)]
        public string FromStopId { get; set; }

        /// <summary>
        /// Nástupní zastávka - ID ze stops.txt
        /// </summary>
        [CsvField("to_stop_id", 2)]
        public string ToStopId { get; set; }

        /// <summary>
        /// Typ přestupu dle výčtu GTFS
        /// </summary>
        [CsvField("transfer_type", 3)]
        public TransferType TransferType { get; set; }

        /// <summary>
        /// Minimální čas na přestup (v sekundách)
        /// </summary>
        [CsvField("min_transfer_time", 4)]
        public int? MinTransferTime { get; set; }

        /// <summary>
        /// Spoj (ID z trips.txt), ze kterého se přestupuje v zastávce <see cref="FromStopId"/>.
        /// Validní pouze pro <see cref="TransferType"/> = <see cref="TransferType.MinTimeTransfer"/>
        /// </summary>
        [CsvField("from_trip_id", 7)]
        public string FromTripId { get; set; }

        /// <summary>
        /// Spoj (ID z trips.txt), na který se nastupuje v zastávce <see cref="ToStopId"/>.
        /// Validní pouze pro <see cref="TransferType"/> = <see cref="TransferType.MinTimeTransfer"/>
        /// </summary>
        [CsvField("to_trip_id", 8)]
        public string ToTripId { get; set; }

        /// <summary>
        /// Maximální čas v sekundách, kolik navazující spoj může čekat na zpožděný spoj
        /// </summary>
        [CsvField("max_waiting_time", 51)]
        public int MaxWaitingTime { get; set; }

        public override string ToString()
        {
            return $"{FromStopId} -> {ToStopId}";
        }
    }
}
