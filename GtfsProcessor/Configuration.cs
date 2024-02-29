using IniParser;
using System.Collections.Generic;
using System.IO;

namespace GtfsProcessor
{
    /// <summary>
    /// Spravuje nastavení běhu programu (načítá z INI)
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Root složka pro všechna data
        /// </summary>
        public string HomeFolder { get; private set; }

        /// <summary>
        /// Plné cesty ke XML souborům se spoji
        /// </summary>
        public List<string> TripFiles { get; private set; }

        /// <summary>
        /// True, pokud mají být načteny i neveřejné spoje
        /// </summary>
        public bool ProcessNonpublicTrips { get; private set; }

        /// <summary>
        /// Soubor s dodatečnými záznamy pro stops.txt (hlavně pro paths & levels)
        /// </summary>
        public string AdditionalStopsFileName { get; private set; }

        /// <summary>
        /// Soubor s doplňujícími ručně zadanými přestupními časy
        /// </summary>
        public string AdditionalTransfersFileName { get; private set; }

        /// <summary>
        /// Soubor se všemi fare_rules
        /// </summary>
        public string FareRulesFileName { get; private set; }
        
        /// <summary>
        /// Složka s GTFS soubory s vlakovými jízdními řády
        /// </summary>
        public string TrainGtfsFolder { get; private set; }

        /// <summary>
        /// CSV soubor s trasami metra
        /// </summary>
        public string MetroNetworkFile { get; private set; }

        /// <summary>
        /// Složka, do které se ukládají (a zpětně načítají) data o spojích, aby měly stejná IDčka v navazujících feedech
        /// </summary>
        public string TripPersistentDbFolder { get; private set; }

        /// <summary>
        /// Plná cesta do složky, kam mají být umístěny TXT soubory z GTFS
        /// </summary>
        public string GtfsOutputFolder { get; private set; }

        /// <summary>
        /// Plná cesta do složky, kam se mají ukládat logovací soubory
        /// </summary>
        public string LogFolder { get; private set; }

        /// <summary>
        /// Při záznamu loggery, které podporují Message type se chyba stejného typu zaloguje
        /// max tolikrát, kolik uvádí tato hodnota (výchozí je <see cref="int.MaxValue"/>).
        /// </summary>
        public int MaxSimilarLogRecords { get; private set; }

        /// <summary>
        /// Nebude zapisovat nic do persistentní trip databáze
        /// </summary>
        public bool TripDbAsReadOnly { get; private set; }

        /// <summary>
        /// Seznam linek, které nemají jít do výstupu (pro rychlou záchranu)
        /// </summary>
        public IList<int> IgnoredLines { get; private set; }

        /// <summary>
        /// Minimální počet spojů metra, které musí být načteny, jinak bude export abortován
        /// </summary>
        public int MinimumMetroTrips { get; private set; }

        /// <summary>
        /// Minimální počet spojů tramvají, které musí být načteny, jinak bude export abortován
        /// </summary>
        public int MinimumTramTrips { get; private set; }

        /// <summary>
        /// Minimální počet spojů městských busů, které musí být načteny, jinak bude export abortován
        /// </summary>
        public int MinimumBusTo299Trips { get; private set; }

        /// <summary>
        /// Minimální počet spojů příměstských busů, které musí být načteny, jinak bude export abortován
        /// </summary>
        public int MinimumBusFrom300Trips { get; private set; }

        /// <summary>
        /// Minimální počet spojů vlaků, které musí být načteny, jinak bude export abortován
        /// </summary>
        public int MinimumTrainTrips { get; private set; }

        /// <summary>
        /// Minimální úspěšnost při přidělování trip ID (procento trip ID, které byly recyklovány z minula, tj. nebylo nutné je generovat nové)
        /// </summary>
        public int MinimumTripDatabaseHitPercentage { get; private set; }


        protected Configuration(string dataHomeFolder)
        {
            HomeFolder = dataHomeFolder;
            MaxSimilarLogRecords = int.MaxValue;
            IgnoredLines = new List<int>();
        }

        /// <summary>
        /// Načte data ze zadaného INI souboru. INI soubor je buď specifikován plnou cestou, nebo relativní vzhledem k <see cref="HomeFolder"/>.
        /// </summary>
        /// <param name="configFileName"></param>
        public static Configuration Load(string homeFolder, string configFileName)
        {
            var result = new Configuration(homeFolder);
            result.Init(configFileName);
            return result;
        }

        protected void Init(string configFileName)
        {
            var configFullPath = Path.IsPathRooted(configFileName) ? configFileName : Path.Combine(HomeFolder, configFileName);

            var iniParser = new FileIniDataParser();
            var iniData = iniParser.ReadFile(configFullPath);

            var inputFilesSection = iniData.Sections["InputFiles"];
            TripFiles = new List<string>();
            var tripFileNames = inputFilesSection["TripFiles"];
            foreach (var fileName in tripFileNames.Split(','))
            {
                TripFiles.Add(Path.Combine(HomeFolder, fileName));
            }

            ProcessNonpublicTrips = ParseBool(inputFilesSection["ProcessNonpublicTrips"], false);
            TripPersistentDbFolder = GetFullPath(inputFilesSection["TripPersistentDbFolder"]);
            AdditionalStopsFileName = GetFullPath(inputFilesSection["AdditionalStopsFileName"]);
            AdditionalTransfersFileName = GetFullPath(inputFilesSection["AdditionalTransfersFileName"]);
            FareRulesFileName = GetFullPath(inputFilesSection["FareRulesFileName"]);
            TrainGtfsFolder = GetFullPath(inputFilesSection["TrainGtfsFolder"]);
            MetroNetworkFile = GetFullPath(inputFilesSection["MetroNetworkFile"]);

            var outputFilesSection = iniData.Sections["OutputFiles"];
            GtfsOutputFolder = GetFullPath(outputFilesSection["GtfsOutputFolder"]);

            var logsSection = iniData.Sections["Log"];
            LogFolder = GetFullPath(logsSection["LogFolder"]);
            if (logsSection.ContainsKey("MaxSimilarLogRecords"))
            {
                MaxSimilarLogRecords = int.Parse(logsSection["MaxSimilarLogRecords"]);
            }

            var parametersSection = iniData.Sections["Parameters"];
            if (parametersSection != null)
            {
                TripDbAsReadOnly = ParseBool(parametersSection["TripDbAsReadOnly"], false);
                if (parametersSection["IgnoredLines"] != null)
                {
                    var ignoredLines = parametersSection["IgnoredLines"].Split(',');
                    foreach (var line in ignoredLines)
                    {
                        IgnoredLines.Add(int.Parse(line));
                    }
                }
            }

            var verificationSection = iniData.Sections["Verification"];
            if (verificationSection != null)
            {
                MinimumMetroTrips = ParseInt(verificationSection["MinimumMetroTrips"]);
                MinimumTramTrips = ParseInt(verificationSection["MinimumTramTrips"]);
                MinimumBusTo299Trips = ParseInt(verificationSection["MinimumBusTo299Trips"]);
                MinimumBusFrom300Trips = ParseInt(verificationSection["MinimumBusFrom300Trips"]);
                MinimumTrainTrips = ParseInt(verificationSection["MinimumTrainTrips"]);
                MinimumTripDatabaseHitPercentage = ParseInt(verificationSection["MinimumTripDatabaseHitPercentage"]);
            }
        }

        private string GetFullPath(string fileName)
        {
            return fileName != null ? Path.Combine(HomeFolder, fileName) : null;
        }

        private bool ParseBool(string content, bool defaultValue)
        {
            if (content == null)
                return defaultValue;

            bool result;
            if (!bool.TryParse(content, out result))
                return defaultValue;

            return result;
        }

        private int ParseInt(string content, int defaultValue = 0)
        {
            if (content == null)
                return defaultValue;

            return int.Parse(content);
        }
    }
}
