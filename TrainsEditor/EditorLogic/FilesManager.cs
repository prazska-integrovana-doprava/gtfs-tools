using CzpttModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TrainsEditor.ExportModel;
using TrainsEditor.ViewModel;
using TrainsEditor.CommonLogic;
using TrainsEditor.CommonModel;

namespace TrainsEditor.EditorLogic
{
    /// <summary>
    /// Manager třída pro práci s XML soubory s vlaky
    /// </summary>
    static class FilesManager
    {
        public static readonly TrainGroupLoader TrainGroupLoader = new TrainGroupLoader();

        /// <summary>
        /// Načte data vlaků do ViewModelu editoru. Data načítá nejprve do CommonModelu (pomocí <see cref="CommonLogic.TrainGroupLoader"/> do <see cref="TrainGroupCollection"/>) 
        /// a z něho pak transformuje do ViewModelu.
        /// Během toho je schopen reportovat status a podporuje zrušení operace (kvůli kooperaci s UI).
        /// </summary>
        /// <param name="folder">Složka se soubory vlaků (všemi).</param>
        /// <param name="pastDataLimit">Datum a čas, před nímž nás již vlaky nezajímají. Vlaky, které podle své zadané bitmapy neplatí po tomto datu, nebudou načteny. Lze vložit null, pak jsou načteny všechny vlaky.</param>
        /// <param name="stationDatabase">Databáze zastávek PID pro určení zejména tarifních pásem a ASW ID</param>
        /// <param name="routeDatabase">Databáze linek PID pro určení dopravce a trasy</param>
        /// <param name="startDateForVisualCalendar">Počáteční datum pro vizuální kalendář (viz <see cref="CalendarVisualBitmap"/>)</param>
        /// <param name="endDateForVisualCalendar">Koncové datum pro vizuální kalendář (viz <see cref="CalendarVisualBitmap"/>)</param>
        /// <param name="reportCallback">Callback pro report stavu, který zároveň umožňuje zrušit operaci. Používáme callback <see cref="CommonLogic.TrainGroupLoader"/>u. Protože načítání dat probíhá ve dvou fázích (nejdřív načtení do <see cref="TrainGroupCollection"/> a pak transformace do ViewModelu) a první fáze probíhá uvnitř <see cref="CommonLogic.TrainGroupLoader"/>u, reportuje se navenek stav postupně od 0 do 100 v první fázi a pak znovu od 0 do 100 v druhé fázi (transformace). Zároveň fázi transformace nejde zrušit, dokončí se vždy se všemi soubory, které se načetly v první fázi.</param>
        /// <returns>ViewModely načtených vlaků</returns>
        public static IEnumerable<AbstractTrainFile> LoadTrainFiles(string folder, DateTime? pastDataLimit, StationDatabase stationDatabase, RouteDatabase routeDatabase,
            DateTime? startDateForVisualCalendar, DateTime? endDateForVisualCalendar, TrainGroupLoader.TrainsLoaderCallback reportCallback = null)
        {
            var groupsByTrId = TrainGroupLoader.LoadTrainFiles(folder, pastDataLimit, reportCallback);
            var loadedFiles = new List<AbstractTrainFile>();
            int nLoaded = 0;
            foreach (var group in groupsByTrId.OrderBy(gr => gr.MinimumNonzeroTrainNumber))
            {
                var transformedGroup = group.TrainFiles.Select(tr => TransformFile(tr, stationDatabase, routeDatabase)).ToArray();
                ResetOverwrittingAndOriginalTrains(transformedGroup);
                loadedFiles.AddRange(transformedGroup);
                reportCallback?.Invoke(++nLoaded, groupsByTrId.GroupCount, out bool shouldResume);
            }

            if (startDateForVisualCalendar.HasValue && endDateForVisualCalendar.HasValue)
            {
                foreach (var trainFile in loadedFiles)
                {
                    trainFile.VisualBitmap = new CalendarVisualBitmap(trainFile, startDateForVisualCalendar.Value, endDateForVisualCalendar.Value);
                }
            }

            return loadedFiles;
        }

        // Transformuje soubor z CommonModelu do ViewModelu
        private static AbstractTrainFile TransformFile(SingleTrainFile trainFile, StationDatabase stationDb, RouteDatabase routeDb)
        {
            if (trainFile.IsCancelation)
            {
                return new TrainCancelationFile(trainFile);
            }
            else
            {
                return new TrainFile(trainFile, stationDb, routeDb);
            }
        }

        /// <summary>
        /// Znovu načte soubor, ze kterého byl původně stvořen <paramref name="trainFile"/> (tzn. zahodí změny a přenačte daty z disku).
        /// </summary>
        /// <param name="trainFile">Soubor s daty vlaku (v něm je uložena i cesta na disku, ze kterého se data přenačtou)</param>
        /// <param name="stationDatabase">Databáze stanic pro určení zejména tarifních pásem a ASW ID.</param>
        /// <param name="routeDb">Databáze linek pro určení dopravců a tras</param>
        public static void ReloadFile(AbstractTrainFile trainFile, StationDatabase stationDatabase, RouteDatabase routeDb)
        {
            var reloadedFileData = TrainGroupLoader.ReloadFile(trainFile.FileData);
            trainFile.ResetData(reloadedFileData, stationDatabase, routeDb);

            if (reloadedFileData.OwnerGroup != trainFile.FileData.OwnerGroup)
            {
                // pokud jsme vlak vyjmuli ze skupiny, musíme to promítnout i do view modelu a obnovit bitmapy
                RemoveTrainFromGroup(trainFile);

                // ale stejně to neumíme, protože ho neumíme zařadit správně do té nové skupiny
                throw new InvalidOperationException("Cannot change train owner group.");
            }
            else if (reloadedFileData.CreationDateTime != trainFile.FileData.CreationDateTime)
            {
                // může se změnit pořadí
                ResetOverwrittingAndOriginalTrains(trainFile.AllTrainsInGroup.ToArray()); 
            }

            trainFile.RefreshVisualBitmapsForWholeGroup();
        }

        // Ve skupině již transformovaných vlaků je prováže pomocí vlastností OverwrittingTrains a OriginalTrains
        private static void ResetOverwrittingAndOriginalTrains(IEnumerable<AbstractTrainFile> transformedGroup)
        {
            foreach (var trainFile in transformedGroup)
            {
                trainFile.OverwrittingTrains = transformedGroup.Where(f => f.CreationDate > trainFile.CreationDate).OrderBy(f => f.CreationDate).ToList();
                trainFile.OriginalTrains = transformedGroup.Where(f => f.CreationDate < trainFile.CreationDate).OrderBy(f => f.CreationDate).ToList();
                if (trainFile is TrainCancelationFile cancelationFile)
                {
                    cancelationFile.CopyDataFromOriginalTrain();
                }
            }
        }

        /// <summary>
        /// Uloží data z ViewModelu do XML souboru vlaku
        /// </summary>
        /// <param name="train">Data vlaku (obsahují i cestu na disku, kam budou uložena)</param>
        public static void SaveFile(AbstractTrainFile train)
        {
            var streamWriter = new StreamWriter(train.FullPath);
            if (train is TrainFile trainFile)
            {
                var xmlSerializer = new XmlSerializer(typeof(CZPTTCISMessage));
                xmlSerializer.Serialize(streamWriter, trainFile.TrainData);
            }
            else if (train is TrainCancelationFile trainCancelation)
            {
                var xmlSerializer = new XmlSerializer(typeof(CZCanceledPTTMessage));
                xmlSerializer.Serialize(streamWriter, trainCancelation.TrainCancelationData);
            }
            else
            {
                throw new NotImplementedException();
            }

            train.ClearUnsavedChangesFlag();
            streamWriter.Close();
        }

        /// <summary>
        /// Smaže soubory vlaků
        /// </summary>
        /// <param name="trains">Vlaky</param>
        public static void DeleteFiles(params AbstractTrainFile[] trains)
        {
            foreach (var train in trains)
            {
                File.Delete(train.FullPath);
                RemoveTrainFromGroup(train);
            }
        }

        // Odstraní vlak ze skupiny - to může mít vliv i na kalendáře, což ohandluje CommonModel
        private static void RemoveTrainFromGroup(AbstractTrainFile train)
        {
            train.FileData.OwnerGroup.RemoveTrain(train.FileData);

            foreach (var otherTrain in train.OriginalTrains)
            {
                otherTrain.OverwrittingTrains.Remove(train);
            }

            foreach (var otherTrain in train.OverwrittingTrains)
            {
                otherTrain.OriginalTrains.Remove(train);
            }

            train.RefreshVisualBitmapsForWholeGroup();
        }

        /// <summary>
        /// Vytvoří duplikát souboru vlaku s bitmapou platnou v rozmezí od <paramref name="newStartDate"/> do <paramref name="newEndDate"/>.
        /// Funguje tak, že zkopíruje soubor na disku, ten standardním způsobem načte, zařadí do skupiny, ořeže start date a end date a znovu přepočítá kalendáře.
        /// </summary>
        /// <param name="train">Data vlaku, který má být zduplikován</param>
        /// <param name="newStartDate">Počátek platnosti duplikovaného vlaku</param>
        /// <param name="newEndDate">Konec platnosti duplikovaného vlaku</param>
        /// <param name="stationDatabase">Databáze stanic pro určení tarifních pásem a ASW ID</param>
        /// <param name="routeDatabase">Databáze linek pro určení dopravců, tras a ASW ID</param>
        /// <returns>Duplikovaný vlak</returns>
        public static AbstractTrainFile DuplicateFile(AbstractTrainFile train, DateTime newStartDate, DateTime newEndDate, StationDatabase stationDatabase, RouteDatabase routeDatabase)
        {
            var newFileName = Path.ChangeExtension(Path.ChangeExtension(train.FullPath, null) + $"_{DateTime.Now:yyyyMMdd_HHmmss}", Path.GetExtension(train.FullPath));
            File.Copy(train.FullPath, newFileName);
            var fileData = TrainGroupLoader.LoadAnotherFile(newFileName);
            // TODO TODO TODO
            var loadedFile = TransformFile(fileData, stationDatabase, routeDatabase);
            loadedFile.CreationDate = DateTime.Now;
            loadedFile.Calendar.ResizeBitmap(newStartDate, newEndDate, false);
            loadedFile.OriginalTrains = train.OriginalTrains.Union(new[] { train }).Union(train.OverwrittingTrains).ToList();
            loadedFile.OverwrittingTrains = new List<AbstractTrainFile>();
            SaveFile(loadedFile);

            loadedFile.VisualBitmap = new CalendarVisualBitmap(loadedFile, train.VisualBitmap.VisualStartDate, train.VisualBitmap.VisualEndDate);
            foreach (var originalTrain in loadedFile.OriginalTrains)
            {
                originalTrain.OverwrittingTrains.Add(loadedFile);
            }

            loadedFile.RefreshVisualBitmapsForWholeGroup();
            return loadedFile;
        }

        /// <summary>
        /// Vytvoří CANCEL soubor, který ruší daný vlak v období od <paramref name="newStartDate"/> do <paramref name="newEndDate"/>.
        /// Funguje tak, že si cancel záznam celý vyrobí, zařadí do skupiny a přepočítá kalendáře a pak ho uloží do souboru.
        /// </summary>
        /// <param name="train">Vlak, který má být zrušen</param>
        /// <param name="newStartDate">Počátek rušení</param>
        /// <param name="newEndDate">Konec rušení</param>
        /// <returns>Rušící záznam</returns>
        public static TrainCancelationFile CreateCancelFile(TrainFile train, DateTime newStartDate, DateTime newEndDate)
        {
            var newFileName = Path.Combine(Path.GetDirectoryName(train.FullPath), "cancel_" + Path.GetFileNameWithoutExtension(train.FullPath) + $"_{DateTime.Now:yyyyMMdd_HHmmss}" + Path.GetExtension(train.FullPath));
            if (newStartDate < train.Calendar.StartDate)
            {
                newStartDate = train.Calendar.StartDate;
            }

            var numDaysToSkipBeginning = (newStartDate - train.Calendar.StartDate).Days;
            var numDays = (newEndDate - newStartDate).Days + 1;
            var cancelData = new CZCanceledPTTMessage()
            {
                PlannedTransportIdentifiers = train.TrainData.Identifiers.OfType<PlannedTransportIdentifiers>().ToList(),
                CZPTTCancelation = DateTime.Now,
                PlannedCalendar = new PlannedCalendar()
                {
                    ValidityPeriod = new PlannedCalendar.ValidityPeriodType()
                    {
                        StartDateTime = newStartDate,
                        EndDateTime = newEndDate,
                    },
                    BitmapDays = new string(train.Calendar.CalendarData.BitmapDays.Skip(numDaysToSkipBeginning).Take(numDays).ToArray())
                }
            };

            var trainData = new SingleTrainFile(cancelData, newFileName);
            TrainGroupLoader.LoadedTrainGroups.AddTrain(trainData);
            trainData.OwnerGroup.ProcessCalendars();

            var cancelTrain = new TrainCancelationFile(trainData);
            ResetOverwrittingAndOriginalTrains(train.AllTrainsInGroup.Union(new[] { cancelTrain }));
            SaveFile(cancelTrain);
            cancelTrain.VisualBitmap = new CalendarVisualBitmap(cancelTrain, train.VisualBitmap.VisualStartDate, train.VisualBitmap.VisualEndDate);
            foreach (var originalTrain in cancelTrain.OriginalTrains)
            {
                originalTrain.OverwrittingTrains.Add(cancelTrain);
            }

            cancelTrain.RefreshVisualBitmapsForWholeGroup();
            return cancelTrain;
        }

    }
}
