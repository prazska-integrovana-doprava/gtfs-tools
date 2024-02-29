using AswModel.Extended.Logging;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;
using System.Collections.Generic;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává záznamy o obězích <see cref="Obeh"/> a ukládá je do databáze <see cref="AswSingleFileFeed.Trips"/>.
    /// 
    /// Počítá se s tím, že spoje ještě nejsou načtené, naopak při načítání spojů se dohledávají oběhy
    /// </summary>
    class RunProcessor : IProcessor<Obeh>
    {
        private AswSingleFileFeed feedFile;
        private TheAswDatabase db;
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;

        /// <summary>
        /// Spoje si z toho pak najdou správný oběh
        /// </summary>
        public IDictionary<int, List<RunDescriptor>> RunByTripId { get; private set; }

        public RunProcessor(AswSingleFileFeed feedFile, TheAswDatabase db)
        {
            this.feedFile = feedFile;
            this.db = db;
            RunByTripId = new Dictionary<int, List<RunDescriptor>>();
        }

        public void Process(Obeh xmlRun)
        {
            var result = new RunDescriptor()
            {
                RootLineNumber = xmlRun.CLinky,
                RunNumber = xmlRun.Poradi,
                ServiceAsBits = ServiceDaysBitmap.FromBitmapString(xmlRun.KJ),
                TripIds = xmlRun.SpojID,
                ConnectedTrips = new List<List<int>>()
            };

            foreach (var connectedTripsRecord in xmlRun.DlouheSpoje)
            {
                result.ConnectedTrips.Add(connectedTripsRecord.SpojID);
            }

            foreach (var tripId in result.TripIds)
            {
                RunByTripId.GetValueAndAddIfMissing(tripId, new List<RunDescriptor>()).Add(result);
            }

            feedFile.Trips.AddTripSequence(result);
        }
    }
}
