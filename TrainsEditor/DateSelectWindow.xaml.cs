using System;
using System.Windows;

namespace TrainsEditor
{
    /// <summary>
    /// Interaction logic for DateSelectWindow.xaml
    /// </summary>
    public partial class DateSelectWindow : Window
    {
        public static DateTime? LastSelectedStartDate { get; private set; }

        public static DateTime? LastSelectedEndDate { get; private set; }

        public class DateSelectDialogResult
        {
            public bool? DialogResult { get; set; }            

            public DateTime StartDate { get; set; }

            public DateTime EndDate { get; set; }
        }

        public DateSelectWindow()
        {
            InitializeComponent();
        }

        internal static DateSelectDialogResult ShowSelectDateDialog(DateTime originalStartDate, DateTime originalEndDate)
        {
            var dialog = new DateSelectWindow();
            dialog.lblCurrentStartDate.Content = originalStartDate.ToString("dd.MM.yyyy");
            dialog.lblCurrentEndDate.Content = originalEndDate.ToString("dd.MM.yyyy");
            dialog.dtNewStartDate.SelectedDate = LastSelectedStartDate ?? DateTime.Now.Date;
            dialog.dtNewEndDate.SelectedDate = LastSelectedEndDate ?? originalEndDate;
            dialog.ShowDialog();

            if (dialog.DialogResult.GetValueOrDefault())
            {
                LastSelectedStartDate = dialog.dtNewStartDate.SelectedDate;
                LastSelectedEndDate = dialog.dtNewEndDate.SelectedDate;
            }

            return new DateSelectDialogResult()
            {
                DialogResult = dialog.DialogResult,
                StartDate = dialog.dtNewStartDate.SelectedDate.Value,
                EndDate = dialog.dtNewEndDate.SelectedDate.Value,
            };
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
