using CommonLibrary;
using CsvSerializer;
using GtfsLogging;
using GtfsModel;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using GtfsModel.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrainsEditor.CommonLogic;
using TrainsEditor.CommonModel;
using TrainsEditor.ExportModel;

namespace TrainsEditor.GtfsExport
{
    /// <summary>
    /// Modul pro transformaci vlaků z modelu <see cref="TrainGroupCollection"/> do modelu GTFS. Umí buď transformovat celou složku, nebo vybranou množinu souborů.
    /// </summary>
    class GtfsExportModule
    {
        /// <summary>
        /// Datum, ke kterému feed generujeme (lze z něj odvodit například že nás nezajímají starší údaje, tj. vlaky, které jedou pouze před tímto datem).
        /// Počítá se z aktuálního data a času, do 3:00 je to včerejšek, od 3:00 dnešek.
        /// </summary>
        public readonly DateTime ReferenceStartDate = DateTime.Now.AddHours(-3).Date;

        // Při otáčení směru porovnáváme vždy s nějakým "vzorovým" tripem. Ne vždy je jeho určení snadné a občas se vybírá trip vyloženě debilní,
        // zde je možné nastavit jej pro linku natvrdo (viz metoda AdjustDirections)
        private List<TripDirectionSpec> RepresentativeTrips;

        // Složka s XML soubory vlaků
        private readonly string _trainFilesPath;

        /// <summary>
        /// ID dopravce "PID" v GTFS
        /// </summary>
        public const int PidAgencyId = 99;

        private TrainGroupLoader _trainGroupLoader;

        private readonly StationDatabase _stopDatabase;
        private readonly RouteDatabase _routeDatabase;
        private readonly ICommonLogger loaderLog = Loggers.TrainsLoaderLoggerInstance;
        private readonly ISimpleLogger processLog = Loggers.TrainsProcessLoggerInstance;
        private readonly ISimpleLogger outputLog = Loggers.TrainsOutputLoggerInstance;

        public GtfsExportModule(StationDatabase stationDb, RouteDatabase routeDb, List<TripDirectionSpec> representativeTrips, string trainsFilesPath, TrainGroupLoader trainGroupLoader)
        {
            _stopDatabase = stationDb;
            _routeDatabase = routeDb;
            _trainFilesPath = trainsFilesPath;
            _trainGroupLoader = trainGroupLoader;
            RepresentativeTrips = representativeTrips;
        }

        public GtfsExportModule(StationDatabase stationDb, RouteDatabase routeDb, List<TripDirectionSpec> representativeTrips, string trainFilesPath, TrainGroupLoader trainGroupLoader, DateTime referenceStartDate)
            : this(stationDb, routeDb, representativeTrips, trainFilesPath, trainGroupLoader)
        {
            ReferenceStartDate = referenceStartDate;
        }

        public GtfsExportModule(StationDatabase stationDb, RouteDatabase routeDb, List<TripDirectionSpec> representativeTrips, string trainFilesPath, TrainGroupLoader trainGroupLoader,
            ICommonLogger loaderLog, ISimpleLogger processLog, ISimpleLogger outputLog)
            : this(stationDb, routeDb, representativeTrips, trainFilesPath, trainGroupLoader)
        {
            this.loaderLog = loaderLog;
            this.processLog = processLog;
            this.outputLog = outputLog;
        }

        public GtfsExportModule(StationDatabase stationDb, RouteDatabase routeDb, List<TripDirectionSpec> representativeTrips, string trainFilesPath, TrainGroupLoader trainGroupLoader, DateTime referenceStartDate,
        ICommonLogger loaderLog, ISimpleLogger processLog, ISimpleLogger outputLog)
            : this(stationDb, routeDb, representativeTrips, trainFilesPath, trainGroupLoader, loaderLog, processLog, outputLog)
        {
            ReferenceStartDate = referenceStartDate;
        }

        /// <summary>
        /// Spustí generování GTFS ze složky. Načte data do modelu <see cref="TrainGroupCollection"/> a následně jej transformuje do GTFS.
        /// </summary>
        /// <param name="mapNetworkFileName">Soubor se sítí vlaků (pro trasy)</param>
        /// <param name="outputFolder">Složka, kam mají být uloženy GTFS soubory</param>
        /// <param name="logPath">Složka, kam se budou ukládat logy</param>
        /// <param name="reportCallback">Callback, kam se hlásí progress a může zrušit načítání dat</param>
        public bool Run(string mapNetworkFileName, string outputFolder, string logPath, TextWriter console, TrainGroupLoader.TrainsLoaderCallback reportCallback = null)
        {
            var calendarConstructor = new CalendarConstructor(ReferenceStartDate);
            console.WriteLine("Načítání vlaků...");
            var trainsByTrId = LoadTrainsFromFolder(_trainFilesPath, reportCallback);

            bool shouldResume = true;
            reportCallback?.Invoke(1, 1, out shouldResume);
            if (!shouldResume)
            {
                console.WriteLine("PŘERUŠENO");
                return false;
            }

            console.WriteLine("Zpracování, tvorba kalendářů, opravy směrů...");
            var trainTrips = TransformTrains(trainsByTrId, calendarConstructor, console);

            reportCallback?.Invoke(1, 1, out shouldResume);
            if (!shouldResume)
            {
                console.WriteLine("PŘERUŠENO");
                return false;
            }

            console.WriteLine("Výpis vlaků do logu...");
            var tripsByTrainId = trainTrips.GroupBy(tt => tt.WholeTrain.TrIdCompanyAndCoreAndYear);
            foreach (var trIdAndTrains in tripsByTrainId)
            {
                outputLog.Log($"{trIdAndTrains.Key}:");
                foreach (var train in trIdAndTrains)
                {
                    outputLog.Log(VerboseDescriptor.DescribeTrip(train));
                    outputLog.Log("");
                }

                outputLog.Log("---------------------------------------------------------------------");
                outputLog.Log("");
            }

            reportCallback?.Invoke(1, 1, out shouldResume);
            if (!shouldResume)
            {
                console.WriteLine("PŘERUŠENO");
                return false;
            }

            console.WriteLine("Tvorba tras...");
            var shapeConstructor = new ShapeConstructor();
            shapeConstructor.LoadPointData(mapNetworkFileName);
            var shapeDatabase = new ShapeDatabase(shapeConstructor, _stopDatabase.UsedStops, loaderLog);
            foreach (var trainTrip in trainTrips)
            {
                shapeDatabase.SetShapeAndDistTraveled(trainTrip);
            }

            var calendars = calendarConstructor.GetAllCalendars();

            reportCallback?.Invoke(1, 1, out shouldResume);
            if (!shouldResume)
            {
                console.WriteLine("PŘERUŠENO");
                return false;
            }

            // TODO a na to si udělat nějaký obecnější framework? stejně tak na ty .IsUsed a možná na celou tu "Feed databázi"
            console.WriteLine("Ukládání dat...");
            var feed = new GtfsFeed()
            {
                Agency = new List<GtfsAgency>() { CreateAgencyInstance() },
                Calendar = calendars.Select(cal => cal.ToGtfsCalendar()).ToList(),
                CalendarDates = calendars.SelectMany(cal => cal.GetAllGtfsExceptions()).ToList(),
                Routes = _routeDatabase.UsedLines.Select(l => l.ToGtfsRoute(PidAgencyId)).ToList(),
                Stops = _stopDatabase.UsedStops.Select(s => s.ToGtfsStop()).Distinct().ToList(),
                StopTimes = trainTrips.SelectMany(t => t.GetGtfsStopTimes()).ToList(),
                Trips = trainTrips.Select(t => t.ToGtfsTrip()).ToList(),
                Shapes = shapeDatabase.Shapes.SelectMany(s => s.ToGtfsShape()).ToList(),
                RouteSubAgencies = _routeDatabase.UsedLines.SelectMany(l => l.SubAgencies).ToList(),
            };

            GtfsFeedSerializer.SerializeFeed(outputFolder, feed);

            // TODO potřeba aktualizovaný číselník SR70

            console.WriteLine("DOKONČENO");
            return true;
        }

        /// <summary>
        /// Transformuje načtené vlaky do GTFS struktury
        /// </summary>
        /// <param name="trainGroupCollection">Načtené vlaky po skupinách</param>
        /// <param name="calendarConstructor">Instance <see cref="CalendarConstructor"/> k vytvoření kalendářů.</param>
        /// <returns>GTFS reprezentace zadaných vlaků</returns>
        public List<TrainTrip> TransformTrains(TrainGroupCollection trainGroupCollection, CalendarConstructor calendarConstructor, TextWriter console)
        {
            var trains = new List<Train>();
            foreach (var group in trainGroupCollection)
            {
                var merged = TransformTrainGroup(group, console);
                trains.AddRange(merged);
            }

            // jen kontrola a hlášení protičasů
            foreach (var train in trains.Where(t => t.HasInvalidTimes))
            {
                console.WriteLine($" - POZOR: Vlak {train} ({string.Join(" / ", train.GetTrainNumbersUnique())} od {train.StartDate:dd.MM.yyyy}) obsahuje protičas v {train.FirstInvalidTime}");
            }

            var trainTrips = trains.OrderBy(t => t.LineTrips.Min(tr => tr.TrainNumber)).SelectMany(t => t.LineTrips).ToList();

            var tripIdSet = new HashSet<string>();
            foreach (var trainTrip in trainTrips)
            {
                // vytvořit kalendář a označit linku a zastávky referencované tripem
                trainTrip.GtfsId = $"{trainTrip.Route.AswId}_{trainTrip.TrainNumber}_{trainTrip.StartDate:yyMMdd}";
                while (!tripIdSet.Add(trainTrip.GtfsId))
                {
                    loaderLog.Log(LogMessageType.WARNING_TRAIN_DUPLICATE_ID, $"Duplicitní ID: {trainTrip.GtfsId}, nastavuji alternativní připojením 'X'. Příčinou může být, že vlak je rozdělen náhradní dopravou na dva samostatné kusy.");
                    trainTrip.GtfsId += 'X';
                }

                trainTrip.BlockId = IdentifierManagement.GenerateBlockId(trainTrip);
                trainTrip.CalendarRecord = calendarConstructor.GetCalendarFor(trainTrip.StartDate, trainTrip.ServiceBitmap);
                trainTrip.Route.Trips.Add(trainTrip);
                foreach (var stopTime in trainTrip.StopTimes)
                {
                    ((TrainStop)stopTime.Stop).IsUsed = true;
                }
            }

            foreach (var trainLine in _routeDatabase.Lines.Values)
                AdjustDirections(trainLine.Trips.Select(t => (TrainTrip)t), loaderLog, console);

            return trainTrips;
        }

        private TrainGroupCollection LoadTrainsFromFolder(string folder, TrainGroupLoader.TrainsLoaderCallback reportCallback)
        {
            return _trainGroupLoader.LoadTrainFiles(folder, DateTime.Now.AddHours(-3).Date, reportCallback);
        }


        private List<Train> TransformTrainGroup(TrainGroup group, TextWriter console)
        {
            var compareAndMerge = new TrainVariantMerge(processLog);
            var transformedGroup = group.TrainFiles.Where(tr => !tr.IsCancelation).Select(
                tr => Train.Create(tr, _stopDatabase, _routeDatabase, loaderLog, processLog, stt => CheckTrainPrevVersionLineInfo(stt, tr.OverwrittenTrains, console))
            );

            // provedeme znovu filtering na datum, protože u některých vlaků jsme mohli posunout EndDate na základě přepsaného konce bitmapy
            var transformedGroupFiltered = transformedGroup.Where(tr => tr != null && !tr.ServiceBitmap.IsEmpty && tr.EndDate >= ReferenceStartDate).ToArray();

            return transformedGroupFiltered.MergeIdentical(compareAndMerge).ToList(); // je důležité, aby se ta filtrovaná skupina enumerovala jen jednou, protože při slučování se upravují záznamy a opakovaný merge by způsoboval chyby
        }

        // když se narazí na vlak s nevyplněnou linkou, podíváme se, jestli jeho předchůdce ji vyplněnou měl a pokud ano, nahlásíme to jako podezřelé
        private void CheckTrainPrevVersionLineInfo(List<StationTime> stationTimes, List<SingleTrainFile> overwrittenTrains, TextWriter console)
        {
            int n = 0;
            foreach (var stationTime in stationTimes)
            {
                foreach (var train in overwrittenTrains)
                {
                    var stationTimeInTrain = train.TrainData?.CZPTTInformation?.CZPTTLocation?.FirstOrDefault(loc => loc.Location.LocationPrimaryCode == stationTime.StationCode);
                    if (stationTimeInTrain != null && stationTimeInTrain.GetLineInfo() != TrainLineInfo.UndefinedLineInfoInstance)
                    {
                        n++;
                        if (n >= 2)
                        {
                            console.WriteLine($" - POZOR: Vlak {stationTime.TrainTypeOnDeparture} {stationTime.TrainNumberOnDeparture} nemá nastavenu linku v úseku {stationTimes.First().StationName} - {stationTimes.Last().StationName}, ale jeho předchůdce, kterého přepisuje, ano. Vlak je ignorován.");
                            return;
                        }
                    }
                }
            }
        }

        private void AdjustDirections(IEnumerable<TrainTrip> trips, ICommonLogger log, TextWriter console)
        {
            if (!trips.Any())
                return;

            // vybereme zástupce jako spoj s co nejvíce zastávkami
            var representativeTripsForDirection = new Dictionary<Direction, TrainTrip>()
            {
                { Direction.Inbound, GetRepresentativeTrip(trips, Direction.Inbound) },
                { Direction.Outbound, GetRepresentativeTrip(trips, Direction.Outbound) },
            };

            // po jednom porovnáváme posloupnosti zastávek a pokud jsou tam nějaké záporné pohyby, reportujeme je
            foreach (var trip in trips)
            {
                var representativeTrip = representativeTripsForDirection[trip.DirectionId];

                int positiveMoves = 0, negativeMoves = 0;
                Stop negativeMoveFrom = null, negativeMoveTo = null;
                for (int i = 1; i < trip.StopTimes.Count; i++)
                {
                    var fromStop = trip.StopTimes[i - 1].Stop;
                    var toStop = trip.StopTimes[i].Stop;
                    var fromStopOnRepresentative = representativeTrip.StopTimes.LastOrDefault(st => st.Stop == fromStop);
                    var toStopOnRepresentative = representativeTrip.StopTimes.FirstOrDefault(st => st.Stop == toStop);
                    if (fromStopOnRepresentative != null && toStopOnRepresentative != null)
                    {
                        var indexOfPrev = representativeTrip.StopTimes.IndexOf(fromStopOnRepresentative);
                        var indexOfCurrent = representativeTrip.StopTimes.IndexOf(toStopOnRepresentative);
                        if (indexOfPrev < indexOfCurrent)
                        {
                            // koresponduje se směrem
                            positiveMoves++;
                        }
                        else if (indexOfPrev > indexOfCurrent)
                        {
                            // v representative tripu je opačné pořadí zastávek
                            negativeMoves++;
                            if (negativeMoveFrom == null)
                            {
                                negativeMoveFrom = fromStop;
                                negativeMoveTo = toStop;
                            }
                        }
                    }
                }

                if (negativeMoves > positiveMoves)
                {
                    var msg = $"Vlak {trip.ToStringEx()} linky {trip.Route.ShortName} má většinu zastávek v opačném pořadí než vlak {representativeTrip.ToStringEx()}, otáčím směr.";
                    log.Log(LogMessageType.INFO_TRAIN_ONE_OPPOSITE_STOP_DIRECTION, msg);
                    console.WriteLine(" - INFO: " + msg);
                    trip.RevertDirection();
                }
                else if (negativeMoves > 0)
                {
                    var msg = $"Vlak {trip.ToStringEx()} linky {trip.Route.ShortName} má zastávky {negativeMoveFrom.Name} a {negativeMoveTo.Name} v opačném pořadí, než vlak {representativeTrip.ToStringEx()}.";
                    log.Log(LogMessageType.INFO_TRAIN_ONE_OPPOSITE_STOP_DIRECTION, msg);
                    console.WriteLine(" - POZOR: " + msg);
                }
            }
        }

        // triviálně vrací vlak zprostředka seznamu vlaků daného směru
        private TrainTrip GetRepresentativeTrip(IEnumerable<TrainTrip> trips, Direction direction)
        {
            var tripsOfDirection = trips.Where(t => t.DirectionId == direction).ToArray();
            if (tripsOfDirection.Length == 0)
            {
                return null;
            }
            else
            {
                // snaží se najít co nejdelší vlak; pokud má daného reprezentanta, hledá podle čísla a bere nejdelší variantu, jinak hledá nejdelší vlak v celém směru linky
                var givenRepresentatives = RepresentativeTrips.Where(t => t.LineName == tripsOfDirection.First().Route.ShortName).Select(r => r.TrainNumber);
                var representativeInDirection = tripsOfDirection.Where(t => givenRepresentatives.Contains(t.TrainNumber));
                if (!representativeInDirection.Any())
                {
                    representativeInDirection = tripsOfDirection;
                }

                return representativeInDirection.OrderByDescending(t => t.StopTimes.Count).First();
            }

            //if (!tripsOfDirection.Any())
            //    return null;

            //var maxStopCount = tripsOfDirection.Max(t => t.StopTimes.Count);
            //return tripsOfDirection.FirstOrDefault(t => t.StopTimes.Count == maxStopCount);
        }

        private static GtfsAgency CreateAgencyInstance()
        {
            return new GtfsAgency()
            {
                Id = PidAgencyId,
                Name = "Pražská integrovaná doprava - vlaky",
                Lang = "cs",
                Phone = "+420234704560",
                Timezone = "Europe/Prague",
                Url = "https://pid.cz",
            };
        }
    }
}
