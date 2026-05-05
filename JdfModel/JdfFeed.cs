using CommonLibrary;
using CsvSerializer;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace JdfModel
{
    public class JdfFeed
    {
        public Dictionary<int, Stop> Stops { get; private set; }

        public Dictionary<string, Agency> Agencies { get; private set; }

        public Dictionary<int, Route> Routes { get; private set; }

        public Dictionary<int, List<RouteExt>> RoutesExtendedData { get; private set; }

        public List<Trip> Trips { get; private set; }

        public Dictionary<string, FixedCode> FixedCodes { get; private set; }

        public List<TimeRemark> TimeRemarks { get; private set; }

        public List<RouteStop> RouteStops { get; private set; }

        public List<StopTime> StopTimes { get; private set; }

        public List<AlternativeAgency> AlternativeAgencies { get; private set; }

        public List<TimedTransfer> TimeTransfer { get; private set; }

        public static JdfFeed LoadFromDirectory(string path)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return new JdfFeed()
            {
                Stops = DeserializeFile<Stop>(path, "zastavky.txt").ToDictionary(s => s.StopId),
                Agencies = DeserializeFile<Agency>(path, "dopravci.txt").ToDictionary(a => a.Id),
                Routes = DeserializeFile<Route>(path, "linky.txt").ToDictionary(r => r.RouteId),
                RoutesExtendedData = DeserializeFile<RouteExt>(path, "linext.txt").GroupBy(r => r.RouteId).ToDictionary(r => r.Key, r => r.ToList()),
                Trips = DeserializeFile<Trip>(path, "spoje.txt"),
                FixedCodes = DeserializeFile<FixedCode>(path, "pevnykod.txt").ToDictionary(fc => fc.CodeId),
                TimeRemarks = DeserializeFile<TimeRemark>(path, "caskody.txt"),
                RouteStops = DeserializeFile<RouteStop>(path, "zaslinky.txt"),
                StopTimes = DeserializeFile<StopTime>(path, "zasspoje.txt"),
                AlternativeAgencies = DeserializeFile<AlternativeAgency>(path, "altdop.txt"),
                TimeTransfer = DeserializeFile<TimedTransfer>(path, "navaznosti.txt"),
            };
        }

        public static IEnumerable<(string path, JdfFeed feed)> LoadFromDirectoryRecursive(string path)
        {
            if (Directory.EnumerateFiles(path, "*.txt").Any2())
            {
                // musí být ve složce aspoň dva texťáky
                yield return (path, LoadFromDirectory(path));
            }

            var subfolders = Directory.GetDirectories(path).OrderBy(d => Path.GetFileName(d));
            foreach (var subdir in subfolders)
            {
                var entries = LoadFromDirectoryRecursive(subdir);
                foreach (var entry in entries)
                {
                    yield return entry;
                }
            }
        }

        public static JdfFeed LoadFromZipArchive(ZipArchive zipFile, string pathInArchive)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var entries = zipFile.Entries.Where(e => Path.GetDirectoryName(e.FullName) == pathInArchive).ToList();
            if (!entries.Any2())
            {
                // prázdná složka
                return null;
            }

            return new JdfFeed()
            {
                Stops = DeserializeFileInArchive<Stop>(entries, "zastavky.txt").ToDictionary(s => s.StopId),
                Agencies = DeserializeFileInArchive<Agency>(entries, "dopravci.txt").ToDictionary(a => a.Id),
                Routes = DeserializeFileInArchive<Route>(entries, "linky.txt").ToDictionary(r => r.RouteId),
                RoutesExtendedData = DeserializeFileInArchive<RouteExt>(entries, "linext.txt").GroupBy(r => r.RouteId).ToDictionary(r => r.Key, r => r.ToList()),
                Trips = DeserializeFileInArchive<Trip>(entries, "spoje.txt"),
                FixedCodes = DeserializeFileInArchive<FixedCode>(entries, "pevnykod.txt").ToDictionary(fc => fc.CodeId),
                TimeRemarks = DeserializeFileInArchive<TimeRemark>(entries, "caskody.txt"),
                RouteStops = DeserializeFileInArchive<RouteStop>(entries, "zaslinky.txt"),
                StopTimes = DeserializeFileInArchive<StopTime>(entries, "zasspoje.txt"),
                AlternativeAgencies = DeserializeFileInArchive<AlternativeAgency>(entries, "altdop.txt"),
                TimeTransfer = DeserializeFileInArchive<TimedTransfer>(entries, "navaznosti.txt"),
            };
        }

        public static IEnumerable<(string path, JdfFeed feed)> LoadFromZipArchiveRecursive(string zipFilePath)
        {
            var zip = ZipFile.OpenRead(zipFilePath);
            var folders = zip.Entries.Select(e => Path.GetDirectoryName(e.FullName) ?? "").Distinct();
            foreach (var folder in folders)
            {
                var jdfFeed = LoadFromZipArchive(zip, folder);
                if (jdfFeed != null)
                {
                    yield return (zipFilePath + "\\" + folder, jdfFeed);
                }
            }
        }

        private static List<T> DeserializeFile<T>(string path, string fileName) where T: new()
        {
            var encoding = Encoding.GetEncoding(1250);
            return CsvFileSerializer.DeserializeFile<T>(Path.Combine(path, fileName), ',', null, "ddMMyyyy", encoding, false, ";");
        }

        private static List<T> DeserializeFileInArchive<T>(List<ZipArchiveEntry> entries, string fileName) where T: new()
        {
            var entry = entries.FirstOrDefault(e => Path.GetFileName(e.FullName).Equals(fileName, System.StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                var stream = entry.Open();
                var reader = new StreamReader(stream, Encoding.GetEncoding(1250));
                return CsvFileSerializer.Deserialize<T>(reader, ',', null, "ddMMyyyy", false, ";");
            }
            else
            {
                return new List<T>();
            }
        }
    }
}
