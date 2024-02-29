using AswModel.Extended.Logging;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává záznamy o linkách <see cref="Linka"/> z ASW JŘ a přidává je do databáze <see cref="TheAswDatabase.Lines"/>. Resolvuje případné duplicity se záznamy, které už v databázi jsou.
    /// 
    /// Potřebuje už mít načtené dopravce
    /// </summary>
    class LineProcessor : IProcessor<Linka>
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private TheAswDatabase db;

        public LineProcessor(TheAswDatabase db)
        {
            this.db = db;
        }

        public void Process(Linka xmlLine)
        {
            var serviceAsBits = ServiceDaysBitmap.FromBitmapString(xmlLine.KJ);
            if (serviceAsBits.IsEmpty)
                return;
            
            var routeRecord = new Route()
            {
                LineNumber = xmlLine.CLinky,
                LineName = !string.IsNullOrEmpty(xmlLine.AliasLinky) ? xmlLine.AliasLinky : xmlLine.CLinky.ToString(),
                RouteDescription = xmlLine.Nazev,
                IdosRouteCategory = (IdosRouteCategory)xmlLine.KategorieProIdos,
                IsNight = xmlLine.Nocni,
                LineType = ParseZoneType(xmlLine.TLinky, xmlLine),
                ServiceAsBits = serviceAsBits,
            };
            
            if (!db.Lines.AddOrMergeVersion(routeRecord.LineNumber, routeRecord, (r1, r2) => r1.LineName == r2.LineName))
            {
                dataLog.Log(LogMessageType.WARNING_ROUTE_CONFLICTED_RECORDS, $"Linka {routeRecord} má překrývající se záznamy platnosti. Záznam s platností {routeRecord.ServiceAsBits} nebyl přidán (v případě, že jde o změnu dopravce linky může být OK).");
            }

            var routeAllVersions = db.Lines.Find(xmlLine.CLinky);
            var routeVersion = routeAllVersions.FindByServiceDays(serviceAsBits);
            if (routeVersion != null)
            {
                // jen pokud jde o první verzi linky, u ostatních to neřešíme
                foreach (var cisNumber in xmlLine.LicCislo)
                {
                    var agency = db.Agencies.GetValueOrDefault(xmlLine.CDopravce);
                    if (agency == null)
                    {
                        dataLog.Log(LogMessageType.ERROR_MISSING_AGENCY, $"Dopravce {xmlLine.CDopravce} v databázi chybí, přestože je referencován linkou {routeRecord.LineNumber}");
                        continue;
                    }

                    routeVersion.RouteAgencies.Add(new RouteAgency()
                    {
                        Agency = agency,
                        CisLineNumber = cisNumber,
                    });
                }
            }
        }

        // Rozlišení městské a příměstské linky (potřeba pro získání správné verze zastávky)
        private AswLineType ParseZoneType(string lineType, Linka xmlLine)
        {
            switch (lineType)
            {
                case "A": // městská
                    return AswLineType.PraguePublicTransport;
                case "B": // příměstská
                case "Z": // prý to samé co "B"
                    return AswLineType.SuburbanTransport;
                case "V": // regionální
                    return AswLineType.RegionalTransport;
                case "R": // vlak
                    return AswLineType.RailTransport;
                case "-": // neveřejná
                    return AswLineType.UndefinedTransport;
                default:
                    dataLog.Log(LogMessageType.WARNING_ROUTE_UNKNOWN_TYPE, $"Neznámý typ linky {lineType}. Linka i její spoje budou ignorovány.", xmlLine);
                    return AswLineType.UndefinedTransport;
            }
        }
    }
}
