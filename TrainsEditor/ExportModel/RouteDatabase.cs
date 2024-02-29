using GtfsLogging;
using GtfsModel;
using System.Collections.Generic;
using System.Linq;
using TrainsEditor.GtfsExport;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Databáze linek
    /// </summary>
    class RouteDatabase
    {
        /// <summary>
        /// Linky indexované jejich názvem (veřejným označením)
        /// </summary>
        public Dictionary<string, TrainRoute> Lines { get; private set; }

        /// <summary>
        /// Linky, které byly použity (mají IsUsed = true)
        /// </summary>
        public IEnumerable<TrainRoute> UsedLines { get { return Lines.Values.Where(l => l.IsUsed); } }
                
        private static ICommonLogger log = Loggers.AswDataLoaderLoggerInstance;
        
        protected RouteDatabase(Dictionary<string, TrainRoute> lines)
        {
            Lines = lines;
        }

        public static RouteDatabase CreateRouteDb(AswModel.Extended.LineDatabase aswLines)
        {
            // II. linky
            var lines = new Dictionary<string, TrainRoute>();
            foreach (var aswLine in aswLines)
            {
                var aswLineFirstVersion = aswLine.FirstVersion();
                var line = new TrainRoute()
                {
                    AswId = aswLineFirstVersion.LineNumber,
                    GtfsId = $"L{aswLineFirstVersion.LineNumber}",
                    LongName = aswLineFirstVersion.RouteDescription,
                    ShortName = aswLineFirstVersion.LineName,
                };

                foreach (var agency in aswLineFirstVersion.RouteAgencies)
                {
                    line.SubAgencies.Add(new RouteSubAgency()
                    {
                        RouteId = line.GtfsId,
                        SubAgencyId = agency.Agency.Id,
                        SubAgencyName = agency.Agency.Name,
                    });
                }

                if (!lines.ContainsKey(line.ShortName))
                {
                    lines.Add(line.ShortName, line);
                }
                else
                {
                    log.Log(LogMessageType.WARNING_TRAIN_LINE_DUPLICATE, $"Linka {line.ShortName} je v číselníku vícekrát (rozdílné kalendáře?). Beru jen první výskyt, ID {line.AswId}.");
                }
            }

            return new RouteDatabase(lines);
        }
    }
}
