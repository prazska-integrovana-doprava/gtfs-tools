using CsvSerializer;
using System.IO;

namespace GtfsModel.Functions
{
    /// <summary>
    /// Zajišťuje serializaci a deserializaci GTFS souborů.
    /// </summary>
    /// <remarks>
    /// Serializovat je možné pouze položky primitivních typů <see cref="int"/>, <see cref="string"/>, <see cref="Enum"/>
    /// a třídy implementující <see cref="IGtfsSerializable{T}"/>
    /// </remarks>
    public static class GtfsFeedSerializer
    {
        /// <summary>
        /// Uloží GTFS feed do TXT souborů v zadané složce
        /// </summary>
        /// <param name="outputFolder">Výstupní složka, do které budou uloženy TXT soubory</param>
        /// <param name="gtfsFeed">GTFS feed</param>
        public static void SerializeFeed(string outputFolder, GtfsFeed gtfsFeed)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "agency.txt"), gtfsFeed.Agency);
            CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "stops.txt"), gtfsFeed.Stops);
            CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "routes.txt"), gtfsFeed.Routes);
            CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "trips.txt"), gtfsFeed.Trips);
            CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "stop_times.txt"), gtfsFeed.StopTimes);
            if (gtfsFeed.Calendar != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "calendar.txt"), gtfsFeed.Calendar);
            if (gtfsFeed.CalendarDates != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "calendar_dates.txt"), gtfsFeed.CalendarDates);
            if (gtfsFeed.FareRules != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "fare_rules.txt"), gtfsFeed.FareRules);
            if (gtfsFeed.Shapes != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "shapes.txt"), gtfsFeed.Shapes);
            if (gtfsFeed.Transfers != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "transfers.txt"), gtfsFeed.Transfers);
            if (gtfsFeed.FeedInfo != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "feed_info.txt"), gtfsFeed.FeedInfo);

            if (gtfsFeed.RouteSubAgencies != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "route_sub_agencies.txt"), gtfsFeed.RouteSubAgencies);
            if (gtfsFeed.TripRuns != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "obehy.csv"), gtfsFeed.TripRuns);
            if (gtfsFeed.RouteStops != null) CsvFileSerializer.SerializeFile(Path.Combine(outputFolder, "route_stops.txt"), gtfsFeed.RouteStops);
        }

        /// <summary>
        /// Načte GTFS feed z TXT souborů ve složce
        /// </summary>
        /// <param name="folder">Složka, ve které jsou uloženy vstupní soubory</param>
        /// <returns>GTFS feed</returns>
        public static GtfsFeed DeserializeFeed(string folder)
        {
            return new GtfsFeed()
            {
                Agency = CsvFileSerializer.DeserializeFile<GtfsAgency>(Path.Combine(folder, "agency.txt")),
                Stops = CsvFileSerializer.DeserializeFile<GtfsStop>(Path.Combine(folder, "stops.txt")),
                Routes = CsvFileSerializer.DeserializeFile<GtfsRoute>(Path.Combine(folder, "routes.txt")),
                Trips = CsvFileSerializer.DeserializeFile<GtfsTrip>(Path.Combine(folder, "trips.txt")),
                StopTimes = CsvFileSerializer.DeserializeFile<GtfsStopTime>(Path.Combine(folder, "stop_times.txt")),
                Calendar = CsvFileSerializer.DeserializeFile<GtfsCalendarRecord>(Path.Combine(folder, "calendar.txt")),
                CalendarDates = CsvFileSerializer.DeserializeFile<GtfsCalendarDate>(Path.Combine(folder, "calendar_dates.txt")),
                FareRules = CsvFileSerializer.DeserializeFile<GtfsFareRule>(Path.Combine(folder, "fare_rules.txt")),
                Shapes = CsvFileSerializer.DeserializeFile<GtfsShapePoint>(Path.Combine(folder, "shapes.txt")),
                Transfers = CsvFileSerializer.DeserializeFile<GtfsTransfer>(Path.Combine(folder, "transfers.txt")),
                FeedInfo = CsvFileSerializer.DeserializeFile<GtfsFeedInfo>(Path.Combine(folder, "feed_info.txt")),

                RouteSubAgencies = CsvFileSerializer.DeserializeFile<RouteSubAgency>(Path.Combine(folder, "route_sub_agencies.txt")),
                // oběhy nenačítáme (zatím nejsou potřeba)
                // route stops nenačítáme (zatím nejsou potřeba)
            };
        }
    }
}
