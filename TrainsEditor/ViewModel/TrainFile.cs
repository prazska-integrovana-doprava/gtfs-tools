using CommonLibrary;
using CzpttModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using TrainsEditor.CommonLogic;
using TrainsEditor.CommonModel;
using TrainsEditor.ExportModel;

namespace TrainsEditor.ViewModel
{
    /// <summary>
    /// Záznam o jedoucím vlaku (odpovídá jednomu XML souboru CZPTT <see cref="CZPTTCISMessage"/>)
    /// </summary>
    class TrainFile : AbstractTrainFile, INotifyPropertyChanged
    {
        /// <summary>
        /// Data vlaku (soubor)
        /// </summary>
        public CZPTTCISMessage TrainData => FileData.TrainData;

        public override PlannedTransportIdentifiers TrainId => TrainData.GetTrainIdentifier();

        public override DateTime CreationDate
        {
            get { return TrainData.CZPTTCreation; }
            set { TrainData.CZPTTCreation = value; }
        }
        
        public float[] DaysOfWeek { get; private set; }

        public TrainNetworkSpecificParamsProvider NetworkSpecificParamsProvider { get; private set; }


        public event PropertyChangedEventHandler PropertyChanged;

        public TrainFile(SingleTrainFile trainFile, StationDatabase stationDb, RouteDatabase routeDb)
            : base(trainFile)
        {
            ResetData(trainFile, stationDb, routeDb);
        }
        
        public override void ResetData(SingleTrainFile trainFile, StationDatabase stationDb, RouteDatabase routeDb)
        {
            base.ResetData(trainFile, stationDb, routeDb);
            Calendar.PropertyChanged += Calendar_PropertyChanged;
            NetworkSpecificParamsProvider = new TrainNetworkSpecificParamsProvider(TrainData);

            Locations = new ObservableCollection<TrainLocation>();
            CZPTTLocation prevLocation = null;
            foreach (var location in TrainData.CZPTTInformation.CZPTTLocation)
            {
                var isFirstLocation = location == TrainData.CZPTTInformation.CZPTTLocation.First();
                var isLastLocation = location == TrainData.CZPTTInformation.CZPTTLocation.Last();
                var locationEx = TrainLocation.Construct(location, prevLocation, isFirstLocation || isLastLocation, stationDb, routeDb, NetworkSpecificParamsProvider);
                locationEx.PropertyChanged += Location_PropertyChanged;
                Locations.Add(locationEx);

                prevLocation = location;
            }

            RefreshDaysOfWeekVisual();

            Locations.CollectionChanged += Locations_CollectionChanged;
            LocationsChanged(false);
        }

        public override void ClearUnsavedChangesFlag()
        {
            base.ClearUnsavedChangesFlag();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HasUnsavedChanges"));
        }

        // vrátí všechna čísla a typy vlaků jako řetězce (např. "R 981", "0s 9891")
        private IEnumerable<string> GetTrainTypesAndNumbers()
        {
            var result = new List<Tuple<CommercialTrafficType, int>>();
            foreach (var location in Locations)
            {
                var typeAndNum = new Tuple<CommercialTrafficType, int>(location.LocationData.GetCommercialTrafficTypeInfo(), location.LocationData.OperationalTrainNumber);
                bool present = false;
                foreach (var prev in result)
                {
                    if (prev.Item1 == typeAndNum.Item1 && prev.Item2 == typeAndNum.Item2)
                    {
                        present = true;
                        break;
                    }
                }
                
                if (!present)
                {
                    result.Add(typeAndNum);
                }
            }

            foreach (var typeAndNum in result)
            {
                if (typeAndNum.Item1 != null)
                {
                    yield return $"{typeAndNum.Item1} {typeAndNum.Item2}";
                }
                else
                {
                    if (result.All(tr => tr.Item2 != typeAndNum.Item2 || tr.Item1 == null))
                    {
                        // nemá typ vlaku - pokud není v seznamu stejné číslo, které by typ vlaku zadaný mělo, tak vrátíme jen číslo
                        yield return typeAndNum.Item2.ToString();
                    }
                }
            }
        }

        public override void RefreshDaysOfWeekVisual()
        {
            DaysOfWeek = new float[]
            {
                CalculateDayOfWeek(DayOfWeek.Monday),
                CalculateDayOfWeek(DayOfWeek.Tuesday),
                CalculateDayOfWeek(DayOfWeek.Wednesday),
                CalculateDayOfWeek(DayOfWeek.Thursday),
                CalculateDayOfWeek(DayOfWeek.Friday),
                CalculateDayOfWeek(DayOfWeek.Saturday),
                CalculateDayOfWeek(DayOfWeek.Sunday),
            };

            DaysOfWeekColors = new Brush[DaysOfWeek.Length];
            for (int i = 0; i < 5; i++)
            {
                DaysOfWeekColors[i] = new SolidColorBrush(Color.FromArgb((byte)(DaysOfWeek[i] * 255), 181, 230, 29));
                DaysOfWeekColors[i].Freeze();
            }

            for (int i = 5; i < DaysOfWeekColors.Length; i++)
            {
                DaysOfWeekColors[i] = new SolidColorBrush(Color.FromArgb((byte)(DaysOfWeek[i] * 255), 228, 189, 29));
                DaysOfWeekColors[i].Freeze();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DaysOfWeek"));
        }

        // počítá hodnotu "jak moc" vlak jede v daný den v týdnu (0 = nikdy, 1 = vždy)
        private float CalculateDayOfWeek(DayOfWeek dayOfWeek)
        {
            int activeCount = 0, totalCount = 0;
            var bitmap = TrainData.CZPTTInformation.PlannedCalendar.BitmapDays;
            for (int i = 0; i < bitmap.Length; i++)
            {
                var date = Calendar.StartDate.AddDays(i);
                if (DaysOfWeekCalendars.TrainsInstance.GetDayOfWeekFor(date) == dayOfWeek)
                {
                    if (bitmap[i] == '1')
                    {
                        activeCount++;
                    }

                    totalCount++;
                }
            }

            return (float)activeCount / totalCount;            
        }
        
        private void Calendar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            HasUnsavedChanges = true;
            RefreshDaysOfWeekVisual();
            RefreshVisualBitmapsForWholeGroup();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        private void Location_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LocationsChanged(true);
        }

        private void Locations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var trainLocations = TrainData.CZPTTInformation.CZPTTLocation;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<TrainLocation>())
                    {
                        trainLocations.Insert(Locations.IndexOf(item), item.LocationData);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.OfType<TrainLocation>())
                    {
                        trainLocations.Remove(item.LocationData);
                    }

                    break;
            }

            LocationsChanged(true);
        }

        private void LocationsChanged(bool hasUnsavedChanges)
        {
            var trainLocations = TrainData.CZPTTInformation.CZPTTLocation;
            IntegratedSystems = IntegratedSystemsExtensions.GetIntegratedSystemsWithAtLeastTwoStops(Locations.Select(loc => loc.IntegratedSystems));
            AllTrainNumbers = trainLocations.Select(loc => loc.OperationalTrainNumber).Where(num => num > 0).Distinct().ToArray();
            TrainTypeAndNumber = string.Join(" / ", GetTrainTypesAndNumbers());
            
            AllLineNames = trainLocations.Select(loc => loc.GetLineInfo().LineName).Where(lname => !string.IsNullOrWhiteSpace(lname)).Distinct().ToArray();
            LineName = string.Join(" / ", AllLineNames);

            Route = trainLocations.First().GetLocationNameAndTime() + " – " + trainLocations.Last().GetLocationNameAndTime();

            HasUnsavedChanges = hasUnsavedChanges;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public override string ToString()
        {
            return $"{TrainTypeAndNumber} [linka {LineName}]";
        }
    }
}
