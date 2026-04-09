using GtfsLogging;
using GtfsModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TrainsEditor.GtfsExport;
using TrainsEditor.SystemDescriptionModel;

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
                
        private static ICommonLogger log = Loggers.SystemDataLoaderLoggerInstance;
        
        protected RouteDatabase(Dictionary<string, TrainRoute> lines)
        {
            Lines = lines;
        }

        /// <summary>
        /// Načte data z ASW
        /// </summary>
        /// <param name="aswLines">Linky z ASW JŘ</param>
        public static RouteDatabase CreateRouteDb(AswModel.Extended.LineDatabase aswLines)
        {
            var lines = new Dictionary<string, TrainRoute>();
            foreach (var aswLine in aswLines)
            {
                var aswLineFirstVersion = aswLine.FirstVersion();
                var line = new TrainRoute()
                {
                    AswId = aswLineFirstVersion.LineNumber,
                    RouteId = aswLineFirstVersion.LineNumber.ToString(),
                    GtfsId = $"L{aswLineFirstVersion.LineNumber}",
                    LongName = aswLineFirstVersion.RouteDescription,
                    ShortName = aswLineFirstVersion.LineName,
                    Color = Color.FromArgb(37, 30, 98),
                    TextColor = Color.White,
                };

                foreach (var agency in aswLineFirstVersion.RouteAgencies)
                {
                    line.SubAgencies.Add(new RouteSubAgency()
                    {
                        RouteId = line.GtfsId,
                        SubAgencyId = agency.Agency.Id.ToString(),
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

        /// <summary>
        /// Načte data z konfiguračního souboru
        /// </summary>
        /// <param name="routeData">Data linek</param>
        /// <param name="agencyData">Data dopravců</param>
        /// <returns></returns>
        public static RouteDatabase CreateRouteDb(IEnumerable<Route> routeData, IEnumerable<Agency> agencyData)
        {
            var lines = new Dictionary<string, TrainRoute>();
            foreach (var route in routeData)
            {
                var line = new TrainRoute()
                {
                    RouteId = route.ShortName,
                    GtfsId = route.ShortName,
                    LongName = route.LongName,
                    ShortName = route.ShortName,
                    Color = string.IsNullOrEmpty(route.ColorCodeHtml) ? Color.Black : ColorTranslator.FromHtml(route.ColorCodeHtml),
                    TextColor = Color.White,
                };

                foreach (var agency in agencyData)
                {
                    line.SubAgencies.Add(new RouteSubAgency()
                    {
                        RouteId = line.GtfsId,
                        SubAgencyId = agency.Id.ToString(),
                        SubAgencyName = agency.Name,
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
