using CommonLibrary;
using GtfsLogging;
using GtfsModel.Functions;
using JdfModel;
using JdfToGtfsProcessor.Stops;
using JdfToGtfsProcessor.Transfers;
using Microsoft.Extensions.Configuration;
using ShapeManager;
using System.Xml.Serialization;

namespace JdfToGtfsProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = LoadSettings();
            if (settings.LogFolder == null)
            {
                Console.WriteLine("V konfiguračním souboru není zadána složka pro ukládání logů (položka LogFolder). Nelze pokračovat.");
                return;
            }

            var logFactory = new LogWriterFactory(settings.LogFolder);
            var commonLog = new SimpleLogger(logFactory.CreateWriterToFile("JdfProcessor_Common"));
            var missingPlatformCodeLog = new SimpleLogger(logFactory.CreateWriterToFile("JdfProcessor_MissingPlatformCodes"));
            var routeLog = new SimpleLogger(logFactory.CreateWriterToFile("JdfProcessor_RouteLog"));
            var jdfTimedTransferLog = new SimpleLogger(logFactory.CreateWriterToFile("JdfProcessor_JdfTimedTransfers"));

            StopDatabase stopDatabase;
            Console.WriteLine("Načítám data o zastávkách...");
            if (settings.StopDataFile == null)
            {
                Console.WriteLine("V konfiguračním souboru není zadána cesta k souboru s daty zastávek (položka StopDataFile). Nelze pokračovat.");
                return;
            }

            try
            {
                var stopData = LoadXmlData<StopDataCollection>(settings.StopDataFile);
                var stopDataDictionary = stopData!.GetStopsByNumbers();
                stopDatabase = new StopDatabase(stopDataDictionary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nepodařilo se načíst data ze {settings.StopDataFile}:\n\n" + ex.Message);
                return;
            }

            Console.WriteLine("Načítám JDF...");
            if (settings.JdfFolders == null) 
            {
                Console.WriteLine("V konfiguračním souboru není zadána žádná cesta ke složce s JDF soubory (položka JdfFolders). Nelze pokračovat.");
                return;
            }

            var jdfFeeds = new List<(string path, JdfFeed feed)>();
            foreach (var jdfFolder in settings.JdfFolders)
            {
                if (!Directory.Exists(jdfFolder))
                {
                    Console.WriteLine($"Složka {jdfFolder} zadaná jako zdroj JDF dat nebyla nalezena.");
                    continue;
                }

                Console.WriteLine(jdfFolder + "...");
                var jdfFeedsThisFile = JdfFeed.LoadFromDirectoryRecursive(jdfFolder, out var exceptions).ToList();
                jdfFeeds.AddRange(jdfFeedsThisFile);
                foreach (var ex in exceptions)
                {
                    Console.WriteLine(ex);
                    commonLog.Log(ex.ToString());
                }

            }

            if (!jdfFeeds.Any())
            {
                Console.WriteLine($"Ze zadaných složek nebyl načten žádný JDF feed (je cesta zadána správně?). Nelze pokračovat.");
                return;
            }

            var jdfFeedProcessor = new JdfFeedProcessor(stopDatabase);
            jdfFeedProcessor.InitFeed(FeedPublisher.KODIS);

            foreach (var jdfFeedEntry in jdfFeeds)
            {
                Console.WriteLine(jdfFeedEntry.path);
                jdfFeedProcessor.ProcessFeed(jdfFeedEntry.feed, 
                    new SimpleLoggerByFile(jdfFeedEntry.path, commonLog),
                    new SimpleLoggerByFile(jdfFeedEntry.path, missingPlatformCodeLog),
                    new SimpleLoggerByFile(jdfFeedEntry.path, routeLog),
                    new SimpleLoggerByFile(jdfFeedEntry.path, jdfTimedTransferLog),
                    true);
            }

            var gtfsFeedEx = jdfFeedProcessor.GetResultGtfsFeed(commonLog);

            Console.WriteLine("Transformuji data do finálního GTFS modelu...");
            var tripsToRemove = gtfsFeedEx.Trips.Values.Where(t => !t.StopTimes.Any2()).ToArray();
            foreach (var tripToRemove in tripsToRemove)
            {
                commonLog.Log($"Spoj {tripToRemove} má méně než dvě zastavení, bude smazán.");
                gtfsFeedEx.Trips.Remove(tripToRemove.GtfsId);
            }

            Console.WriteLine("Konstruuji trasy...");
            var shapeLog = new CommonLogger(logFactory.CreateWriterToFile("JdfProcessor_ShapeConstructor"));
            ConstructShapesFromNetwork(settings.TrolleybusNetworkFile, null, GtfsModel.Enumerations.TrafficType.Trolleybus, gtfsFeedEx, shapeLog);
            ConstructShapesFromNetwork(settings.BusNetworkFile, null, GtfsModel.Enumerations.TrafficType.Bus, gtfsFeedEx, shapeLog);

            Console.WriteLine("Načítám soubory s garantovanými přestupy...");
            var busToBusTransfers = LoadTransfersFile(settings.BusToBusTransfersFile, "BUS->BUS");
            var trainToBusTransfers = LoadTransfersFile(settings.TrainToBusTransfersFile, "VLAK->BUS");
            var externalTransferLog = new SimpleLogger(logFactory.CreateWriterToFile("JdfProcessor_ExternalTransfers"));
            var transferProcessor = new ExternalTimedTransfersProcessor(busToBusTransfers, trainToBusTransfers, externalTransferLog, stopDatabase);

            Console.WriteLine("Zpracovávám garantované přestupy BUS->BUS...");
            gtfsFeedEx.Transfers.AddRange(transferProcessor.ProcessBusToBusTransfers(gtfsFeedEx.Routes));

            if (settings.TrainGtfsFolder != null)
            {
                // vlaky zpracováváme úplně bokem z dat SŽ, takže jen vlastně děláme merge dvou GTFS
                Console.WriteLine("Načítám GTFS vlaků...");
                var trainsGtfsFeed = GtfsFeedSerializer.DeserializeFeed(settings.TrainGtfsFolder);
                gtfsFeedEx.MergeWith(trainsGtfsFeed,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.AllowDuplicityTakeOriginal,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity,
                    GtfsModel.Extended.Feed.MergeDuplicityRule.DisallowDuplicity);

                Console.WriteLine("Zpracovávám garantované přestupy VLAK->BUS...");
                gtfsFeedEx.Transfers.AddRange(transferProcessor.ProcessTrainToBusTransfers(gtfsFeedEx.Routes));
            }
            else
            {
                Console.WriteLine("XX Soubor s GTFS vlaků nezadán, vlaky nebudou načteny.");
            }

            var gtfsFeed = gtfsFeedEx.ToGtfsFeed();

            Console.WriteLine("Ukládám GTFS soubory...");
            GtfsFeedSerializer.SerializeFeed(settings.OutputFolder, gtfsFeed);

            commonLog.Close();
            missingPlatformCodeLog.Close();
            routeLog.Close();
            externalTransferLog.Close();
            jdfTimedTransferLog.Close();
            Console.WriteLine("Hotovo.");
        }

        private static AppSettings LoadSettings()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var settings = config.Get<AppSettings>();
            return settings!;
        }

        private static T? LoadXmlData<T>(string path)
        {
            var serializer = new XmlSerializer(typeof(T));

            using (var reader = new StreamReader(path))
            {
                var data = (T?)serializer.Deserialize(reader);
                return data;
            }
        }

        private static void ConstructShapesFromNetwork(string? networkFileName, string? waypointsFileName, GtfsModel.Enumerations.TrafficType trafficType, GtfsModel.Extended.Feed gtfsFeedEx, ICommonLogger log)
        {
            if (string.IsNullOrEmpty(networkFileName))
            {
                return;
            }

            var trips = gtfsFeedEx.Trips.Values.Where(t => t.Route.Type == trafficType).ToList();
            var stops = trips.SelectMany(t => t.StopTimes).Select(st => st.Stop).Distinct().ToList();
            var shapeDb = ShapeDatabase.Create(networkFileName, stops, log, waypointsFileName, 120);
            shapeDb.ProcessTrips(trips);
            foreach (var shape in shapeDb.Shapes)
            {
                gtfsFeedEx.Shapes.Add(shape.Id, shape);
            }
        }

        private static List<XmlTransfer> LoadTransfersFile(string? path, string transferName)
        {
            if (path != null)
            {
                try
                {
                    var transferCollection = LoadXmlData<XmlTransferCollection>(path);
                    if (transferCollection == null || transferCollection.Items == null)
                    {
                        Console.WriteLine($"Soubor {path} je prázdný.");
                        return new();
                    }

                    return transferCollection.Items;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nepodařilo se načíst data ze {path}:\n\n" + ex.Message);
                    return new();
                }
            }
            else
            {
                Console.WriteLine($"Soubor pro návaznosti {transferName} není zadán.");
                return new();
            }
        }
    }
}
