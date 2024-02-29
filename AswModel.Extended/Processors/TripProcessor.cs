using AswModel.Extended.Logging;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává záznamy o spojích z ASW JŘ <see cref="Spoj"/> a ukládá je do databáze <see cref="AswSingleFileFeed.Trips"/>.
    /// 
    /// Potřebuje už mít načtené zastávky, typy vozů, linky, grafikony, poznámky a oběhy
    /// </summary>
    class TripProcessor : IProcessor<Spoj>
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private IIgnoredTripsLogger ignoredLog = Loggers.IgnoredTripsLoggerInstance;

        private AswSingleFileFeed feedFile;
        private TheAswDatabase db;
        private IDictionary<int, List<RunDescriptor>> runsByTripId;
        private bool processNonpublicTrips;

        public TripProcessor(AswSingleFileFeed feedFile, TheAswDatabase db, IDictionary<int, List<RunDescriptor>> runsByTripId, bool processNonpublicTrips)
        {
            this.feedFile = feedFile;
            this.db = db;
            this.runsByTripId = runsByTripId;
            this.processNonpublicTrips = processNonpublicTrips;
        }

        public void Process(Spoj xmlTrip)
        {
            var serviceAsBits = ServiceDaysBitmap.FromBitmapString(xmlTrip.KJ);
            var route = LoadRouteAndLog(serviceAsBits, xmlTrip);
            if (route == null)
            {
                route = TryCreateRoute(xmlTrip);
                if (route != null)
                {
                    // o lince víme pouze číslo, takže kdyby existovala jiná verze, slučujeme vždy
                    db.Lines.AddOrMergeVersion(route.LineNumber, route, (first, second) => first.LineNumber == second.LineNumber);
                    dataLog.Log(LogMessageType.WARNING_TRIP_ROUTE_MISSING_ADDED, $"Linka {xmlTrip.CLinky} není v databázi, ale vypadá jako normální, takže byl záznam pro linku dotvořen a spoj byl zpracován.", xmlTrip);
                }
                else
                {
                    dataLog.Log(LogMessageType.WARNING_TRIP_ROUTE_MISSING, $"Linka {xmlTrip.CLinky} není v databázi a nevypadá jako že by měla být veřejná. Spoj nebyl načten.", xmlTrip);
                    return;
                    // teoreticky bychom ten spoj mohli načíst alespoň do trip database, ale upřímně se bojím, jestli by se sneslo, že by měl .Route = null,
                    // to by se muselo vyzkoušet až pokud to bude někdy potřeba
                }
            }

            var trafficType = ParseTrafficType(xmlTrip);
            var remarks = ProcessRemarks(xmlTrip.PozID, xmlTrip).ToArray();

            var tripSequence = FindAndCheckTripSequenceAndLog(xmlTrip.SpojID, xmlTrip);
            if (tripSequence == null)
                return;

            var graph = LoadGraphAndLog(xmlTrip);
            if (graph == null)
                return;

            bool wheelchairAccessibility, wheelchairAccessibilityCorrect;
            if (trafficType == AswTrafficType.Metro)
            {
                // metro v datech bezbariérové není, přitom reálně je, nastavíme ručně
                wheelchairAccessibility = true;
                wheelchairAccessibilityCorrect = true;
            }
            else
            {
                wheelchairAccessibilityCorrect = db.VehicleTypeIsWheelchairAccessible.TryGetValue(xmlTrip.CTypuVozu, out wheelchairAccessibility);
            }

            var tripRecord = new Trip()
            {
                TripId = xmlTrip.SpojID,
                ServiceAsBits = serviceAsBits,
                Graph = graph,
                IsDiverted = xmlTrip.Vylukovy,
                OwnerRun = tripSequence,
                CurrentRunNumber = xmlTrip.Poradi,
                VehicleType = xmlTrip.CTypuVozu,
                Route = route,
                IsWheelchairAccessible = wheelchairAccessibility,
                TrafficType = trafficType,
                TripCharacter = (Trip.Character)xmlTrip.CCharVyk,
                TripType = (TripOperationType)xmlTrip.CTypVyk,
                TripNumber = xmlTrip.Cislo,
                DirectionId = xmlTrip.SmerTam ? 0 : 1,
                Remarks = remarks,
                IsPublic = !xmlTrip.Neverejny || processNonpublicTrips,
                HasManipulationFlag = xmlTrip.Manipulacni,
                Agency = db.Agencies.GetValueOrDefault(xmlTrip.CDopravce),
                RouteLicenceNumber = route.RouteAgencies.FirstOrDefault(ra => ra.Agency.Id == xmlTrip.CDopravce)?.CisLineNumber ?? 999999,
            };
            
            CheckTripCalendar(tripRecord);
            LoadStopTimes(tripRecord, tripSequence, xmlTrip);
            tripRecord.IsPublic = CheckAndLogIsTripPublic(tripRecord); // musí už mít načtený list zastávek a nastaveno, zda náhodou není určený jako neveřejný pomocí attr. 'neve'
            
            if (xmlTrip.CTypuVozu == 0 && tripRecord.IsPublic)
            {
                dataLog.Log(LogMessageType.WARNING_TRIP_NULL_VEHICLE_TYPE, "Spoj má nulový typ vozu, přestože dle ostatních indicií jde o normální a jede dle JŘ. Zároveň nelze určit bezbariérovou přístupnost (předpokládám false)", xmlTrip);
            }
            else if (!wheelchairAccessibilityCorrect && tripRecord.IsPublic)
            {
                dataLog.Log(LogMessageType.WARNING_TRIP_UNKNOWN_VEHICLE_TYPE, $"Typ vozu {xmlTrip.CTypuVozu} není známý. Nelze určit bezbariérovou přístupnost (předpokládám false). ", xmlTrip);
            }

            feedFile.Trips.AddTrip(tripRecord);
        }

        // zpracuje druh dopravy (číslo z ASW JŘ do enumu TrafficType)
        private AswTrafficType ParseTrafficType(Spoj xmlTrip)
        {
            AswTrafficType[] XmlTrafficTypeConverter = new[]
            {
                AswTrafficType.Undefined, // = 0
                AswTrafficType.Metro, // = 1
                AswTrafficType.Tram, // = 2
                AswTrafficType.Bus, // = 3
                AswTrafficType.Funicular, // = 4
                AswTrafficType.Rail, // = 5
                AswTrafficType.Ferry, // = 6
                AswTrafficType.Trolleybus, // = 7
            };

            try
            {
                return XmlTrafficTypeConverter[xmlTrip.CDruhuDopravy];
            }
            catch (IndexOutOfRangeException)
            {
                dataLog.Log(LogMessageType.ERROR_UNKNOWN_TRAFFIC_TYPE, $"Typ dopravy obsahuje neznámou hodnotu {xmlTrip.CDruhuDopravy}, předpokládám Undefined.", xmlTrip);
                return AswTrafficType.Undefined;
            }
        } 

        private Route LoadRouteAndLog(ServiceDaysBitmap bitmap, Spoj xmlTrip)
        {
            Route route;
            if (!db.Lines.FindOrDefault(xmlTrip.CLinky, bitmap, out route))
            {
                if (route != null)
                {
                    dataLog.Log(LogMessageType.WARNING_TRIP_MISSING_ROUTE_VERSION, $"Linka {xmlTrip.CLinky} platná v {bitmap} není v databázi. Používám jinou verzi platnou v {route.ServiceAsBits}.", xmlTrip);
                }
            }
            
            return route;
        }

        private Graph LoadGraphAndLog(Spoj xmlTrip)
        {
            var graphId = new GraphIdAndCompany()
            {
                GraphId = xmlTrip.GrafID,
                CompanyId = xmlTrip.CZavodu,
            };

            Graph graph = db.Graphs.GetValueOrDefault(graphId);
            if (graph == null)
            {
                dataLog.Log(LogMessageType.ERROR_TRIP_WRONG_GRAPH_ID, $"ID grafikonu {xmlTrip.GrafID} není v databázi grafikonů. Ignoruji spoj.", xmlTrip);
            }

            return graph;
        }

        // načte oběh a zkontroluje, jestli spoj do oběhu sedí (tzn. nezačíná později, než předchozí spoj končí)
        private List<RunDescriptor> FindAndCheckTripSequenceAndLog(int tripId, Spoj xmlTrip)
        {
            List<RunDescriptor> resultRuns;
            if (!runsByTripId.TryGetValue(tripId, out resultRuns))
            {
                dataLog.Log(LogMessageType.ERROR_TRIP_WRONG_RUN_NUM, $"Pro spoj {tripId} neexistuje oběh. Ignoruji spoj.", xmlTrip);
                return null;
            }

            if (!xmlTrip.Zstv.Any())
            {
                dataLog.Log(LogMessageType.ERROR_TRIP_NO_STOPTIMES, "Spoj nemá žádné zastavení", xmlTrip);
                return null;
            }
            else if (xmlTrip.Zstv[0].Odjezd == -1)
            {
                dataLog.Log(LogMessageType.ERROR_TRIP_STARTS_WITH_UNDEF_DEPARTURE, "Spoj začíná nedefinovaným odjezdem, lze očekávat problémy při dalším načítání", xmlTrip);
            }

            // načteme první odjezd jen abychom zjistili, kdy spoj začíná
            var startTime = new Time(xmlTrip.Zstv[0].Odjezd - xmlTrip.Zstv[0].OdjezdPoPosunu * 3600);
            if (startTime < RunDescriptor.StartOfRunFromPreviousDayUntil)
            {
                dataLog.Log(LogMessageType.WARNING_TRIP_STARTS_BEFORE_3AM, "Spoj začíná před 3. hodinou ranní, což je podezřelé, takovéto spoje totiž většinou jedou podle předchozího provozního dne.", xmlTrip);
            }

            // TODO dělat kontrolu návaznosti spojů na oběhu někde jinde, tady už to nejde, protože nově nemusí jít spoje v souboru po sobě
            //dataLog.Log(LogMessageType.WARNING_TRIP_PREMATURE_DEPARTURE, $"Spoj s výjezdem v {startTime} patří do oběhu s posledním příjezdem v {resultRun.EndTime} (cestování časem?)", xmlTrip);

            return resultRuns;
        }

        // pokud lince chybí záznam v číselníku, ale jde o nějakou asi normální linku, tak jí ten záznam vytvoříme
        private Route TryCreateRoute(Spoj xmlTrip)
        {
            Route route = null;

            if (xmlTrip.CLinky >= 1 && xmlTrip.CLinky < 49 || xmlTrip.CLinky >= 90 && xmlTrip.CLinky <= 99)
            {
                route = new Route()
                {
                    LineNumber = xmlTrip.CLinky,
                    LineName = xmlTrip.CLinky.ToString(),
                    LineType = AswLineType.PraguePublicTransport,
                    ServiceAsBits = ServiceDaysBitmap.CreateAlwaysValidBitmap(db.GlobalLastDay + 1),
                };
            }
            else if (xmlTrip.CLinky >= 100 && xmlTrip.CLinky < 280 || xmlTrip.CLinky >= 900 && xmlTrip.CLinky < 920)
            {
                route = new Route()
                {
                    LineNumber = xmlTrip.CLinky,
                    LineName = xmlTrip.CLinky.ToString(),
                    LineType = AswLineType.PraguePublicTransport,
                    ServiceAsBits = ServiceDaysBitmap.CreateAlwaysValidBitmap(db.GlobalLastDay + 1),
                };
            }
            else if (xmlTrip.CLinky >= 300 && xmlTrip.CLinky < 415)
            {
                route = new Route()
                {
                    LineNumber = xmlTrip.CLinky,
                    LineName = xmlTrip.CLinky.ToString(),
                    LineType = AswLineType.SuburbanTransport,
                    ServiceAsBits = ServiceDaysBitmap.CreateAlwaysValidBitmap(db.GlobalLastDay + 1),
                };
            }
            else if (xmlTrip.CLinky >= 415 && xmlTrip.CLinky < 799 || xmlTrip.CLinky >= 950 && xmlTrip.CLinky < 970)
            {
                route = new Route()
                {
                    LineNumber = xmlTrip.CLinky,
                    LineName = xmlTrip.CLinky.ToString(),
                    LineType = AswLineType.RegionalTransport,
                    ServiceAsBits = ServiceDaysBitmap.CreateAlwaysValidBitmap(db.GlobalLastDay + 1),
                };
            }

            return route;
        }
        
        // načte poznámky z tagu "po"; může jich být více oddělených mezerou
        private IEnumerable<Remark> ProcessRemarks(List<int> remarkIds, Spoj xmlTrip)
        {
            foreach (var remarkId in remarkIds)
            {
                Remark remark;
                if (!feedFile.Remarks.TryGetValue(remarkId, out remark))
                {
                    dataLog.Log(LogMessageType.ERROR_TRIP_MISSING_REMARK, $"Spoj odkazuje na poznámku {remarkId}, která neexistuje", xmlTrip);
                    continue;
                }

                yield return remark;
            }
        }

        private void LoadStopTimes(Trip tripRecord, List<RunDescriptor> tripRuns, Spoj xmlTrip)
        {
            // donačtení jednotlivých zastavení
            var stopTimes = new List<StopTime>();
            var stopTimesParser = new StopTimeProcessor(feedFile, db);
            foreach (var xmlStopTime in xmlTrip.Zstv)
            {
                // složité určení, jestli jsme ve smyčce (může být první nebo poslední zastávka)
                bool isFirst = xmlStopTime == xmlTrip.Zstv.First();
                bool isLast = xmlStopTime == xmlTrip.Zstv.Last();
                bool inLoop = isFirst && xmlTrip.Zstv.Count >= 2 && xmlStopTime.ZacatekSmycky && xmlTrip.Zstv[1].KonecSmycky
                           || isLast && xmlTrip.Zstv.Count >= 2 && xmlTrip.Zstv[xmlTrip.Zstv.Count - 2].ZacatekSmycky && xmlStopTime.KonecSmycky;

                var stopTime = stopTimesParser.Process(xmlStopTime, tripRecord, stopTimes.LastOrDefault(), inLoop);
                if (stopTime != null)
                {
                    stopTimes.Add(stopTime);
                }
            }

            tripRecord.SetStopTimes(stopTimes);

            // kontrola posloupnosti časů (dělá se jen u veřejných zastavení) - zároveň zaktualizuje EndTime oběhu
            var endTime = new Time();
            foreach (var stopTime in tripRecord.StopTimesPublicPart)
            {
                // kontrola návratu v čase (umožňuje se jen na ve smyčce v metru, kde to tak občas v datech je a zjevně je to OK
                if (endTime > stopTime.ArrivalTime)
                {
                    dataLog.Log(LogMessageType.WARNING_STOPTIME_PREMATURE_ARRIVAL, $"Zastavení s příjezdem {stopTime.ArrivalTime} následuje po odjezdu v {endTime} (spoj {tripRecord}, cestování časem?). Provádím korekci času.", stopTime);
                    stopTime.ArrivalTime = endTime;
                }

                endTime = stopTime.DepartureTime;

                if (stopTime.ArrivalTime > stopTime.DepartureTime)
                {
                    dataLog.Log(LogMessageType.WARNING_STOPTIME_PREMATURE_DEPARTURE, $"Zastavení s odjezdem {stopTime.DepartureTime} následuje po příjezdu v {stopTime.ArrivalTime} (spoj {tripRecord}, cestování časem?). Provádím korekci času.", stopTime);
                    stopTime.DepartureTime = stopTime.ArrivalTime;
                }
            }
        }

        // vrací, jestli je trip veřejný, pokud vrací false, měl by být ignorován
        private bool CheckAndLogIsTripPublic(Trip tripRecord)
        {
            if (!tripRecord.IsPublic)
            {
                ignoredLog.Log("Neveřejný spoj", tripRecord);
                return false;
            }

            if (tripRecord.TripCharacter != Trip.Character.PID && tripRecord.TripCharacter != Trip.Character.Contractual && tripRecord.TripCharacter != Trip.Character.SubstituteForMetro
                && tripRecord.TripCharacter != Trip.Character.SubstituteForTram && tripRecord.TripCharacter != Trip.Character.SubstituteForTrain && tripRecord.TripCharacter != Trip.Character.SubstituteOthers)
            {
                // divný spoj, ignorujeme
                ignoredLog.Log($"Charakter = {tripRecord.TripCharacter} ({(int)tripRecord.TripCharacter})", tripRecord);
                return false;
            }

            if (tripRecord.HasManipulationFlag && tripRecord.TrafficType != AswTrafficType.Tram)
            {
                // manipulační spoj, ignorujeme, u tramvají neignorujeme, protože to mají špatně
                ignoredLog.Log("Manipulační spoj (netramvajový)", tripRecord);
                return false;
            }

            // TODO ideálně vůbec ve výstupu nemít, nebo ošetřit nějak líp, než to takhle hackovat
            // alespoň mít list ignorovaných spojů
            bool isIrregular = tripRecord.TripType != TripOperationType.Regular;
            if (isIrregular)
            {
                if (tripRecord.TrafficType != AswTrafficType.Tram && tripRecord.TrafficType != AswTrafficType.Metro)
                {
                    // výjezd / zátah / přejezd v busech - vždy neveřejný
                    ignoredLog.Log("Výjezd / zátah / přejezd", tripRecord);
                    return false;
                }
                else if (tripRecord.TrafficType == AswTrafficType.Tram && !tripRecord.HasManipulationFlag)
                {
                    // u tramvají je to prapodivně opáčně - spoje označené jako manipulační reálně cestující
                    // vozí, naopak ty, které jako manipulační označené nejsou (a jsou zátah/výjezd) lidi nevozí
                    ignoredLog.Log("Výjezd / zátah / přejezd v tramvaji neoznačený jako manipulační", tripRecord);
                    return false;
                }
            }

            if (!tripRecord.PublicStopTimes.Any2())
            {
                ignoredLog.Log("Spoj má méně než dvě veřejná zastavení", tripRecord);
                return false;
            }

            return true;
        }

        // zkontroluje, že kalendář je podmnožinou kalendáře oběhu i grafu
        private void CheckTripCalendar(Trip trip)
        {
            var tripRunsBitmap = ServiceDaysBitmap.Union(trip.OwnerRun.Select(r => r.ServiceAsBits));
            if (!trip.ServiceAsBits.IsSubsetOf(tripRunsBitmap))
            {
                dataLog.Log(LogMessageType.WARNING_TRIP_CALENDAR_NOT_SUBSET_OF_SEQUENCE, $"Kalendář jízd spoje {trip.ServiceAsBits} není podmnožinou kalendáře jízd oběhu {tripRunsBitmap} (spoj {trip}, oběh {trip.OwnerRun.First()})");
            }
            
            if (!trip.ServiceAsBits.IsSubsetOf(trip.Graph.ValidityRange))
            {
                dataLog.Log(LogMessageType.WARNING_TRIP_CALENDAR_NOT_SUBSET_OF_GRAPH, $"Kalendář jízd spoje {trip.ServiceAsBits} není podmnožinou kalendáře jízd grafikonu {trip.Graph.ValidityRange} (spoj {trip}, graf {trip.Graph})");
            }
        }
    }
}
