using CommonLibrary;
using GtfsLogging;
using GtfsModel.Functions;
using JdfModel;
using JdfToGtfsProcessor.Stops;
using System.Xml.Serialization;

namespace JdfToGtfsProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var logFactory = new LogWriterFactory(@"c:\temp\jrspoje\log");
            var log = new SimpleLogger(logFactory.CreateWriterToFile("JdfProcessor_Common"));

            StopDatabase stopDatabase;
            Console.WriteLine("Načítám data o zastávkách a návaznostech...");
            try
            {
                var stopData = LoadXmlData<StopDataCollection>(@"c:\temp\jrspoje\jdf\ZAST_ODIS.xml");
                var stopDataDictionary = stopData!.GetStopsByNumbers();
                stopDatabase = new StopDatabase(stopDataDictionary);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Nepodařilo se načíst data ze ZAST_ODIS.xml:\n\n" + ex.Message);
                return;
            }

            Console.WriteLine("Načítám JDF...");
            var jdfFeeds = JdfFeed.LoadFromDirectoryRecursive(@"c:\temp\jrspoje\jdf\").ToList();
            var jdfFeedProcessor = new JdfFeedProcessor(stopDatabase);
            jdfFeedProcessor.InitFeed(FeedPublisher.KODIS);

            foreach (var jdfFeedEntry in jdfFeeds)
            {
                Console.WriteLine(jdfFeedEntry.path);
                jdfFeedProcessor.ProcessFeed(jdfFeedEntry.feed, new SimpleLoggerByFile(jdfFeedEntry.path, log), false);
            }

            var gtfsFeedEx = jdfFeedProcessor.GetResultGtfsFeed(log);

            Console.WriteLine("Transformuji data do finálního GTFS modelu...");
            var tripsToRemove = gtfsFeedEx.Trips.Values.Where(t => !t.StopTimes.Any2()).ToArray();
            foreach (var tripToRemove in tripsToRemove)
            {
                log.Log($"Spoj {tripToRemove} má méně než dvě zastavení, bude smazán.");
                gtfsFeedEx.Trips.Remove(tripToRemove.GtfsId);
            }

            foreach (var trip in gtfsFeedEx.Trips.Values)
            {
                trip.Headsign = trip.StopTimes.Last().Stop.Name;
            }

            var gtfsFeed = gtfsFeedEx.ToGtfsFeed();

            Console.WriteLine("Ukládám GTFS soubory...");
            GtfsFeedSerializer.SerializeFeed(@"c:\temp\jrspoje\gtfs_output", gtfsFeed);

            log.Close();
            Console.WriteLine("Hotovo.");
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



    }
}
