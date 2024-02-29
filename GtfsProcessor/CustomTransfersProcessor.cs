using GtfsLogging;
using CsvSerializer;
using CsvSerializer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using GtfsModel.Extended;
using GtfsProcessor.Logging;

namespace GtfsProcessor
{
    /// <summary>
    /// Zpracuje ručně zadané přestupy nad GTFS daty
    /// </summary>
    class CustomTransfersProcessor
    {
        private class CustomTransfer
        {
            /// <summary>
            /// Číslo uzlu
            /// </summary>
            [CsvField("src_node_id", 1)]
            public int SrcNode { get; set; }
            
            /// <summary>
            /// Číslo zastávky nebo název stanice (funguje jen první varianta)
            /// </summary>
            [CsvField("src_stop", 2)]
            public string SrcStop { get; set; }

            [CsvField("dest_node_id", 3)]
            public int DestNode { get; set; }

            [CsvField("dest_stop", 4)]
            public string DestStop { get; set; }

            [CsvField("transfer_time", 5)]
            public int TransferTimeSecs { get; set; }

            [CsvField("bidirectional", 6)]
            public bool IsBidirectional { get; set; }
        }

        private List<Stop> stops;
        private ICommonLogger log = Loggers.CommonLoggerInstance;
        private string transfersFileName;

        public CustomTransfersProcessor(string transfersFileName, IEnumerable<BaseStop> stops)
        {
            this.transfersFileName = transfersFileName;
            this.stops = (from stop in stops where stop is Stop select (Stop) stop).ToList();
        }

        /// <summary>
        /// Načte custom přestupy a vrátí je předpřipravené, loguje chyby, když některou zastávku nenalezne
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MinimumTimeTransfer> ParseTimeTransfers()
        {
            var customTransfers = CsvFileSerializer.DeserializeFile<CustomTransfer>(transfersFileName);

            foreach (var customTransfer in customTransfers)
            {
                var srcStops = FindStop(customTransfer.SrcNode, customTransfer.SrcStop);
                var destStops = FindStop(customTransfer.DestNode, customTransfer.DestStop);
                if (!srcStops.Any() || !destStops.Any())
                    continue; // už je zalogováno

                foreach (var srcStop in srcStops)
                {
                    foreach (var destStop in destStops)
                    {
                        var resultTransfer = new MinimumTimeTransfer()
                        {
                            FromStop = srcStop,
                            ToStop = destStop,
                            MinTransferTime = customTransfer.TransferTimeSecs,
                        };

                        yield return resultTransfer;

                        if (customTransfer.IsBidirectional)
                        {
                            yield return new MinimumTimeTransfer()
                            {
                                FromStop = resultTransfer.ToStop,
                                ToStop = resultTransfer.FromStop,
                                MinTransferTime = resultTransfer.MinTransferTime,
                            };
                        }
                    }
                }
            }
        }

        // vrátí GTFS zastávky odpovídající zadání
        private IEnumerable<Stop> FindStop(int nodeId, string stopNameOrId)
        {
            int stopId;
            if (int.TryParse(stopNameOrId, out stopId))
            {
                // varianta s číslem sloupku
                var result = stops.Where(s => s.AswNodeId == nodeId && s.AswStopId == stopId);
                if (!result.Any())
                {
                    log.Log(LogMessageType.INFO_STOP_NOT_FOUND, $"Zastávka {nodeId}/{stopId} nebyla nalezena nebo není využita, přestupní záznam bude ignorován");
                    return Enumerable.Empty<Stop>();
                }

                return result;
            }
            else
            {
                // varianta název stanice - nepoužívá se
                throw new NotImplementedException();
                /*
                var resultStation = stopDatabase.FindStation(nodeId, stopNameOrId);
                if (resultStation == null)
                {
                    log.Log(LogMessageType.WARNING_STOP_NOT_FOUND, $"Stanice {stopNameOrId} v uzlu {nodeId} nebyla nalezena, přestupní záznam bude ignorován");
                    return Enumerable.Empty<IGtfsStop>();
                }

                if (exitId == -1)
                {
                    // string neobsahoval ID výstupu
                    return new[] { resultStation };
                }
                else
                {
                    var resultExit = resultStation.EntrancesToStation.SingleOrDefault(entrance => entrance.EntranceIndex == exitId);
                    if (resultExit == null)
                    {
                        log.Log(LogMessageType.WARNING_STOP_EXIT_NOT_FOUND, $"Vstup {exitId} do stanice {nodeId} nebyl nalezen (nebo má duplicitní záznamy), přestupní záznam bude ignorován");
                        return Enumerable.Empty<IGtfsStop>();
                    }

                    return new[] { resultExit };
                }
                */
            }
        }
    }
}
