using StopTimetableGen.Printers;
using System;
using System.Windows.Forms;

namespace StopTimetableGen
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                MessageBox.Show("Aplikace vyžaduje dva argumenty - cestu ke složce s GTFS daty a složku se šablonami ZJŘ.", "Generování ZJŘ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExcelPrinter.InitTemplates(args[1]);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain(args[0]));
        }
    }
}
