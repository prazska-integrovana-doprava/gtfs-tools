using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainsEditor.CommonModel
{
    /// <summary>
    /// Reprezentuje skupinu souborů se stejným základovým ID vlaku (TR Company + Core + Year - tedy kombinace ID dopravce a jeho ID vlaku a GVD),
    /// tzn. různé varianty tohoto vlaku a různé změny a rušení vlaků ve vybrané dny.
    /// Vlaky ve skupině mohou mít i různá čísla, ale z pohledu dopravce jde o jeden a ten samý vlak (respektive jeho varianty).
    /// V rámci skupiny jsou rovnou dopočítány kalendáře podle toho, jak se ve skupině přepisují starší vlaky novějšími.
    /// </summary>
    class TrainGroup
    {
        private readonly List<SingleTrainFile> _trainFiles;

        /// <summary>
        /// Vrací všechny vlaky ve skupině seřazené podle data vzniku.
        /// </summary>
        public IEnumerable<SingleTrainFile> TrainFiles => _trainFiles;

        /// <summary>
        /// Slouží k řazení skupin podle čísla vlaku
        /// </summary>
        public int MinimumNonzeroTrainNumber
        {
            get
            {
                var trainNumbers = _trainFiles.Where(tr => !tr.IsCancelation) // jen skutečné vlaky ze skupiny
                    .SelectMany(tr => tr.TrainData.CZPTTInformation.CZPTTLocation). // všechna zastavení všech vlaků skupiny ve stanicích
                    Select(loc => loc.OperationalTrainNumber).Where(num => num > 0); // čísla vlaků v zastaveních větší než 0

                if (trainNumbers.Any())
                {
                    return trainNumbers.Min();
                }
                else
                {
                    return int.MaxValue;
                }
            }
        }

        public TrainGroup()
        {
            _trainFiles = new List<SingleTrainFile>();
        }

        /// <summary>
        /// Přidá soubor vlaku do skupiny. Ten by měl mít shodné TR ID Company a Core a Year (nekontroluje se). Nastaví též <see cref="SingleTrainFile.OwnerGroup"/> na tuto skupinu.
        /// </summary>
        /// <param name="trainFile">Soubor vlaku</param>
        public void AddTrain(SingleTrainFile trainFile)
        {
            _trainFiles.Add(trainFile);
            trainFile.OwnerGroup = this;
        }

        /// <summary>
        /// Odebere soubor vlaku ze skupiny.
        /// </summary>
        /// <param name="trainFile"></param>
        public void RemoveTrain(SingleTrainFile trainFile)
        {
            _trainFiles.Remove(trainFile);
        }

        /// <summary>
        /// Naplní všem vlakům ve skupině hodnotu <see cref="SingleTrainFile.BitmapEx"/>.
        /// Hodnoty se nastavují vždy z dat souborů ve skupině, metodu tedy lze použít i pro obnovení bitmap po úpravě nebo přenačtení některého ze souborů skupiny.
        /// </summary>
        public void ProcessCalendars()
        {
            _trainFiles.Sort((x, y) => x.CreationDateTime.CompareTo(y.CreationDateTime));
            foreach (var trainFile in _trainFiles)
            {
                trainFile.InitBitmap();
            }

            for (int i = 0; i < _trainFiles.Count; i++)
            {
                for (int j = i + 1; j < _trainFiles.Count; j++)
                {
                    if (!_trainFiles[i].IsCancelation)
                    {
                        IncorporateOverwrittingTrains(_trainFiles[i], _trainFiles[j]);
                    }
                }
            }
        }

        // provede přepis vlaku jiným vlakem
        private void IncorporateOverwrittingTrains(SingleTrainFile train, SingleTrainFile overTrain)
        {
            var daysDelta = (overTrain.StartDate - train.StartDate).Days;
            bool isOverwritten = false;
            for (int myIndex = Math.Max(0, daysDelta); myIndex < train.BitmapEx.Length; myIndex++)
            {
                var overIndex = myIndex - daysDelta;
                if (overIndex >= overTrain.BitmapEx.Length)
                    break;

                if (train.BitmapEx[myIndex] == CalendarValue.Active)
                {
                    if (overTrain.BitmapEx[overIndex] == CalendarValue.Active)
                    {
                        train.BitmapEx[myIndex] = CalendarValue.Overwritten;
                        isOverwritten = true;
                    }
                }
            }

            if (isOverwritten)
            {
                overTrain.OverwrittenTrains.Add(train);
            }
        }
    }
}
