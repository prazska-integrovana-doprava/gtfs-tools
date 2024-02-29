using System;

namespace StopProcessor
{
    /// <summary>
    /// Spravuje nastavení běhu programu
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Root složka pro GTFS data
        /// </summary>
        public string GtfsFolder { get; private set; }

        /// <summary>
        /// Plná cesta ke XML souboru se zastávkami
        /// </summary>
        public string StopsFile { get; private set; }

        /// <summary>
        /// Plná cesta k CSV souboru s překlady zkratek názvů na plné názvy
        /// </summary>
        public string FullNamesTranslationsFile { get; private set; }

        /// <summary>
        /// Plná cesta do složky, kam mají být umístěny soubory se zastávkami (XML, JSON)
        /// </summary>
        public string StopsOutputFolder { get; private set; }

        /// <summary>
        /// Plná cesta do složky, kam se mají ukládat logovací soubory
        /// </summary>
        public string LogFolder { get; private set; }
        
        /// <summary>
        /// Načte nastavení z příkazové řádky
        /// </summary>
        public static Configuration Load(string[] args)
        {
            var result = new Configuration();
            result.Init(args);
            return result;
        }

        protected void Init(string[] args)
        {
            if (args.Length != 5)
            {
                throw new Exception("Wrong number of arguments. Correct usage: StopProcessor.exe <GTFS> <Stops.xml> <StopTranslations.csv> <OutputFolder> <LogFolder>");
            }

            GtfsFolder = args[0];
            StopsFile = args[1];
            FullNamesTranslationsFile = args[2];
            StopsOutputFolder = args[3];
            LogFolder = args[4];
        }
    }
}
