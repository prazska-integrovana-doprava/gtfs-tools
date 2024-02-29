using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using CzpttModel;
using TrainsEditor.CommonModel;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Načítá vlaky z XML souborů do skupin. O skupinách viz <see cref="TrainGroup"/>.
    /// </summary>
    class TrainGroupLoader
    {
        private readonly XmlSerializer xmlSerializer = new XmlSerializer(typeof(CZPTTCISMessage));
        private readonly XmlSerializer xmlCancelSerializer = new XmlSerializer(typeof(CZCanceledPTTMessage));

        private Dictionary<string, SingleTrainFile> _loadedFilesCache = new Dictionary<string, SingleTrainFile>();

        /// <summary>
        /// Callback pro <see cref="LoadTrainFiles" />. Reportuje status a umožňuje přerušit načítací proces.
        /// </summary>
        /// <param name="numberOfFilesLoaded">Počet načtených souborů</param>
        /// <param name="totalNumberOfFiles">Celkový počet souborů k načtení</param>
        /// <param name="shouldResume">Nastavit true, pokud se má pokračovat; false ukončí operaci a metoda vrátí, co bylo dosud načteno.</param>
        public delegate void TrainsLoaderCallback(int numberOfFilesLoaded, int totalNumberOfFiles, out bool shouldResume);

        /// <summary>
        /// Po zavolání <see cref="LoadTrainFiles"/> obsahuje načtené soubory. S opětovným voláním se resetuje.
        /// </summary>
        public TrainGroupCollection LoadedTrainGroups { get; private set; }

        /// <summary>
        /// Načte vlaky z XML souborů v dané složce (a podsložkách).
        /// </summary>
        /// <param name="folder">Složka s XML soubory</param>
        /// <param name="pastDataLimit">Je-li zadáno, vlaky, jejichž platnost končí před tímto dnem, budou ignorovány</param>
        /// <param name="reportCallback">Callback, kam se hlásí progress a může zrušit načítání (při zrušení je vrácena ta část souborů, které již byly načteny).</param>
        /// <returns>Načtené vlaky rozdělené po skupinách.</returns>
        public TrainGroupCollection LoadTrainFiles(string folder, DateTime? pastDataLimit, TrainsLoaderCallback reportCallback = null)
        {
            var fileList = Directory.EnumerateDirectories(folder).SelectMany(dir => Directory.EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly)).ToArray();

            var result = new TrainGroupCollection();
            bool shouldResume = true;
            reportCallback?.Invoke(0, fileList.Length, out shouldResume);
            if (!shouldResume)
                return result;

            int nLoaded = 0;
            foreach (var file in fileList)
            {
                var trainFile = LoadFileInternal(file);

                if (!pastDataLimit.HasValue || trainFile.EndDate >= pastDataLimit.Value.Date)
                {
                    // přidáváme pouze pokud už není neplatný (v případě, kdy caller zadal limit na platnost)
                    result.AddTrain(trainFile);
                }
            
                reportCallback?.Invoke(++nLoaded, fileList.Length, out shouldResume);
                if (!shouldResume)
                    break;
            }

            result.ProcessCalendars();
            LoadedTrainGroups = result;
            return result;
        }

        /// <summary>
        /// Přenačte soubor z disku (obnoví i dotčené skupiny souborů včetně jejich kalendářů)
        /// </summary>
        /// <param name="trainFile">Soubor k přenačtení</param>
        /// <returns>Přenačtený soubor</returns>
        public SingleTrainFile ReloadFile(SingleTrainFile trainFile)
        {
            // chirurgicky odstraníme z původní skupiny
            trainFile.OwnerGroup.RemoveTrain(trainFile);
            trainFile.OwnerGroup.ProcessCalendars();

            // načteme znovu a umístíme do skupiny
            var reloadedTrain = LoadFileInternal(trainFile.FileFullPath);
            LoadedTrainGroups.AddTrain(reloadedTrain);
            reloadedTrain.OwnerGroup.ProcessCalendars();

            return reloadedTrain;
        }

        /// <summary>
        /// Načte a zařadí nový soubor (obnoví i dotčenou skupinu souborů)
        /// </summary>
        /// <param name="fullPath">Cesta k souboru</param>
        /// <returns>Přenačtený soubor</returns>
        public SingleTrainFile LoadAnotherFile(string fullPath)
        {
            // načteme znovu a umístíme do skupiny
            var loadedTrain = LoadFileInternal(fullPath);
            LoadedTrainGroups.AddTrain(loadedTrain);
            loadedTrain.OwnerGroup.ProcessCalendars();
            return loadedTrain;
        }

        private SingleTrainFile LoadFileInternal(string fullPath)
        {
            var fileLastModifiedTime = File.GetLastWriteTime(fullPath);
            if (_loadedFilesCache.TryGetValue(fullPath, out SingleTrainFile cachedFile))
            {
                if (fileLastModifiedTime == cachedFile.FileLastModifiedTime)
                {
                    return cachedFile;
                }
            }

            SingleTrainFile result = null;
            using (var xmlReader = XmlReader.Create(fullPath))
            {
                var fileName = Path.GetFileName(fullPath);
                if (fileName.StartsWith("cancel"))
                {
                    var czpttCanceledMessage = (CZCanceledPTTMessage)xmlCancelSerializer.Deserialize(xmlReader);
                    result = new SingleTrainFile(czpttCanceledMessage, fullPath);
                }
                else
                {
                    var czpttMessage = (CZPTTCISMessage)xmlSerializer.Deserialize(xmlReader);
                    result = new SingleTrainFile(czpttMessage, fullPath);
                }
            }

            result.FileLastModifiedTime = fileLastModifiedTime;
            if (result != null)
            {
                _loadedFilesCache[fullPath] = result;
            }

            return result;
        }
    }
}
