using System.Windows;
using TrainsEditor.ExportModel;

namespace TrainsEditor
{
    /// <summary>
    /// Interaction logic for LocationWindow.xaml
    /// </summary>
    public partial class LocationWindow : Window
    {
        private StationDatabase StationDatabase;

        internal LocationWindow(StationDatabase stationDatabase)
        {
            StationDatabase = stationDatabase;
            InitializeComponent();

            cbStation.ItemsSource = stationDatabase.AllStops.Values;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
