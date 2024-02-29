using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;

namespace TrainsEditor.CommonModel
{
    /// <summary>
    /// Drží data o všech vlacích rozdělených po skupinách. O skupinách viz <see cref="TrainGroup"/>.
    /// </summary>
    class TrainGroupCollection : IEnumerable<TrainGroup>
    {
        // indexováno přes TrIdCompanyAndCoreAndYear
        private readonly Dictionary<string, TrainGroup> _trainGroups;

        /// <summary>
        /// Počet skupin vlaků
        /// </summary>
        public int GroupCount => _trainGroups.Count;

        /// <summary>
        /// Vrátí data všechn souborů ze všech skupin.
        /// </summary>
        public IEnumerable<SingleTrainFile> AllTrainFiles => _trainGroups.Values.SelectMany(group => group.TrainFiles);

        public TrainGroupCollection()
        {
            _trainGroups = new Dictionary<string, TrainGroup>();
        }

        /// <summary>
        /// Vytvoří instanci na základě dat vlaků. Shodný výsledek s postupným voláním <see cref="AddTrain"/>.
        /// </summary>
        /// <param name="trainFiles">Data vlaků</param>
        public static TrainGroupCollection FromTrainCollection(IEnumerable<SingleTrainFile> trainFiles)
        {
            var result = new TrainGroupCollection();
            foreach (var train in trainFiles)
            {
                result.AddTrain(train);
            }

            return result;
        }

        /// <summary>
        /// Zařadí vlak do správné skupiny (případně skupinu vytvoří, pokud ještě neexistuje).
        /// </summary>
        /// <param name="trainFile"></param>
        public void AddTrain(SingleTrainFile trainFile)
        {
            var ownerGroup = _trainGroups.GetValueAndAddIfMissing(trainFile.TrIdCompanyAndCoreAndYear, new TrainGroup());
            ownerGroup.AddTrain(trainFile);
        }

        /// <summary>
        /// Nastaví všem vlakům ve všech skupinách hodnotu <see cref="SingleTrainFile.BitmapEx"/>.
        /// </summary>
        public void ProcessCalendars()
        {
            foreach (var trainGroup in _trainGroups.Values)
            {
                trainGroup.ProcessCalendars();
            }
        }

        public IEnumerator<TrainGroup> GetEnumerator()
        {
            return _trainGroups.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _trainGroups.Values.GetEnumerator();
        }
    }
}
