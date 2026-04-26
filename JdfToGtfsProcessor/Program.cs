using CommonLibrary;
using GtfsLogging;
using GtfsModel.Functions;
using JdfModel;
using JdfToGtfsProcessor.Stops;
using JdfToGtfsProcessor.Transfers;
using Microsoft.Extensions.Configuration;
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
            if (settings.JdfFolder == null) 
            {
                Console.WriteLine("V konfiguračním souboru není zadána cesta ke složce s JDF soubory (položka JdfFolder). Nelze pokračovat.");
                return;
            }
            else if (!Directory.Exists(settings.JdfFolder))
            {
                Console.WriteLine($"Složka {settings.JdfFolder} zadaná jako zdroj JDF dat nebyla nalezena. Nelze pokračovat.");
                return;
            }

            var jdfFeeds = JdfFeed.LoadFromDirectoryRecursive(settings.JdfFolder).ToList();
            if (!jdfFeeds.Any())
            {
                Console.WriteLine($"Ze složky {settings.JdfFolder} nebyl načten žádný JDF feed (je cesta zadána správně?). Nelze pokračovat.");
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
                    false);
            }

            var gtfsFeedEx = jdfFeedProcessor.GetResultGtfsFeed(commonLog);

            Console.WriteLine("Transformuji data do finálního GTFS modelu...");
            var tripsToRemove = gtfsFeedEx.Trips.Values.Where(t => !t.StopTimes.Any2()).ToArray();
            foreach (var tripToRemove in tripsToRemove)
            {
                commonLog.Log($"Spoj {tripToRemove} má méně než dvě zastavení, bude smazán.");
                gtfsFeedEx.Trips.Remove(tripToRemove.GtfsId);
            }

            foreach (var trip in gtfsFeedEx.Trips.Values)
            {
                trip.Headsign = trip.StopTimes.Last().Stop.Name;
                trip.Route.Trips.Add(trip);
            }

            Console.WriteLine("Načítám soubory s garantovanými přestupy...");
            var busToBusTransfers = LoadTransfersFile(settings.BusToBusTransfersFile, "BUS->BUS");
            var trainToBusTransfers = LoadTransfersFile(settings.TrainToBusTransfersFile, "VLAK->BUS");
            var transferLog = new SimpleLogger(logFactory.CreateWriterToFile("JdfProcessor_Transfers"));
            var transferProcessor = new TimedTransfersProcessor(busToBusTransfers, trainToBusTransfers, transferLog, stopDatabase);

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
