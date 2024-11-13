using AswModel.Extended;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TrainsEditor.CommonLogic;
using TrainsEditor.Properties;
using TrainsEditor.ViewModel;
using System;
using TrainsEditor.ExportModel;
using TrainsEditor.GtfsExport;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using TrainsEditor.EditorLogic;
using System.Diagnostics;
using GtfsModel.Functions;
using GtfsLogging;
using TrainsEditor.CommonModel;
using System.IO;

namespace TrainsEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CollectionViewSource TrainsView { get; set; }

        ObservableCollection<AbstractTrainFile> TrainFiles => (ObservableCollection<AbstractTrainFile>) TrainsView.Source;

        TheAswDatabase AswXmlData { get; set; }

        StationDatabase StationDatabase { get; set; }

        RouteDatabase RouteDatabase { get; set; }

        Regex FilterRegex { get; set; }
        Regex FulltextRegex { get; set; }

        private BackgroundWorker backgroundWorker;

        private readonly DispatcherTimer refreshTimer;

        private class BackgroundLoaderArgument
        {
            public string Folder { get; set; }
            public DateTime? PastDataLimit { get; set; }
            public DateTime? StartDateForVisualCalendar { get; set; }
            public DateTime? EndDateForVisualCalendar { get; set; }
        }

        private class BackgroundLoaderResult
        {
            public ObservableCollection<AbstractTrainFile> LoadedFiles { get; set; }
            public Exception Exception { get; set; }
            public string ExceptionFileName { get; set; }
        }

        private class BackgroundGtfsGenerateResult
        {
            public bool Finished { get; set; }
            public string Output { get; set; }
            public Exception Exception { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            Loggers.InitAswDataLoggers(Settings.Default.LogFolder);
            AswXmlData = TheAswDatabase.Construct(false, Settings.Default.StopsAndLinesFileName);
            StationDatabase = StationDatabase.CreateStationDb(AswXmlData.Stops, Settings.Default.SR70Stops, CorrectionConfig.RewriteStations);
            RouteDatabase = RouteDatabase.CreateRouteDb(AswXmlData.Lines);
            Loggers.ClLoseAswDataLoggers();

            txtFolderRepo.Text = Settings.Default.RepositoryFolder;
            refreshTimer = new DispatcherTimer();
            refreshTimer.Tick += RefreshTimer_Tick;

            foreach (var integratedSystem in IntegratedSystemsExtensions.IntegratedSystemsList)
            {
                cbIntegratedSystemsList.Items.Add(integratedSystem);
                cbIntegratedSystemsList.SelectedIndex = 0;
            }
        }

        private void SetRefreshTimer()
        {
            refreshTimer.Interval = new TimeSpan(5000000); // 500 ms
            refreshTimer.Start();
        }

        private void BackgroundWorker_DoLoadFilesFromFolder(object sender, DoWorkEventArgs e)
        {
            var arg = (BackgroundLoaderArgument)e.Argument;
            try
            {
                var orderedTrains = FilesManager.LoadTrainFiles(arg.Folder, arg.PastDataLimit, StationDatabase, RouteDatabase, arg.StartDateForVisualCalendar, arg.EndDateForVisualCalendar, LoadTrainFilesCallback);
                e.Result = new BackgroundLoaderResult()
                {
                    LoadedFiles = new ObservableCollection<AbstractTrainFile>(orderedTrains),
                    Exception = null
                };
            }
            catch (Exception ex)
            {
                e.Result = new BackgroundLoaderResult()
                {
                    LoadedFiles = new ObservableCollection<AbstractTrainFile>(),
                    Exception = ex,
                    ExceptionFileName = FilesManager.TrainGroupLoader.CurrentlyProcessedFileName,
                };
            }
        }

        public void LoadTrainFilesCallback(int loadedFilesCount, int totalFilesCount, out bool shouldResume)
        {
            backgroundWorker.ReportProgress(loadedFilesCount * 100 / totalFilesCount);
            shouldResume = !backgroundWorker.CancellationPending;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            progressBar.IsIndeterminate = e.ProgressPercentage >= 100;
        }

        private void BackgroundWorker_LoadFilesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var result = (BackgroundLoaderResult)e.Result;
            if (result.Exception != null)
            {
                var exceptionFileName = result.ExceptionFileName != null ? "\n\nSoubor: " + result.ExceptionFileName : "";
                MessageBox.Show($"Během načítání dat došlo k chybě a bylo přerušeno!\n\n{result.Exception.Message}{exceptionFileName}", "Editor vlaků", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            TrainsView = new CollectionViewSource() { Source = result.LoadedFiles };
            TrainsView.Filter += TrainsView_Filter;
            TrainsView.GroupDescriptions.Add(new PropertyGroupDescription() { PropertyName = "TrainId.CompanyAndCoreAndYear" });
            DataContext = TrainsView.View;

            btnStop.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;
            lblCurrentAction.Content = "";

            btnDownload.IsEnabled = true;
            btnLoadRepo.IsEnabled = true;
            btnGenerateGtfs.IsEnabled = true;
            btnSelectAll.IsEnabled = true;
            btnSelectNone.IsEnabled = true;
            btnDeleteFile.IsEnabled = true;
            btnDuplicateFile.IsEnabled = true;
            btnCreateCancelFile.IsEnabled = true;
            btnReloadFile.IsEnabled = true;
            btnSaveChanges.IsEnabled = true;
            btnOpenInTextEditor.IsEnabled = true;
            btnShowGtfs.IsEnabled = true;
            btnShowChanges.IsEnabled = true;

            backgroundWorker = null;
        }
        
        private void btnLoadRepo_Click(object sender, RoutedEventArgs e)
        {
            btnDownload.IsEnabled = false;
            btnLoadRepo.IsEnabled = false;
            btnGenerateGtfs.IsEnabled = false;
            btnStop.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            progressBar.IsIndeterminate = false;
            progressBar.Visibility = Visibility.Visible;
            lblCurrentAction.Content = "Načítání dat...";

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorker_DoLoadFilesFromFolder;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_LoadFilesCompleted;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;

            backgroundWorker.RunWorkerAsync(new BackgroundLoaderArgument()
            {
                Folder = txtFolderRepo.Text,
                PastDataLimit = chcIgnorePastData.IsChecked.GetValueOrDefault() ? DateTime.Now.AddHours(-4) : (DateTime?)null,
                StartDateForVisualCalendar = dtStartDate.SelectedDate,
                EndDateForVisualCalendar = dtEndDate.SelectedDate,
            });
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            refreshTimer.Stop();
            if (TrainsView != null)
                TrainsView.View.Refresh();
        }

        private void TrainsView_Filter(object sender, FilterEventArgs e)
        {
            if (!(e.Item is AbstractTrainFile train))
                return;

            if (chcFilterApplyNewerThan.IsChecked.GetValueOrDefault() && dtFilterNewerThan.SelectedDate.HasValue)
            {
                var dateTimeNewerThan = dtFilterNewerThan.SelectedDate.Value;
                if (train.AllTrainsInGroup.All(tr => tr.CreationDate < dateTimeNewerThan))
                {
                    e.Accepted = false;
                    return;
                }
            }

            var integratedTrainsOnly = (cbFilterIntegratedSystem.SelectedItem == cbFilterIntegratedOnly);
            var nonIntegratedTrainsOnly = (cbFilterIntegratedSystem.SelectedItem == cbFilterNonIntegratedOnly);
            var selectedIntegratedSystem = (IntegratedSystemsEnum)cbIntegratedSystemsList.SelectedItem;
            if (integratedTrainsOnly && train.AllTrainsInGroup.All(tr => !tr.IntegratedSystems.Contains(selectedIntegratedSystem))
                || nonIntegratedTrainsOnly && train.AllTrainsInGroup.All(tr => tr.IntegratedSystems.Contains(selectedIntegratedSystem)))
            {
                e.Accepted = false;
                return;
            }

            var activeTrainsOnly = (cbFilterValidity.SelectedItem == cbFilterValiditySelectValidOnly);
            var notActiveTrainsOnly = (cbFilterValidity.SelectedItem == cbFilterValiditySelectNotValidOnly);
            if (activeTrainsOnly && train.AllTrainsInGroup.All(tr => tr.Calendar.EndDate < DateTime.Now.Date)
                || notActiveTrainsOnly && train.AllTrainsInGroup.Any(tr => tr.Calendar.EndDate > DateTime.Now.Date))
            {
                e.Accepted = false;
                return;
            }

            if (FilterRegex != null)
            { 
                e.Accepted = train.AllTrainsInGroup.Any(tr => 
                    tr.AllTrainNumbers.Any(num => FilterRegex.IsMatch(num.ToString()))
                    || tr.AllLineNames.Any(line => FilterRegex.IsMatch(line))
                    || tr.Locations.Any(loc => FulltextRegex.IsMatch(loc.LocationName)));
            }
            else
            {
                e.Accepted = true;
            }
        }

        protected void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is TrainFile item)
            {
                var trainWindow = new TrainWindow(StationDatabase, RouteDatabase);
                trainWindow.DataContext = item;
                trainWindow.Show();
            }
        }
        
        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = txtFolderRepo.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFolderRepo.Text = dialog.SelectedPath;
            }
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var trimmedStr = txtFilter.Text.Trim();
            if (trimmedStr == "")
            {
                FilterRegex = null;
                FulltextRegex = null;
            }
            else
            {
                try
                {
                    FilterRegex = new Regex("(?i:^" + trimmedStr.Replace("x", "[0-9]") + "$)");
                    FulltextRegex = new Regex("(?i:" + trimmedStr + ")");
                    txtFilter.Foreground = Brushes.Black;
                }
                catch
                {
                    txtFilter.Foreground = Brushes.Red;
                    FilterRegex = null;
                    FulltextRegex = null;
                }
            }

            SetRefreshTimer();
        }

        private void btnDuplicateFile_Click(object sender, RoutedEventArgs e)
        {
            var trains = listView.SelectedItems.OfType<AbstractTrainFile>().ToArray();
            if (!trains.Any())
            {
                MessageBox.Show("Nejsou vybrány žádné vlaky.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectDateDialogResult = DateSelectWindow.ShowSelectDateDialog(trains.Min(t => t.Calendar.StartDate), trains.Max(t => t.Calendar.EndDate));
            if (selectDateDialogResult.DialogResult.GetValueOrDefault())
            {
                listView.SelectedItems.Clear();
                foreach (var train in trains)
                {
                    var lastTrainInGroup = train.OverwrittingTrains.Any() ? train.OverwrittingTrains.Last() : train;
                    var duplicatedTrain = FilesManager.DuplicateFile(train, selectDateDialogResult.StartDate, selectDateDialogResult.EndDate, StationDatabase, RouteDatabase);
                    TrainFiles.Insert(TrainFiles.IndexOf(lastTrainInGroup) + 1, duplicatedTrain);
                    listView.SelectedItems.Add(duplicatedTrain);
                    if (duplicatedTrain is TrainFile)
                    {
                        var trainWindow = new TrainWindow(StationDatabase, RouteDatabase);
                        trainWindow.DataContext = duplicatedTrain;
                        trainWindow.Show();
                    }
                }
            }

            //TrainsView.View.Refresh();
            listView.Focus();
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in listView.Items)
            {
                listView.SelectedItems.Add(item);
            }

            listView.Focus();
        }

        private void btnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            listView.SelectedItems.Clear();
        }

        private void btnDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Nejsou vybrány žádné soubory.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = MessageBox.Show($"Opravdu chcete odstranit vybrané soubory ({listView.SelectedItems.Count})? Tuto operaci nelze vzít zpět.", null, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dialogResult == MessageBoxResult.Yes)
            {
                var trains = listView.SelectedItems.OfType<AbstractTrainFile>().ToArray();
                FilesManager.DeleteFiles(trains);
                foreach (var train in trains)
                {
                    TrainFiles.Remove(train);
                }
            }
        }

        private void btnCancelFilter_Click(object sender, RoutedEventArgs e)
        {
            txtFilter.Text = "";
            txtFilter.Focus();
        }

        private void btnReloadFile_Click(object sender, RoutedEventArgs e)
        {
            var trains = listView.SelectedItems.OfType<AbstractTrainFile>().ToArray();
            foreach (var train in trains)
            {
                FilesManager.ReloadFile(train, StationDatabase, RouteDatabase);

                // TODO dnes neumíme dobře vyřešit, když se změní CreationDate nebo OwnerGroup
                // v případě změny CreationDate to sice umíme inkorporovat do kalendáře, ale už nerealizujeme posun v seznamu vlaků (takže kalendáře budou dobře, ale vlaky budou prohozené)
                // v případě změny OwnerGroup metoda hodí výjimku (ale šlo by to taky udělat přesunem v seznamu vlaků)
                // pořád stojí za zvážení, jestli neupravit strukturu, aby vlaky byly po skupinách
            }
            
            //TrainsView.View.Refresh();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker != null)
            {
                backgroundWorker.CancelAsync();
            }
        }

        private void cbFilterIntegratedSystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private void cbIntegratedSystemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private void cbFilterValidity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private void btnFilterHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Můžete použít:\n\n- Čísla vlaků (x jako zástupný znak). Např.: 610, 8814, 99xx.\nČísla linek. Např. S1, R33, U12.\nČásti názvů stanic. Např. Praha-.");
        }

        private void chcFilterNotInRepo_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private void chcFilterNotInRepo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private IEnumerable<AbstractTrainFile> TrainsWithUnsavedChanges()
        {
            if (TrainsView == null || TrainsView.Source == null)
                return Enumerable.Empty<AbstractTrainFile>();

            return TrainFiles.Where(tr => tr.HasUnsavedChanges);
        }

        private void btnGenerateGtfs_Click(object sender, RoutedEventArgs e)
        {
            if (TrainsWithUnsavedChanges().Any())
            {
                if (MessageBox.Show("Vlaky níže obsahují neuložené změny. Tyto změny nebudou zahrnuty do generování GTFS. Pokud změny chcete zahrnout, stiskněte Zrušit a vlaky uložte.\n\n"
                    + string.Join("\n", TrainsWithUnsavedChanges().Select(tr => tr.TrainTypeAndNumber)), "Editor vlaků", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            btnDownload.IsEnabled = false;
            btnLoadRepo.IsEnabled = false;
            btnGenerateGtfs.IsEnabled = false;
            btnStop.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            progressBar.IsIndeterminate = false;
            progressBar.Visibility = Visibility.Visible;
            lblCurrentAction.Content = "Generování GTFS...";

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorker_DoGenerateGtfs;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_GenerateGtfsCompleted;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;

            Loggers.InitExportModuleLoggers(Settings.Default.LogFolder);
            backgroundWorker.RunWorkerAsync(new GtfsExportModule(StationDatabase, RouteDatabase, Settings.Default.RepresentativeTrips, txtFolderRepo.Text, FilesManager.TrainGroupLoader));
        }

        private void BackgroundWorker_DoGenerateGtfs(object sender, DoWorkEventArgs e)
        {
            var gtfsExportModule = (GtfsExportModule)e.Argument;
            var writer = new StringWriter();
            try
            {
                var finished = gtfsExportModule.Run(Settings.Default.TrackNetworkFile, Settings.Default.OutputFolder, Settings.Default.LogFolder, writer, LoadTrainFilesCallback);
                e.Result = new BackgroundGtfsGenerateResult()
                {
                    Finished = finished,
                    Output = writer.ToString()
                };
            }
            catch (Exception ex)
            {
                e.Result = new BackgroundGtfsGenerateResult()
                {
                    Output = writer.ToString(),
                    Exception = ex
                };
            }
        }

        private void BackgroundWorker_GenerateGtfsCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Loggers.CloseExportModuleLoggers();

            var result = (BackgroundGtfsGenerateResult)e.Result;
            string resultText = "";
            if (result.Exception == null)
            {
                if (result.Finished)
                {
                    resultText = "GENEROVÁNÍ SKONČILO ÚSPĚŠNĚ\n-----------\n\nLOG:\n\n" + result.Output;
                }
            }
            else
            {
                resultText = "PŘI GENEROVÁNÍ DOŠLO K CHYBĚ\n-----------\n\nLOG:\n\n" + result.Output + "\n" + result.Exception.ToString();
            }

            var fileName = $"GTFS_LOG_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt";
            using (var writer = File.CreateText(fileName))
            {
                writer.Write(resultText);
            }

            resultText += "\nUloženo do souboru: " + fileName;

            btnStop.Visibility = Visibility.Hidden;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Hidden;
            lblCurrentAction.Content = "";

            btnDownload.IsEnabled = true;
            btnLoadRepo.IsEnabled = true;
            btnGenerateGtfs.IsEnabled = true;

            backgroundWorker = null;

            if (result.Exception == null && result.Finished)
            {
                TextWindow.ShowTextInfo(resultText);
            }
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (TrainsWithUnsavedChanges().Any())
            {
                if (MessageBox.Show("Vlaky níže obsahují neuložené změny, které budou ztraceny. Pokud je chcete uložit, stiskněte Zrušit a vlaky uložte.\n\n"
                    + string.Join("\n", TrainsWithUnsavedChanges().Select(tr => tr.TrainTypeAndNumber)), "Editor vlaků", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            foreach (var train in TrainsWithUnsavedChanges())
            {
                FilesManager.SaveFile(train);
            }
        }

        private void btnRefreshVisuals_Click(object sender, RoutedEventArgs e)
        {
            if (dtStartDate.SelectedDate.HasValue && dtEndDate.SelectedDate.HasValue)
            {
                foreach (var train in TrainFiles)
                {
                    train.VisualBitmap.ResetDateInterval(dtStartDate.SelectedDate.Value, dtEndDate.SelectedDate.Value);
                }
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            var startYear = 2024;
            var endYear = 2025;

            var filesDownloaders = new List<FilesDownloader>();
            for (int year = startYear; year <= endYear; year++)
            {
                var filesDownloader = new FilesDownloader(txtFolderRepo.Text, year);
                var lastModifiedDate = filesDownloader.GetNewestModifiedDate();
                filesDownloader.ListFiles(lastModifiedDate.GetValueOrDefault());

                var msgText = $"GVD {year}: Prozatím ve složce nejsou žádné stažené soubory.";
                var msgButtons = MessageBoxButton.OKCancel;
                if (lastModifiedDate.HasValue)
                {
                    msgText = $"GVD {year}: Poslední stažený soubor je z {lastModifiedDate}.";
                }

                if (filesDownloader.FilesToDownloadList.Count > 0)
                {
                    msgText += $" Celkem bude staženo {filesDownloader.FilesToDownloadList.Count} souborů.";
                }
                else
                {
                    msgText += $" Žádné novější soubory ke stažení se aktuálně na vzdáleném serveru nenachází.";
                    msgButtons = MessageBoxButton.OK;
                }


                if (MessageBox.Show(msgText, "Editor vlaků", msgButtons, MessageBoxImage.Information) == MessageBoxResult.OK
                    && filesDownloader.FilesToDownloadList.Count > 0)
                {
                    dtFilterNewerThan.SelectedDate = lastModifiedDate; // nevadí, že se to přepíše vícekrát, zůstane hodnota z posledního GVD
                    filesDownloaders.Add(filesDownloader);
                }
            }

            foreach (var filesDownloader in filesDownloaders)
            {
                btnDownload.IsEnabled = false;
                btnLoadRepo.IsEnabled = false;
                btnGenerateGtfs.IsEnabled = false;

                lblCurrentAction.Content = $"Stahování nových souborů GVD {filesDownloader.Year}...";
                btnStop.Visibility = Visibility.Visible;
                progressBar.Visibility = Visibility.Visible;
                progressBar.Value = 0;
                progressBar.IsIndeterminate = false;

                backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += BackgroundWorker_DoDownloadFiles; ;
                backgroundWorker.RunWorkerCompleted += BackgroundWorker_DownloadFilesCompleted;
                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.WorkerSupportsCancellation = true;
                backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;

                backgroundWorker.RunWorkerAsync(filesDownloader);
            }
        }

        private void BackgroundWorker_DoDownloadFiles(object sender, DoWorkEventArgs e)
        {
            var filesDownloader = (FilesDownloader)e.Argument;
            try
            {
                filesDownloader.SortFilesToDownload();
                filesDownloader.EnsureDownloadDirectoryExists();
                filesDownloader.ConnectFtp();
            }
            catch (Exception ex)
            {
                e.Result = ex;
                return;
            }

            try
            {
                while (filesDownloader.DownloadAndUnpackNextFile())
                {
                    backgroundWorker.ReportProgress((filesDownloader.ProcessedFileCount + 1) * 100 / filesDownloader.FilesToDownloadList.Count);
                    if (backgroundWorker.CancellationPending)
                    {
                        break;
                    }
                }
            } 
            catch (Exception ex)
            {
                e.Result = ex;
            }
            finally
            {
                filesDownloader.Disconnect();
            }
        }

        private void BackgroundWorker_DownloadFilesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var downloadException = (Exception)e.Result;
            if (downloadException != null)
            {
                MessageBox.Show("Při stahování došlo k chybě a bylo přerušeno!\n\n" + downloadException.Message, "Editor vlaků", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            btnStop.Visibility = Visibility.Hidden;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Hidden;
            lblCurrentAction.Content = "";

            btnDownload.IsEnabled = true;
            btnLoadRepo.IsEnabled = true;
            btnGenerateGtfs.IsEnabled = true;

            backgroundWorker = null;
        }

        private void chcFilterApplyNewerThan_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private void chcFilterApplyNewerThan_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private void dtFilterNewerThan_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                SetRefreshTimer();
        }

        private void btnCreateCancelFile_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Nejsou vybrány žádné soubory.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var trains = listView.SelectedItems.OfType<TrainFile>().ToArray();
            listView.SelectedItems.Clear();
            if (!trains.Any())
            {
                return;
            }

            var lowestStartDate = trains.Min(tr => tr.Calendar.StartDate);
            var highestEndDate = trains.Max(tr => tr.Calendar.EndDate);
            var selectDateDialogResult = DateSelectWindow.ShowSelectDateDialog(lowestStartDate, highestEndDate);

            if (selectDateDialogResult.DialogResult.GetValueOrDefault())
            {
                foreach (var train in trains.OfType<TrainFile>())
                {
                    var lastTrainInGroup = train.OverwrittingTrains.Any() ? train.OverwrittingTrains.Last() : train;
                    var cancelTrain = FilesManager.CreateCancelFile(train, selectDateDialogResult.StartDate, selectDateDialogResult.EndDate);
                    TrainFiles.Insert(TrainFiles.IndexOf(lastTrainInGroup) + 1, cancelTrain);
                    listView.SelectedItems.Add(cancelTrain);
                }
            }

            listView.Focus();
        }

        private void btnOpenInTextEditor_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Nejsou vybrány žádné soubory.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var trains = listView.SelectedItems.OfType<AbstractTrainFile>().ToArray();
            foreach (var train in trains)
            {
                Process.Start("notepad", train.FullPath);
            }
        }

        private void btnShowGtfs_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Nejsou vybrány žádné soubory.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var trains = listView.SelectedItems.OfType<TrainFile>().ToArray();
            var trainGroupCollection = TrainGroupCollection.FromTrainCollection(trains.Select(tr => tr.FileData));

            var writer = new StringWriter();
            var gtfsExportModule = new GtfsExportModule(StationDatabase, RouteDatabase, Settings.Default.RepresentativeTrips, null, FilesManager.TrainGroupLoader, new CommonLogger(writer), new SimpleLogger(writer), new SimpleLogger(writer));
            var calendarConstructor = new CalendarConstructor(gtfsExportModule.ReferenceStartDate);
            var transformedTrains = gtfsExportModule.TransformTrains(trainGroupCollection, calendarConstructor, writer);

            var trainGtfsTexts = transformedTrains.Select(tr => VerboseDescriptor.DescribeTrip(tr));
            TextWindow.ShowTextInfo(string.Join("\n\n", trainGtfsTexts) + "\n\n------------------\n\n" + writer.ToString());
        }

        private void btnShowChanges_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Nejsou vybrány žádné soubory.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var trains = listView.SelectedItems.OfType<TrainFile>().ToArray();
            TrainFile olderTrain, newerTrain;
            if (trains.Length == 1)
            {
                newerTrain = trains[0];
                olderTrain = newerTrain.OriginalTrains.LastOrDefault(tr => tr.TrainId.Variant == newerTrain.TrainId.Variant) as TrainFile;
                if (olderTrain == null)
                {
                    MessageBox.Show("Vybraný vlak nemá žádného předchůdce, není s čím porovnávat.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                listView.SelectedItems.Add(olderTrain);
            }
            else if (trains.Length == 2)
            {
                olderTrain = trains.OrderBy(tr => tr.CreationDate).First();
                newerTrain = trains.OrderBy(tr => tr.CreationDate).Last();
            }
            else
            {
                MessageBox.Show("Vyberte jeden nebo dva vlaky.", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TextWindow.ShowTextInfo(new TrainTextComparison().CompareTrains(olderTrain, newerTrain));
        }
    }
}
