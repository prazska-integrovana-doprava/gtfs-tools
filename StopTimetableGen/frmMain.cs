using CsvSerializer;
using GtfsModel;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using GtfsModel.Functions;
using StopTimetableGen.Printers;
using StopTimetableGen.StopTimetableModel;
using StopTimetableGen.Transformations;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StopTimetableGen
{
    public partial class frmMain : Form
    {
        private string gtfsFolder;
        private List<GtfsRoute> gtfsRoutes;
        private List<GtfsStop> gtfsStops;
        private Task<Feed> gtfsFeedTask;

        public frmMain(string gtfsFolder)
        {
            this.gtfsFolder = gtfsFolder;
            gtfsRoutes = CsvFileSerializer.DeserializeFile<GtfsRoute>(Path.Combine(gtfsFolder, "routes.txt"));
            gtfsStops = CsvFileSerializer.DeserializeFile<GtfsStop>(Path.Combine(gtfsFolder, "stops.txt"));
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            cbRoute.Items.AddRange(gtfsRoutes.Select(route => $"{route.ShortName} | {route.LongName}").ToArray());
            cbIgnoredStop.Items.AddRange(gtfsStops.Where(s => s.LocationType == LocationType.Stop).Select(s => s.Name).Distinct().OrderBy(s => s).ToArray());

            gtfsFeedTask = new Task<Feed>(() =>
            {
                var gtfsFeed = GtfsFeedSerializer.DeserializeFeed(gtfsFolder);
                var result = Feed.Construct(gtfsFeed);
                this.Invoke((MethodInvoker)(() =>
                {
                    this.pbStatus.Value = 100;
                    this.pbStatus.Style = ProgressBarStyle.Blocks;
                    lblStatus.Text = "Data připravena.";
                }));
                return result;
            });
            gtfsFeedTask.Start();

            txtOutputFolder.Text = Environment.CurrentDirectory;
            cbTemplate.Items.AddRange(ExcelPrinter.Templates.Select(t => t.Name).ToArray());
            cbTemplate.SelectedIndex = 0;
        }

        private async void btnGo_Click(object sender, EventArgs e)
        {
            var routeIndex = cbRoute.SelectedIndex;
            if (routeIndex == -1)
            {
                MessageBox.Show("Vyberte linku ze seznamu.", "Generátor ZJŘ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var templateIndex = cbTemplate.SelectedIndex;

            btnGo.Enabled = false;
            var gtfsFeed = await gtfsFeedTask;
            pbStatus.Value = 0;
            lblStatus.Text = "Probíhá export...";

            var weekdaySubsets = new List<WeekdaySubset>();
            if (chAllDays.Checked) weekdaySubsets.Add(WeekdaySubset.AllDays);
            if (chWorkdays.Checked) weekdaySubsets.Add(WeekdaySubset.Workdays);
            if (chWeekends.Checked) weekdaySubsets.Add(WeekdaySubset.Weekends);
            if (chSaturdays.Checked) weekdaySubsets.Add(WeekdaySubset.Saturdays);
            if (chSundays.Checked) weekdaySubsets.Add(WeekdaySubset.Sundays);
            var ignoredStops = lbIgnoredStops.Items.Cast<object>().Select(o => o.ToString()).ToArray();
            try
            {
                var timetableLoaderTask = new Task<LineTimetables>(new TransformationFromGtfs(gtfsFeed, gtfsRoutes[routeIndex].Id, dtStartDate.Value, dtEndDate.Value, weekdaySubsets, ignoredStops, chBenevolentDays.Checked, chcIgnoreLowFloor.Checked).PerformExport);
                timetableLoaderTask.Start();
                timetableLoaderTask.Wait();
                var timetables = await timetableLoaderTask;

                //new TextLogger(timetables).LogToFile();
                var excelPrinter = new ExcelPrinter(timetables, ExcelPrinter.Templates[templateIndex], txtOutputFolder.Text, chcIgnoreLowFloor.Checked);
                excelPrinter.OnPrintProgressChange += ExcelPrinter_OnPrintProgressChange;
                var timetableGeneratorTask = new Task(excelPrinter.PrintToExcel);
                timetableGeneratorTask.Start();
                await timetableGeneratorTask;

                lblStatus.Text = "Export dokončen.";
            }
            catch(AggregateException ex)
            {
                MessageBox.Show("Při exportu došlo k následující chybě:\n\n" + ex.InnerException, "Generátor ZJŘ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Při exportu došlo k chybě.";
            }
            finally
            {
                btnGo.Enabled = true;
                pbStatus.Style = ProgressBarStyle.Blocks;
            }
        }

        private void ExcelPrinter_OnPrintProgressChange(object sender, int e)
        {
            // je voláno z jiného vlákna, takže musíme trošku složitě
            Invoke(new Action(delegate { pbStatus.Value = e; }));
        }

        private void btnAddIgnoredStop_Click(object sender, EventArgs e)
        {
            lbIgnoredStops.Items.Add(cbIgnoredStop.Text);
            cbIgnoredStop.Text = "";
        }

        private void lbIgnoredStops_DoubleClick(object sender, EventArgs e)
        {
            if (lbIgnoredStops.SelectedIndex >= 0)
            {
                lbIgnoredStops.Items.RemoveAt(lbIgnoredStops.SelectedIndex);
            }
        }

        private void btnOutputFolderSelect_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = txtOutputFolder.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtOutputFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }
    }
}
