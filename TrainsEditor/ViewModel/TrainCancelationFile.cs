using CzpttModel;
using System;
using System.Linq;
using System.Windows.Media;
using System.Collections.ObjectModel;
using TrainsEditor.CommonModel;

namespace TrainsEditor.ViewModel
{
    /// <summary>
    /// Jeden rušící soubor vlaku, reprezentuje informaci, že daný vlak je v dané dny zrušen (<see cref="CZCanceledPTTMessage"/>)
    /// </summary>
    class TrainCancelationFile : AbstractTrainFile
    {
        /// <summary>
        /// Data o zrušení vlaku z XML souboru
        /// </summary>
        public CZCanceledPTTMessage TrainCancelationData => FileData.CancelTrainData;

        public override PlannedTransportIdentifiers TrainId => TrainCancelationData.GetTrainIdentifier();

        public override DateTime CreationDate
        {
            get { return TrainCancelationData.CZPTTCancelation; }
            set { TrainCancelationData.CZPTTCancelation = value; }
        }

        /// <summary>
        /// Soubor (vlak), který je tímto souborem rušen.
        /// </summary>
        public AbstractTrainFile OriginalTrain => OriginalTrains.LastOrDefault();

        public TrainCancelationFile(SingleTrainFile fileData)
            : base(fileData)
        {
            RefreshDaysOfWeekVisual();
            Locations = new ObservableCollection<TrainLocation>(); // nemá lokace
        }

        /// <summary>
        /// Nastaví některé informace o vlaku ze souboru, na který tento rušící záznam odkazuje (rušící soubor obsahuje jen referenci)
        /// Konkrétně: linku, trasu, čísla a typy vlaků a IDS
        /// </summary>
        public void CopyDataFromOriginalTrain()
        {
            LineName = OriginalTrain?.LineName ?? "";
            Route = "";
            TrainTypeAndNumber = OriginalTrain?.TrainTypeAndNumber ?? "";
            AllTrainNumbers = OriginalTrain?.AllTrainNumbers ?? new int[0];
            AllLineNames = OriginalTrain?.AllLineNames ?? new string[0];
            IntegratedSystems = (OriginalTrain?.IntegratedSystems).GetValueOrDefault();
        }

        public override void RefreshDaysOfWeekVisual()
        {
            DaysOfWeekColors = Enumerable.Repeat(Brushes.White, 7).ToArray();
            foreach (var dow in DaysOfWeekColors) dow.Freeze();
        }

        public override string ToString()
        {
            return $"zrušení {TrainTypeAndNumber}";
        }

    }
}
