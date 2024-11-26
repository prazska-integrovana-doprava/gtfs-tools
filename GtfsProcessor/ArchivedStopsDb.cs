using CsvSerializer;
using GtfsLogging;
using GtfsModel;
using GtfsModel.Extended;
using GtfsProcessor.DataClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GtfsProcessor
{
    class ArchivedStopsDb
    {
        private DateTime _globalStartDate;
        private string _stopsDbFolder;
        private ISimpleLogger _log;

        public List<GtfsStop> NonTemporaryStopsToBeArchived { get; private set; }

        public List<ArchivedGtfsStop> ArchivedStops { get; private set; }

        public ArchivedStopsDb(DateTime globalStartDate, string stopsDbFolder, ISimpleLogger log)
        {
            _globalStartDate = globalStartDate;
            _stopsDbFolder = stopsDbFolder;
            _log = log;
            NonTemporaryStopsToBeArchived = new List<GtfsStop>();
        }

        public void AddStopToBeArchived(GtfsStop stop)
        {
            if (stop.Id.Contains('_'))
            {
                // bereme pouze první verze zastávek, jinak by to zlobilo
                return;
            }

            NonTemporaryStopsToBeArchived.Add(stop);
        }

        public void AddStopToBeArchived(Stop stop)
        {
            AddStopToBeArchived(stop.ToGtfsStop());
        }

        public void AddMultipleStopsToBeArchived(IEnumerable<GtfsStop> stops)
        {
            foreach (var stop in stops)
            {
                AddStopToBeArchived(stop);
            }
        }

        public void AddMultipleStopsToBeArchived(IEnumerable<Stop> stops)
        {
            AddMultipleStopsToBeArchived(stops.Select(s => s.ToGtfsStop()));
        }
        
        public void LoadArchivedStops()
        {
            for (int i = 0; i < 30; i++)
            {
                var fileName = ConstructFileName(_globalStartDate.AddDays(-i));
                if (File.Exists(fileName) && new FileInfo(fileName).Length > 0)
                {
                    ArchivedStops = CsvFileSerializer.DeserializeFile<ArchivedGtfsStop>(fileName);
                }
            }

            if (ArchivedStops == null)
            {
                ArchivedStops = new List<ArchivedGtfsStop>();
            }

            var toRemove = ArchivedStops.Where(s => s.ArchivedUntil < _globalStartDate).ToArray();
            foreach (var stop in toRemove)
            {
                _log.Log($"Zastávka {stop} odstraněna z archivu.");
                ArchivedStops.Remove(stop);
            }
        }

        public List<GtfsStop> SelectArchivedStopsToAdd(IEnumerable<string> presentGtfsIds)
        {
            var idset = new HashSet<string>(presentGtfsIds);
            var result = new List<GtfsStop>();
            foreach(var stop in ArchivedStops)
            {
                if (!idset.Contains(stop.Id))
                {
                    _log.Log($"Zastávka {stop} přidána z archivu do stops.txt");
                    result.Add(stop);
                }
            }

            return result;
        }

        public void AddCurrentStopsToArchive()
        {
            var x = ArchivedStops.Where(as1 => ArchivedStops.Any(as2 => as1 != as2 && as1.Id == as2.Id)).ToList();
            var archiveDictionary = ArchivedStops.ToDictionary(s => s.Id);
            foreach (var stopToArchive in NonTemporaryStopsToBeArchived)
            {
                var archiveUntil = _globalStartDate.AddDays(30);
                var stopToArchiveIdBase = GetGtfsIdBase(stopToArchive);
                if (archiveDictionary.ContainsKey(stopToArchiveIdBase) && archiveDictionary[stopToArchiveIdBase].ArchivedUntil > archiveUntil)
                {
                    // někdo ručně nastavil vyšší konec archivace, tak ho použijeme místo výchozí hodnoty
                    archiveUntil = archiveDictionary[stopToArchiveIdBase].ArchivedUntil;
                }

                // přepisujeme i stávající hodnoty, abychom aktualizovali data o zastávce na aktuální údaje
                archiveDictionary[stopToArchiveIdBase] = new ArchivedGtfsStop(stopToArchive, archiveUntil);
            }

            ArchivedStops = archiveDictionary.Values.ToList();
        }

        public void SaveArchivedStops()
        {
            var fileName = ConstructFileName(_globalStartDate);
            CsvFileSerializer.SerializeFile(fileName, ArchivedStops);
        }

        private string ConstructFileName(DateTime date)
        {
            return Path.Combine(_stopsDbFolder, $"stops_db_{date:yyMMdd}.txt");
        }

        private string GetGtfsIdBase(GtfsStop stop)
        {
            return stop.Id.Split('_').First();
        }
    }
}
