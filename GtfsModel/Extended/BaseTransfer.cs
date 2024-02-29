using GtfsModel.Enumerations;
using System;
using System.Collections.Generic;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Vše, co se umí tvářit jako přestup - záznam z transfers.txt (rozšíření nad <see cref="GtfsTransfer"/>).
    /// </summary>
    public abstract class BaseTransfer
    {
        /// <summary>
        /// Výchozí zastávka (výstup). Může být i stanice.
        /// </summary>
        public BaseStop FromStop { get; set; }

        /// <summary>
        /// Nástupní zastávka. Může být i stanice
        /// </summary>
        public BaseStop ToStop { get; set; }

        public abstract GtfsTransfer ToGtfsTransfer();

        /// <summary>
        /// Vytvoří data o přestupu na základě GTFS záznamu.
        /// </summary>
        /// <param name="gtfsTransfer">GTFS záznam</param>
        public static BaseTransfer Construct(GtfsTransfer gtfsTransfer, IDictionary<string, BaseStop> stops, IDictionary<string, Trip> trips)
        {
            switch (gtfsTransfer.TransferType)
            {
                case TransferType.MinTimeTransfer:
                    return new MinimumTimeTransfer()
                    {
                        FromStop = stops[gtfsTransfer.FromStopId],
                        ToStop = stops[gtfsTransfer.ToStopId],
                        MinTransferTime = gtfsTransfer.MinTransferTime.GetValueOrDefault(),
                    };

                case TransferType.TimedTransfer:
                    return new TimedTransfer()
                    {
                        FromStop = stops[gtfsTransfer.FromStopId],
                        ToStop = stops[gtfsTransfer.ToStopId],
                        FromTrip = trips[gtfsTransfer.FromTripId],
                        ToTrip = trips[gtfsTransfer.ToTripId],
                        MaxWaitingTimeSeconds = gtfsTransfer.MaxWaitingTime,
                    };

                default:
                    throw new InvalidOperationException($"Unsupported transfer type {gtfsTransfer.TransferType}");
            }
        }
    }
}
