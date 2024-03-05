using AswModel;
using AswModel.Extended;
using GtfsLogging;
using System;
using System.IO;
using GtfsModel;
using System.Linq;
using CsvSerializer;
using System.Collections.Generic;
using GtfsModel.Functions;
using JR_XML_EXP;
using CommonLibrary;
using GtfsProcessor.DataClasses;
using GtfsProcessor.Logging;

namespace GtfsProcessor
{
    class Program
    {
        public const int PidAgencyId = 99;

        private static ICommonLogger log;

        public static void Main(string[] args)
        {
            // abychom si ušetřili přehršel parametrů, přijímáme jen pracovní složku a soubor s konfigurací
            // cesta k souboru, stejně jako všechny cesty v souboru jsou relativní k pracovní složce,
            // třída Configuration je pak přeloží na plné cesty
            if (args.Length != 2)
            {
                throw new ArgumentException("Chybné argumenty aplikace. Použití: GTFS.exe <složka_data> <config_soubor>");
            }

            Configuration config;
            try
            {
                config = Configuration.Load(args[0], args[1]);
            }
            catch (Exception ex)
            {
                // nemůžeme ještě logovat, protože cestu k log složce zjistíme až z konfigurace, tak prostě vyhodíme výjimku
                throw new Exception("Chyba načítání konfigurace: ", ex);
            }

            try
            {
                // spuštění logování, před tímto voláním by každý pokus o log skončil nezdarem, proto je potřeba tím začít
                Loggers.InitLoggers(config.LogFolder, config.MaxSimilarLogRecords);
                log = Loggers.CommonLoggerInstance;

                // po jednom načítáme data z XML souborů do paměti (DAVKAJR je jen datová třída odpovídající XML struktuře - AswModel)
                var aswXmlFileData = new List<Tuple<string, DavkaJR>>();
                foreach (var fileName in config.TripFiles)
                {
                    // mohli bychom použít TheDatabase.Construct rovnou z názvů souborů, ale to bychom nemohli na konzoli vypisovat jednotlivé načítané soubory...
                    aswXmlFileData.Add(new Tuple<string, DavkaJR>(fileName, ProcessFile(fileName)));
                }

                //// TODO hack, dokud neodladím, že je to shodné s původním feedem, pak lze dát pryč a otestovat, že jsem tím nic nezničil
                Console.WriteLine("Fix ID grafikonů a tripů (kompatibilita IDček)...");
                for (int i = 0; i < aswXmlFileData.Count; i++)
                {
                    foreach (var obeh in aswXmlFileData[i].Item2.Obehy)
                    {
                        for (int j = 0; j < obeh.SpojID.Count; j++)
                        {
                            obeh.SpojID[j] += i * 1000000;
                        }

                        foreach (var ds in obeh.DlouheSpoje)
                        {
                            for (int k = 0; k < ds.SpojID.Count; k++)
                            {
                                ds.SpojID[k] += i * 1000000;
                            }
                        }
                    }

                    foreach (var spoj in aswXmlFileData[i].Item2.Spoje)
                    {
                        spoj.SpojID += i * 1000000;
                    }
                }
                ////////////////

                // načtené XML soubory postupně zapracujeme do databáze ASW (AswModel.Extended)
                Console.WriteLine("Zpracování dat...");
                var db = TheAswDatabase.Construct(config.ProcessNonpublicTrips, aswXmlFileData.ToArray());
                aswXmlFileData = null; // lze collectovat

                // ověření, že jsme načetli dost dat (tj. že v nějakém souboru nechybí podstatná část obsahu/není prázdný)
                // minimální smysluplné počty spojů se definují v konfiguraci
                VerifyData(db, config);

                // odstraníme linky, které dle konfigurace nejsou veřejné (do XML občas proniknou, tak abychom je uměli snadno vypínat)
                FilterData(db, config);

                // hlavní akce, transformace AswModel.Extended do GTFS (přes GtfsModel.Extended a GtfsModel)
                SaveGtfs(db, config);
            }
            finally
            {
                Loggers.CloseLoggers();
            }
        }

        // pouze deserializace XML souboru do AswModelu (C# reprezentace obsahu XML souboru 1:1)
        static DavkaJR ProcessFile(string fileName)
        {
            Console.WriteLine($"Načítání souboru: {Path.GetFileName(fileName)} ..");
            return AswXmlSerializer.Deserialize(fileName);
        }

        // ověření, že načtená data obsahují dostatečný počet spojů (počty se zadávají v konfiguraci)
        static void VerifyData(TheAswDatabase db, Configuration config)
        {
            VerifyTripCount(db, trip => trip.TrafficType == AswTrafficType.Metro, config.MinimumMetroTrips, "METRO");
            VerifyTripCount(db, trip => trip.TrafficType == AswTrafficType.Tram, config.MinimumTramTrips, "TRAM");
            VerifyTripCount(db, trip => trip.TrafficType == AswTrafficType.Bus && trip.Route.LineNumber <= 299, config.MinimumBusTo299Trips, "BUS do 299");
            VerifyTripCount(db, trip => trip.TrafficType == AswTrafficType.Bus && trip.Route.LineNumber >= 300 && trip.Route.LineNumber < 800, config.MinimumBusFrom300Trips, "BUS od 300 do 800");
        }

        // ověří dostatek tripů daného druhu (pomocník pro VerifyData)
        static void VerifyTripCount(TheAswDatabase db, Func<Trip, bool> tripSelector, int minCount, string description)
        {
            var count = db.GetAllTrips().Where(tripSelector).Count();
            Console.WriteLine($"Verifikace spojů {description}. Načteno {count}, požadováno {minCount}.");
            if (count < minCount)
            {
                throw new Exception($"Příliš málo načtených spojů {description}. Načteno {count} spojů, požadováno je však minimálně {minCount} spojů.");
            }
        }
        
        // odstraní linky, které jsou v konfiguraci napevno označené jako neveřejné
        static void FilterData(TheAswDatabase db, Configuration config)
        {            
            foreach (var ignoredLine in config.IgnoredLines)
            {
                db.Lines.Remove(ignoredLine);
            }
        }
        
        // transformace AswModel.Extended --> GtfsModel.Extended --> GtfsModel --> GTFS soubor
        static void SaveGtfs(TheAswDatabase db, Configuration config)
        {
            TravelTimeAdjustment.Proceed(db);

            // sem budeme postupně skládat výsledná GTFS data
            var gtfsFeedEx = new GtfsModel.Extended.Feed();
            gtfsFeedEx.FeedInfo = CreateFeedInfoInstance(db.GlobalStartDate, db.GlobalStartDate.AddDays(db.GlobalLastDay));
            
            // jeden GTFS dopravce PID, to je trivka, použijí ho pak všechny linky
            Console.WriteLine("Sestavuji agency...");
            gtfsFeedEx.Agency = new Dictionary<int, GtfsAgency>() { { PidAgencyId, CreateAgencyInstance() } };

            // GTFS zastávky si jednak vyrobíme podle číselníku zastávek a spojů, které projíždějí - zastávky, kde nic nejede, ignorujeme,
            // zastávky v Praze, kde jezdí město i příměsto budou mít dva záznamy kvůli rozdílným pásmům (viz StopVariantsMapping);
            // zároveň si uložíme mapopvání ASW zastávek na GTFS zastávky, ještě se bude hodit při konstrukci stop times
            Console.WriteLine("Sestavuji stops...");
            Dictionary<Stop, StopVariantsMapping> stopsTransformation; // pro následnou tvorbu mapování asw zastávka -> gtfs zastávka, až budeme generovat stoptimes
            gtfsFeedEx.Stops = new StopsTransformation(db, log).CollectAllStopsAndStations(out stopsTransformation).ToDictionary(s => s.GtfsId);

            // GTFS linky jednoduše odpovídají číselníkovým položkám, maximálně kdyby nějaká linka měnila název, bude v GTFS dvakrát;
            // zároveň si uložíme mapování ASW linek na GTFS linky, ještě se bude hodit při konstrukci spojů
            Console.WriteLine("Sestavuji routes...");
            var routesTransformation = new RoutesTransformation(db, log).TransformRoutesToGtfs();
            gtfsFeedEx.Routes = routesTransformation.Values.ToDictionary(r => r.GtfsId);

            // spoje, než s nimi začneme pracovat, sloučíme tak, abychom neměli zbytečné duplicity; dále už budeme pracovat pouze
            // s MergedTripGroupy a budeme je normálně považovat za spoje, ač vznikly sloučením více (ale identických) spojů z ASW;
            Console.WriteLine("Merge spojů...");
            var mergedTripGroups = new TripMergeOperation(db.Lines.SelectMany(l => l.AllVersions())).Perform().ToList(); // ten ToList je hodně důležitý, jinak bychom kvůli lazy vyhodnocování to zbytečně spouštěli vícekrát a ještě k tomu bychom si tím vyráběli různé instance stejného tripu
            // TODO dokud porovnáváme s původním feedem, abychom udrželi stejné pořadí spojů; pak lze smazat
            mergedTripGroups = mergedTripGroups.OrderBy(t => t.TripIds.Min()).ToList();
            
            // tady trochu opravujeme data v případě přejezdů v síti tramvají, více viz TramTripBlockHeadsignProcessor, upravuje přímo data v 'mergedTripGroups'
            Console.WriteLine("Opravy headsign a čísel linek tramvají...");
            mergedTripGroups = new TramTripBlockHeadsignProcessor(log).ProcessTripsBlocks(mergedTripGroups); // tohle vždy musíme provést před formací tripů, protože zde měníme owner routes a to má vliv na GTFS ID tripu

            // vyrobíme GTFS kalendáře podle bitmapových kalendářů, jak to funguje viz CalendarGenerator;
            // zároveň si uložíme mapování spojů na jejich kalendáře, bude se nám hodit při konstrukci spojů
            Console.WriteLine("Sestavuji kalendáře spojů...");
            Dictionary<MergedTripGroup, GtfsModel.Extended.CalendarRecord> calendarToTripAssignment;
            var calendarGenerator = new CalendarGenerator(db.GlobalStartDate);
            gtfsFeedEx.Calendar = calendarGenerator.GenerateCalendarsForTrips(mergedTripGroups, out calendarToTripAssignment).ToDictionary(cal => cal.GtfsId);

            // může se stát, že oběhy mají ještě nějaké kalendáře, které neznáme, tak si je dovygenerujeme a zároveň přiřadíme
            Console.WriteLine("Sestavuji kalendáře oběhů...");
            var runsProcessor = new RunsTransformation(mergedTripGroups);
            Dictionary<MergedRun, GtfsModel.Extended.CalendarRecord> calendarToRunAssignment;
            gtfsFeedEx.Calendar.AddRange(calendarGenerator.GenerateCalendarsForRuns(runsProcessor.Runs, out calendarToRunAssignment).ToDictionary(cal => cal.GtfsId));

            // ještě než půjdeme generovat trasy, musíme zkorigovat polohy nástupišť metra (možná je totiž opravuje dodatečný list zastávek)
            List<GtfsStop> extraStops = null;
            if (config.AdditionalStopsFileName != null)
            {
                extraStops = CsvFileSerializer.DeserializeFile<GtfsStop>(config.AdditionalStopsFileName);
                CorrectStopPositionsInAsw(db.Stops, extraStops);
            }

            // vyrobíme GTFS trasy spojům tak, že spoje se stejnou posloupností zastávek a variant tras budou sdílet stejnou trasu;
            // zároveň si uložíme mapování spojů na trasy a použijeme ho při konstrukci GTFS tripů
            Console.WriteLine("Generuji trasy...");
            Dictionary<MergedTripGroup, ShapeEx> shapeToTripAssignment;
            var shapeConstructor = new ShapeConstructor();
            shapeConstructor.LoadPointData(config.MetroNetworkFile);
            var metroStationPlatforms = db.Stops.Select(s => s.FirstVersion()).Where(s => s.IsMetro && s.IsPublic);
            gtfsFeedEx.Shapes = new ShapeGenerator().GenerateAndAssignShapes(mergedTripGroups, out shapeToTripAssignment, shapeConstructor, metroStationPlatforms, log).ToDictionary(sh => sh.Id);

            // protože spoje mají IDčka v ASW víceméně náhodná (každý den jiná) a protože chceme, aby byly IDčka spojů pokud možno
            // stále stejná a stabilní mezi feedy, máme databázi, která IDčka určuje podle jízdního řádu, databázi si tedy inicializujeme
            // a použijeme při konstrukci GTFS trips k určení GTFS ID
            Console.WriteLine("Připravuji databázi trip IDs...");
            var tripPersistentIdDb = new TripIdPersistentDb(config.TripPersistentDbFolder);
            tripPersistentIdDb.Init(db.GlobalStartDate, mergedTripGroups);

            // zlatý hřeb, konstrukce GTFS spojů, ze skupiny spojů sloučených do jednoho děláme vždy jeden GTFS trip,
            // k tomu využijeme mapy, které jsme si vyrobili při konstrukci zastávek, linek, kalendářů a tras,
            // abychom na základě ASW dat našli správné reference na odpovídající GTFS prvky;
            // zároveň si vyžádáme mapování GTFS stoptimes na ASW přestupní poznámky pro generování trip-to-trip transfers
            Console.WriteLine("Sestavuji trips...");
            Dictionary<GtfsModel.Extended.StopTime, List<Remark>> stopTimesWithTimedTransferRemarks;
            var tripsTransformation = new TripsTransformation(routesTransformation, stopsTransformation, shapeToTripAssignment, calendarToTripAssignment, tripPersistentIdDb)
                .TransformTripsToGtfs(mergedTripGroups, out stopTimesWithTimedTransferRemarks);
            gtfsFeedEx.Trips = tripsTransformation.Values.ToDictionary(t => t.GtfsId);
            new CalendarDebugLogger(gtfsFeedEx.Calendar.Values).LogCalendars();

            var reusedIdsPercentage = 0;
            if (tripPersistentIdDb.TripsWithReusedId + tripPersistentIdDb.TripsWithNewId > 0)
            {
                reusedIdsPercentage = tripPersistentIdDb.TripsWithReusedId * 100 / (tripPersistentIdDb.TripsWithReusedId + tripPersistentIdDb.TripsWithNewId);
            }

            Console.WriteLine($"  - v následujících dnech znovu využito {tripPersistentIdDb.TripsWithReusedId}, přiděleno {tripPersistentIdDb.TripsWithNewId} nových trip ID (reuse {reusedIdsPercentage} %)");
            if (reusedIdsPercentage < config.MinimumTripDatabaseHitPercentage)
            {
                throw new Exception($"Podezřele příliš mnoho nově přidělených trip IDs, reuse je pouze {reusedIdsPercentage} %, ale limit {config.MinimumTripDatabaseHitPercentage} %");
            }

            if (!config.TripDbAsReadOnly)
            {
                Console.WriteLine("Ukládám databázi trip IDs...");
                tripPersistentIdDb.SaveTripDatabase();
            }
            else
            {
                Console.WriteLine("Databáze trip IDS byla jen pro čtení a úpravy v ní se neukládají.");
            }

            //// Zde máme načtený GTFS model se spoji, zastávkami atd.

            if (config.TrainGtfsFolder != null)
            {
                // vlaky zpracováváme úplně bokem z dat SŽ, takže jen vlastně děláme merge dvou GTFS
                Console.WriteLine("Načítám GTFS vlaků...");
                var trainsGtfsFeed = GtfsFeedSerializer.DeserializeFeed(config.TrainGtfsFolder);
                gtfsFeedEx.MergeWith(trainsGtfsFeed,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.AllowDuplicityTakeOriginal,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity);  
            }
            else
            {
                Console.WriteLine("XX Soubor s GTFS vlaků nezadán, vlaky nebudou načteny.");
            }

            Console.WriteLine("Verifikace počtu vygenerovaných spojů...");
            VerifyLoadedTripCount(gtfsFeedEx, trip => trip.Route.Type == GtfsModel.Enumerations.TrafficType.Metro, config.MinimumMetroTrips, "METRO");
            VerifyLoadedTripCount(gtfsFeedEx, trip => trip.Route.Type == GtfsModel.Enumerations.TrafficType.Tram, config.MinimumTramTrips, "TRAM");
            VerifyLoadedTripCount(gtfsFeedEx, trip => trip.Route.Type == GtfsModel.Enumerations.TrafficType.Bus && !trip.Route.IsRegional, config.MinimumBusTo299Trips, "BUS městský");
            VerifyLoadedTripCount(gtfsFeedEx, trip => trip.Route.Type == GtfsModel.Enumerations.TrafficType.Bus && trip.Route.IsRegional, config.MinimumBusFrom300Trips, "BUS příměstský");
            VerifyLoadedTripCount(gtfsFeedEx, trip => trip.Route.Type == GtfsModel.Enumerations.TrafficType.Rail, config.MinimumTrainTrips, "VLAK");

            if (!string.IsNullOrEmpty(config.AdditionalTransfersFileName))
            {
                // doplnění přestupů do transfers.txt, zatím se to moc nepoužívá
                Console.WriteLine($"Načítám přestupy z {config.AdditionalTransfersFileName}...");
                var transfers = new CustomTransfersProcessor(config.AdditionalTransfersFileName, gtfsFeedEx.Stops.Values).ParseTimeTransfers();
                gtfsFeedEx.Transfers.AddRange(transfers);
            }
            else
            {
                Console.WriteLine("XX Nezadán soubor s přestupy navíc, které by měly být přidány do feedu.");
            }

            // trip-to-trip transfers na základě návazných poznámek v ASW JŘ - vyrábíme až zde, protože jsme
            // čekali na načtení vlaků, abychom mohli dělat reference vlak-bus
            Console.WriteLine("Zpracovávám garantované přestupní poznámky...");
            var timedTransfers = new RemarksToTransfersProcessor(gtfsFeedEx.Routes.Values, stopsTransformation, db.GlobalStartDate).ParseTimedTransferRemarks(stopTimesWithTimedTransferRemarks);
            gtfsFeedEx.Transfers.AddRange(timedTransfers);

            var gtfsFeed = gtfsFeedEx.ToGtfsFeed();
            if (extraStops != null)
            {
                // nakonec ruční doplnění vstupů do metra a případných ručních zásahů do zastávek a stanic z externího zdroje
                Console.WriteLine($"Doplňuji zastávky z {config.AdditionalStopsFileName}...");
                MergeFeedStops(gtfsFeed, extraStops);
            }
            else
            {
                Console.WriteLine("XX Nejsou zadány žádné zastávky navíc, které by měly být přidány do feedu.");
            }

            if (config.FareRulesFileName != null)
            {
                Console.WriteLine("Načítám fare_rules...");
                var allFareRules = CsvFileSerializer.DeserializeFile<GtfsFareRule>(config.FareRulesFileName);

                // profiltruje záznamy a ponechá ty, co se nevztahují k žádné konkrétní lince (položka není vyplněná) a pokud se vztahují, musí linka existovat
                gtfsFeed.FareRules = allFareRules.Where(fr => string.IsNullOrWhiteSpace(fr.RouteId) || gtfsFeedEx.Routes.ContainsKey(fr.RouteId)).ToList();
            }
            else
            {
                Console.WriteLine("XX Soubor s fare_rules nezadán, výstup bude bez fare_rules.txt.");
            }

            Console.WriteLine("Doplňuji oběhy...");
            gtfsFeed.TripRuns = runsProcessor.GetTransformedRuns(tripsTransformation, calendarToRunAssignment).ToList();

            Console.WriteLine("Sestavuji trasy linek...");
            gtfsFeed.RouteStops = new RouteStopsGenerator().Generate(gtfsFeedEx.Routes.Values).ToList();
            
            Console.WriteLine("Ukládání souborů...");
            GtfsFeedSerializer.SerializeFeed(config.GtfsOutputFolder, gtfsFeed);
        }

        static void VerifyLoadedTripCount(GtfsModel.Extended.Feed gtfsFeedEx, Func<GtfsModel.Extended.Trip, bool> tripSelector, int minCount, string description)
        {
            var count = gtfsFeedEx.Trips.Values.Where(tripSelector).Count();
            Console.WriteLine($"  - {description}: Načteno {count}, požadováno {minCount}.");
            if (count < minCount)
            {
                throw new Exception($"Příliš málo vygenerovaných spojů {description} v GTFS. Vygenerováno {count} spojů, požadováno je však minimálně {minCount} spojů.");
            }
        }

        static GtfsAgency CreateAgencyInstance()
        {
            return new GtfsAgency()
            {
                Id = PidAgencyId,
                Name = "Pražská integrovaná doprava",
                Lang = "cs",
                Phone = "+420234704560",
                Timezone = "Europe/Prague",
                Url = "https://pid.cz",
            };
        }

        static GtfsFeedInfo CreateFeedInfoInstance(DateTime globalStartDate, DateTime globalEndDate)
        {
            return new GtfsFeedInfo()
            {
                PublisherName = "ROPID",
                PublisherUrl = "https://pid.cz",
                Lang = "cs",
                StartDate = globalStartDate,
                EndDate = globalEndDate,
                ContactEmail = "opendata@pid.cz",
            };
        }

        // zapojí do feedu zastávky ze seznamu stops - pokud ve feedu existuje záznam se stejným ID, je přepsán
        static void MergeFeedStops(GtfsFeed gtfsFeed, List<GtfsStop> additionalStops)
        {
            foreach (var stop in additionalStops)
            {
                var alreadyPresentIndex = gtfsFeed.Stops.FindIndex(s => s.Id == stop.Id);
                if (alreadyPresentIndex > 0)
                {
                    // již přítomna => přepis
                    gtfsFeed.Stops[alreadyPresentIndex] = stop;
                }
                else
                {
                    // nový záznam
                    gtfsFeed.Stops.Add(stop);
                }
            }
        }

        private static void CorrectStopPositionsInAsw(StopDatabase stops, List<GtfsStop> extraStops)
        {
            foreach (var extraStop in extraStops.Where(es => es.AswNodeId > 0 && es.AswStopId > 0))
            {
                var stopDbKey = StopDatabase.CreateKey(extraStop.AswNodeId, extraStop.AswStopId);
                var stopInDb = stops.Find(stopDbKey);
                if (stopInDb == null)
                    continue;

                foreach (var stopVersion in stopInDb.AllVersions())
                {
                    stopVersion.Position = new Coordinates()
                    {
                        GpsLatitude = extraStop.Latitude,
                        GpsLongitude = extraStop.Longitude,
                    };
                }
            }
        }
    }
}
