using CommonLibrary;
using CzpttModel;
using CzpttModel.Kango;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrainsEditor.CommonLogic;
using TrainsEditor.EditorLogic;
using TrainsEditor.ExportModel;
using TrainsEditor.ViewModel;

namespace TrainsEditor
{
    /// <summary>
    /// Interaction logic for TrainWindow.xaml
    /// </summary>
    public partial class TrainWindow : Window
    {
        private static readonly Dictionary<string, int> ShiftValues = new Dictionary<string, int>()
        {
            {"-1 hodina", -3600},
            {"-10 minut", -600},
            {"-1 minuta", -60},
            {"-30 sekund", -30},
            {"+30 sekund", 30},
            {"+1 minuta", 60},
            {"+10 minut", 600},
            {"+1 hodina", 3600}
        };

        private readonly StationDatabase StationDatabase;

        private readonly RouteDatabase RouteDatabase;

        private TrainFile Train => (TrainFile) DataContext;

        internal TrainWindow(StationDatabase stationDatabase, RouteDatabase routeDatabase)
        {
            StationDatabase = stationDatabase;
            RouteDatabase = routeDatabase;

            InitializeComponent();
            foreach (var shiftValue in ShiftValues)
            {
                cbShiftTimeValue.Items.Add(shiftValue.Key);
            }

            cbShiftTimeValue.SelectedIndex = ShiftValues.Count / 2 + 1;

            foreach (var trainType in CommercialTrafficType.CommercialTrafficTypes.Values)
            {
                cbTrainType.Items.Add(trainType);
            }
        }

        private void CopySelectedLocations()
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>();
            var locationsXml = selectedItems.Select(loc => loc.LocationData).ToArray().SerializeObject();
            Clipboard.SetData(DataFormats.UnicodeText, locationsXml);
        }

        private void PasteSelectedLocations(bool pasteToTopOfTheList = false)
        {
            int currentIndex = -1;
            if (!pasteToTopOfTheList)
            {
                currentIndex = lvLocations.Items.IndexOf(lvLocations.SelectedItems.OfType<TrainLocation>().LastOrDefault());
            }

            var locationsXml = (string)Clipboard.GetData(DataFormats.UnicodeText);
            var locationObjects = XmlSerializeToObjectHelper.DeserializeObject<CZPTTLocation[]>(locationsXml);
            var prevLocation = currentIndex > -1 ? (TrainLocation)lvLocations.Items[currentIndex] : null;

            foreach (var locationObj in locationObjects)
            {
                currentIndex++;
                var isNewFirst = (currentIndex == 0);
                var isFirstAdded = (locationObj == locationObjects.First());
                var isLastAdded = (locationObj == locationObjects.Last());
                var isNewLast = (currentIndex == lvLocations.Items.Count);
                var locationViewModel = TrainLocation.Construct(locationObj, prevLocation?.LocationData, isNewFirst || isNewLast && isLastAdded, StationDatabase, RouteDatabase, null);
                if (isNewFirst && lvLocations.Items.Count > 1)
                {
                    var nextLocation = (TrainLocation)lvLocations.Items[0];
                    nextLocation.IsFirstOrLastStation = false;
                }
                else if (isNewLast && isFirstAdded && currentIndex > 1)
                {
                    prevLocation.IsFirstOrLastStation = false;
                }

                Train.Locations.Insert(currentIndex, locationViewModel);
            }
        }

        private void DeleteSelectedLocations()
        {
            var selectedIndex = lvLocations.SelectedIndex;
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>().ToArray();
            foreach (var location in selectedItems)
            {
                Train.Locations.Remove(location);
            }

            if (lvLocations.Items.Count > 0)
            {
                var newFirstItem = (TrainLocation)lvLocations.Items[0];
                newFirstItem.IsFirstOrLastStation = true;
                var newLastItem = (TrainLocation)lvLocations.Items[lvLocations.Items.Count - 1];
                newLastItem.IsFirstOrLastStation = true;
            }

            if (selectedIndex >= lvLocations.Items.Count)
            {
                selectedIndex = lvLocations.Items.Count - 1;
            }

            lvLocations.SelectedIndex = selectedIndex;
        }

        private void CancelAndReloadFile()
        {
            FilesManager.ReloadFile(Train, StationDatabase, RouteDatabase);
            // TODO problémy viz MainWindow.ReloadFile
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            FilesManager.SaveFile(Train);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelAndReloadFile();
        }

        private void btnChangeBitmap_Click(object sender, RoutedEventArgs e)
        {
            var result = DateSelectWindow.ShowSelectDateDialog(Train.Calendar.StartDate, Train.Calendar.EndDate);
            if (result.DialogResult.GetValueOrDefault())
            {
                Train.Calendar.ResizeBitmap(result.StartDate, result.EndDate, true);
            }
        }

        private void lvLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>();
            if (selectedItems.Any())
            {
                var commonLineName = selectedItems.First().LineNumber;
                var commonTrainNumber = selectedItems.First().LocationData.OperationalTrainNumber;
                var commonTrainType = selectedItems.First().LocationData.GetCommercialTrafficTypeInfo();
                foreach (var item in selectedItems)
                {
                    if (item.LineNumber != commonLineName)
                    {
                        commonLineName = "";
                    }

                    if (item.LocationData.OperationalTrainNumber != commonTrainNumber)
                    {
                        commonTrainNumber = 0;
                    }

                    if (item.LocationData.GetCommercialTrafficTypeInfo() != commonTrainType)
                    {
                        commonTrainType = null;
                    }
                }

                txtLineName.Text = commonLineName;
                txtTrainNumber.Text = commonTrainNumber != 0 ? commonTrainNumber.ToString() : "";
                cbTrainType.SelectedItem = commonTrainType;

                // network parametry bereme víc zhruba, podle první vybraný
                var wheelchairPickup = Train.NetworkSpecificParamsProvider.FindCentralNotesForLocation(selectedItems.First().LocationData, CentralNoteCode.WheelchairTransportAndPickup).Any();
                var wheelchairTransport = Train.NetworkSpecificParamsProvider.FindCentralNotesForLocation(selectedItems.First().LocationData, CentralNoteCode.WheelchairTransportAvailable, CentralNoteCode.WheelchairTransportAndPickup).Any();
                if (wheelchairPickup)
                {
                    cbWheelchairModes.SelectedItem = cbWheelchairModesTransportAndPickupItem;
                }
                else if (wheelchairTransport)
                {
                    cbWheelchairModes.SelectedItem = cbWheelchairModesTransportAvailableItem;
                }
                else
                {
                    cbWheelchairModes.SelectedItem = cbWheelchairModesNotAvailableItem;
                }

                // NAD se určuje třístavově, pokud všechny stejné => hodnota, jinak null
                if (selectedItems.All(it => it.IsAlternativeTransport))
                {
                    cbSetAlternativeTransport.IsChecked = true;
                }
                else if (selectedItems.All(it => !it.IsAlternativeTransport))
                {
                    cbSetAlternativeTransport.IsChecked = false;
                }
                else
                {
                    cbSetAlternativeTransport.IsChecked = null;
                }
            }
            else
            {
                cbTrainType.SelectedIndex = -1;
                txtLineName.Text = "";
                txtTrainNumber.Text = "";
            }
        }

        private void btnSetTrainNumber_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>();

            // najde ID druhu dopravy, pokud je druh dopravy v seznamu, jinak vyplní 0
            var commercialTrafficTypeId = CommercialTrafficType.CommercialTrafficTypes.Keys.FirstOrDefault(key => CommercialTrafficType.CommercialTrafficTypes[key] == cbTrainType.SelectedItem);
            if (!int.TryParse(txtTrainNumber.Text, out int trainNumber))
            {
                MessageBox.Show("Číslo vlaku musí být číslo.", "Editor vlaků", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            foreach (var item in selectedItems)
            {
                item.SetTrainTypeAndNumber(commercialTrafficTypeId, trainNumber);
            }
        }

        private void btnSetLineName_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>();

            // TODO mohli bychom přidat kontrolu, že ta linka v PIDu existuje + info, když nejde přeložit na číselník SŽ
            foreach (var item in selectedItems)
            {
                item.LineNumber = txtLineName.Text;
            }
        }

        private void btnShiftTime_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>().ToArray();
            if (cbShiftTimeValue.SelectedIndex < 0)
                return;

            var timeAmount = ShiftValues[(string)cbShiftTimeValue.SelectedItem];

            for (int i = 0; i < selectedItems.Length; i++)
            {
                var item = selectedItems[i];
                if (item.ArrivalTime.HasValue && (i > 0 || chcShiftTimeIncludeFirstArrival.IsChecked.GetValueOrDefault()))
                {
                    if (item.ArrivalTime.Value.TotalSeconds < -timeAmount)
                    {
                        item.ArrivalTime = null;
                    }
                    else
                    {
                        item.ArrivalTime = item.ArrivalTime.Value.AddSeconds(timeAmount);
                    }
                }

                if (item.DepartureTime.HasValue && (i < selectedItems.Length - 1 || chcShiftTimeIncludeLastDeparture.IsChecked.GetValueOrDefault()))
                {
                    if (item.DepartureTime.Value.TotalSeconds < -timeAmount)
                    {
                        item.DepartureTime = null;
                    }
                    else
                    {
                        item.DepartureTime = item.DepartureTime.Value.AddSeconds(timeAmount);
                    }
                }                
            }
        }

        private void lvLocations_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvLocations.SelectedItem != null)
            {
                var locationWindow = new LocationWindow(StationDatabase)
                {
                    DataContext = lvLocations.SelectedItem
                };

                locationWindow.ShowDialog();
            }
        }

        private void btnCopyLocation_Click(object sender, RoutedEventArgs e)
        {
            CopySelectedLocations();
        }

        private void btnPasteLocation_Click(object sender, RoutedEventArgs e)
        {
            PasteSelectedLocations();
        }

        private void btnInsertLocationTop_Click(object sender, RoutedEventArgs e)
        {
            PasteSelectedLocations(true);
        }

        private void btnDeleteLocation_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedLocations();
        }

        private void lvLocations_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                CopySelectedLocations();
            }
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                PasteSelectedLocations();
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelectedLocations();
            }
        }

        private void btnDatesSet1_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(true, d => true);
        }

        private void btnDatesSet0_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(false, d => true);
        }

        private void btnDatesSet1Workdays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(true, d => DaysOfWeekCalendars.TrainsInstance.IsWorkday(d));
        }

        private void btnDatesSet1Saturdays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(true, d => d.DayOfWeek == DayOfWeek.Saturday);
        }

        private void btnDatesSet1Sundays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(true, d => d.DayOfWeek == DayOfWeek.Sunday);
        }

        private void btnDatesSet1Holidays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(true, d => DaysOfWeekCalendars.TrainsInstance.DayExceptions.Keys.Contains(d));
        }

        private void btnDatesSet0Workdays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(false, d => DaysOfWeekCalendars.TrainsInstance.IsWorkday(d));
        }

        private void btnDatesSet0Saturdays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(false, d => d.DayOfWeek == DayOfWeek.Saturday);
        }

        private void btnDatesSet0Sundays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(false, d => d.DayOfWeek == DayOfWeek.Sunday);
        }

        private void btnDatesSet0Holidays_Click(object sender, RoutedEventArgs e)
        {
            SetDatesUniversal(false, d => DaysOfWeekCalendars.TrainsInstance.DayExceptions.Keys.Contains(d));
        }
        private void SetDatesUniversal(bool destValue, Func<DateTime, bool> dateSelector)
        {
            var selectedItems = lvDates.SelectedItems.OfType<TrainCalendar.DateRecord>();
            foreach (var item in selectedItems.Where(item => dateSelector(item.Date)))
            {
                item.Value = destValue;
            }

            Train.Calendar.OnDateRecordChanged();
        }

        private void btnDatesSetInvert_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lvDates.SelectedItems.OfType<TrainCalendar.DateRecord>();
            foreach (var item in selectedItems)
            {
                item.Value = !item.Value;
            }

            Train.Calendar.OnDateRecordChanged();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == 0)
            {
                CancelAndReloadFile();
            }
        }

        private void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Title = "Vlak " + ((AbstractTrainFile)e.NewValue).ToString();
        }

        private void btnSetWheelchair_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>().ToArray();
            if (!selectedItems.Any())
                return;

            var selectedItemsLocationData = selectedItems.Select(l => l.LocationData);
            var firstSelectedItem = selectedItems.OrderBy(loc => lvLocations.Items.IndexOf(loc)).First().LocationData;
            var lastSelectedItem = selectedItems.OrderByDescending(loc => lvLocations.Items.IndexOf(loc)).First().LocationData;
            if (cbWheelchairModes.SelectedItem == cbWheelchairModesTransportAndPickupItem)
            {
                // přidáme poznámky o vozíku s plošinou
                Train.NetworkSpecificParamsProvider.ExpandOrCreateCentralNoteForLocations(firstSelectedItem, lastSelectedItem, CentralNoteCode.WheelchairTransportAndPickup);
            }
            else if (cbWheelchairModes.SelectedItem == cbWheelchairModesTransportAvailableItem)
            {
                // odebereme poznámky o vozíku s plošinou a přidáme poznámky o vozíku bez plošiny
                Train.NetworkSpecificParamsProvider.RemoveCentralNoteForLocations(selectedItemsLocationData, CentralNoteCode.WheelchairTransportAndPickup);
                Train.NetworkSpecificParamsProvider.ExpandOrCreateCentralNoteForLocations(firstSelectedItem, lastSelectedItem, CentralNoteCode.WheelchairTransportAvailable);
            }
            else if (cbWheelchairModes.SelectedItem == cbWheelchairModesNotAvailableItem)
            {
                // odebereme poznámky o vozíku s plošinou i bez plošiny
                Train.NetworkSpecificParamsProvider.RemoveCentralNoteForLocations(selectedItemsLocationData, CentralNoteCode.WheelchairTransportAndPickup);
                Train.NetworkSpecificParamsProvider.RemoveCentralNoteForLocations(selectedItemsLocationData, CentralNoteCode.WheelchairTransportAvailable);
            }

            foreach (var location in Train.Locations)
            {
                location.OnWheelchairAccessChanged();
            }
        }

        private void cbSetAlternativeTransport_Checked(object sender, RoutedEventArgs e)
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>().ToArray();
            foreach (var item in selectedItems)
            {
                item.IsAlternativeTransport = true;
            }
        }

        private void cbSetAlternativeTransport_Unchecked(object sender, RoutedEventArgs e)
        {
            var selectedItems = lvLocations.SelectedItems.OfType<TrainLocation>().ToArray();
            foreach (var item in selectedItems)
            {
                item.IsAlternativeTransport = false;
            }
        }

    }
}
