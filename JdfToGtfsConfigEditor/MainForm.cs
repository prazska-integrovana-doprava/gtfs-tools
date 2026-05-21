using JdfToGtfsProcessor;
using System.Text.Json;

namespace JdfToGtfsConfigEditor
{
    public partial class MainForm : Form
    {
        private const string ConfigFile = "appsettings.json";

        private AppSettings settings = new();

        private readonly ListBox jdfList = new()
        {
            Width = 500,
            Height = 120
        };

        private readonly Dictionary<string, TextBox> fields = [];

        public MainForm()
        {
            Text = "JdfToGtfsProcessor - Config editor";
            Width = 850;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 3
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 500));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            Controls.Add(panel);

            int row = 0;

            panel.Controls.Add(
                new Label
                {
                    Text = "JDF složky",
                    AutoSize = true
                }, 0, row);

            panel.Controls.Add(jdfList, 1, row);

            var buttonPanel = new FlowLayoutPanel();

            var addButton = new Button
            {
                Text = "Pøidat"
            };

            var removeButton = new Button
            {
                Text = "Odebrat"
            };

            buttonPanel.Controls.Add(addButton);
            buttonPanel.Controls.Add(removeButton);

            panel.Controls.Add(buttonPanel, 2, row);

            addButton.Click += AddJdfFolder;
            removeButton.Click += RemoveJdfFolder;

            row++;

            AddField(panel, ref row,
                "StopDataFile", true);

            AddField(panel, ref row,
                "LogFolder", false);

            AddField(panel, ref row,
                "OutputFolder", false);

            AddField(panel, ref row,
                "TrainGtfsFolder", false);

            AddField(panel, ref row,
                "BusToBusTransfersFile", true);

            AddField(panel, ref row,
                "TrainToBusTransfersFile", true);

            AddField(panel, ref row,
                "BusNetworkFile", true);

            AddField(panel, ref row,
                "TrolleybusNetworkFile", true);

            var saveButton = new Button
            {
                Text = "Uložit",
                Width = 150,
                Height = 40
            };

            saveButton.Click += SaveClick;

            panel.Controls.Add(saveButton, 1, row);

            LoadConfig();
        }

        private void AddField(
            TableLayoutPanel panel,
            ref int row,
            string propertyName,
            bool file)
        {
            panel.Controls.Add(
                new Label
                {
                    Text = propertyName,
                    AutoSize = true
                },
                0,
                row);

            var textbox = new TextBox
            {
                Width = 500
            };

            fields[propertyName] = textbox;

            panel.Controls.Add(textbox, 1, row);

            var button = new Button
            {
                Text = "..."
            };

            button.Click += (_, _) =>
            {
                if (file)
                {
                    using OpenFileDialog d = new();

                    if (d.ShowDialog() == DialogResult.OK)
                        textbox.Text = d.FileName;
                }
                else
                {
                    using FolderBrowserDialog d = new();

                    if (d.ShowDialog() == DialogResult.OK)
                        textbox.Text = d.SelectedPath;
                }
            };

            panel.Controls.Add(button, 2, row);

            row++;
        }

        private void AddJdfFolder(
            object? sender,
            EventArgs e)
        {
            using FolderBrowserDialog d = new();

            if (d.ShowDialog() == DialogResult.OK)
                jdfList.Items.Add(d.SelectedPath);
        }

        private void RemoveJdfFolder(
            object? sender,
            EventArgs e)
        {
            if (jdfList.SelectedItem != null)
                jdfList.Items.Remove(
                    jdfList.SelectedItem);
        }

        private void LoadConfig()
        {
            if (!File.Exists(ConfigFile))
            {
                MessageBox.Show(
                    $"Soubor {ConfigFile} nebyl nalezen.");

                return;
            }

            try
            {
                settings =
                    JsonSerializer.Deserialize<AppSettings>(
                        File.ReadAllText(ConfigFile))
                    ?? new();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                settings = new();
            }

            jdfList.Items.Clear();

            if (settings.JdfFolders != null)
            {
                foreach (var f in settings.JdfFolders)
                    jdfList.Items.Add(f);
            }

            fields["StopDataFile"].Text =
                settings.StopDataFile;

            fields["LogFolder"].Text =
                settings.LogFolder;

            fields["OutputFolder"].Text =
                settings.OutputFolder;

            fields["TrainGtfsFolder"].Text =
                settings.TrainGtfsFolder;

            fields["BusToBusTransfersFile"].Text =
                settings.BusToBusTransfersFile;

            fields["TrainToBusTransfersFile"].Text =
                settings.TrainToBusTransfersFile;

            fields["BusNetworkFile"].Text =
                settings.BusNetworkFile;

            fields["TrolleybusNetworkFile"].Text =
                settings.TrolleybusNetworkFile;
        }

        private void SaveClick(
            object? sender,
            EventArgs e)
        {
            settings.JdfFolders =
                jdfList.Items
                    .Cast<string>()
                    .ToArray();

            settings.StopDataFile =
                fields["StopDataFile"].Text;

            settings.LogFolder =
                fields["LogFolder"].Text;

            settings.OutputFolder =
                fields["OutputFolder"].Text;

            settings.TrainGtfsFolder =
                fields["TrainGtfsFolder"].Text;

            settings.BusToBusTransfersFile =
                fields["BusToBusTransfersFile"].Text;

            settings.TrainToBusTransfersFile =
                fields["TrainToBusTransfersFile"].Text;

            settings.BusNetworkFile =
                fields["BusNetworkFile"].Text;

            settings.TrolleybusNetworkFile =
                fields["TrolleybusNetworkFile"].Text;

            File.WriteAllText(
                ConfigFile,
                JsonSerializer.Serialize(
                    settings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

            MessageBox.Show(
                "Konfigurace uložena.");
        }
    }
}
