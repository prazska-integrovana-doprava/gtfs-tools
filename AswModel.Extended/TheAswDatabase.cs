using AswModel.Extended.Logging;
using AswModel.Extended.Processors;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Reprezentuje databázi spojů tvořenou jedním nebo více načtenými soubory.
    /// 
    /// Přímo obsahuje položky, které mají unikátní ID v rámci celé databáze ASW JŘ (databázi lze skládat z více souborů dohromady,
    /// data ale musí být rozdělena do disjunktních skupin, nejmenší dělitelnou jednotkou je celý oběh).
    /// 
    /// Objekty, které mají ID unikátní pouze v rámci jednoho souboru (např. spoje), se ukládají pro každý takový soubor zvlášť jednotlivě
    /// do <see cref="FeedFiles"/>.
    /// 
    /// Číselníkové položky jako zastávky, linky a další, které se mohou měnit v průběhu, jsou ukládány verzovaně, 
    /// Více informací viz třída <see cref="VersionedItemByBitmap{T}"/>.
    /// </summary>
    public class TheAswDatabase
    {
        /// <summary>
        /// Počátek exportu
        /// </summary>
        public DateTime GlobalStartDate { get; set; }

        /// <summary>
        /// Počet dní v exportu (poslední den)
        /// </summary>
        public RelativeDate GlobalLastDay { get; set; }

        /// <summary>
        /// Databáze zastávek. Struktura viz třída <see cref="StopDatabase"/>.
        /// </summary>
        public StopDatabase Stops { get; private set; }

        /// <summary>
        /// Databáze linek. Struktura viz třída <see cref="LineDatabase"/>. 
        /// </summary>
        public LineDatabase Lines { get; private set; }

        /// <summary>
        /// Trasy mezizastávkových úseků (dle trajektorií z ASW JŘ)
        /// </summary>
        public ShapeFragmentCollection ShapeFragments { get; private set; }

        /// <summary>
        /// Mapování z typů vozu (z číselníku ASW) do informace o nízkopodlažnosti spoje (ano/ne).
        /// </summary>
        internal IDictionary<int, bool> VehicleTypeIsWheelchairAccessible { get; private set; }

        /// <summary>
        /// Tarifní systémy (indexováno ID, hodnota je zkratka IDS)
        /// </summary>
        internal IDictionary<int, string> TariffSystems { get; private set; }

        /// <summary>
        /// Názvy dopravců v ASW JŘ
        /// </summary>
        public IDictionary<int, AswAgency> Agencies { get; private set; }

        /// <summary>
        /// Grafikony
        /// 
        /// TODO nebude fungovat, pokud bude v jednom souboru generováno najednou více závodů, protože ID grafikonu je unikátní
        /// pouze v rámci závodu. Musí opravit CHAPS, abych u spoje znal i závod a pak zde musím indexovat dvojicí závod+ID grafikonu
        /// </summary>
        public IDictionary<GraphIdAndCompany, Graph> Graphs { get; private set; }

        /// <summary>
        /// Další data, která nemají unikátní ID v rámci celé databáze ASW, ale pouze v rámci jednoho exportního souboru (např. spoje).
        /// Pro každý načtený soubor je zde jeden záznam.
        /// </summary>
        public List<AswSingleFileFeed> FeedFiles { get; private set; }
        
        public TheAswDatabase()
        {
            Stops = new StopDatabase();
            Lines = new LineDatabase();
            ShapeFragments = new ShapeFragmentCollection();
            VehicleTypeIsWheelchairAccessible = new Dictionary<int, bool>();
            TariffSystems = new Dictionary<int, string>();
            Agencies = new Dictionary<int, AswAgency>();
            Graphs = new Dictionary<GraphIdAndCompany, Graph>();
            FeedFiles = new List<AswSingleFileFeed>();
        }

        /// <summary>
        /// Vrátí všechny spoje ze všech souborů
        /// </summary>
        public IEnumerable<Trip> GetAllTrips()
        {
            return FeedFiles.SelectMany(f => f.Trips.GetAllTrips());
        }

        /// <summary>
        /// Vrátí všechny veřejné spoje ze všech souborů
        /// </summary>
        public IEnumerable<Trip> GetAllPublicTrips()
        {
            return FeedFiles.SelectMany(f => f.Trips.GetAllPublicTrips());
        }

        /// <summary>
        /// Sestaví databázi z ASW XML souborů.
        /// </summary>
        /// <param name="processNonpublicTrips">True, pokud mají být načteny a zpracovány též neveřejné a manipulační spoje.</param>
        /// <param name="fileNames">XML soubory s daty</param>
        /// <returns>Načtená databáze</returns>
        public static TheAswDatabase Construct(bool processNonpublicTrips, params string[] fileNames)
        {
            var aswXmlFiles = new Tuple<string, DavkaJR>[fileNames.Length];
            for (int i = 0; i < fileNames.Length; i++)
            {
                aswXmlFiles[i] = new Tuple<string, DavkaJR>(fileNames[i], AswXmlSerializer.Deserialize(fileNames[i]));
            }

            return Construct(processNonpublicTrips, aswXmlFiles);
        }

        /// <summary>
        /// Sestaví databázi z dat ASW XML souborů (vždy dvojice název + načtený obsah). Pokud z nějakého důvodu není potřeba použít tuto variantu, zvažte
        /// variantu <see cref="Construct(string[])"/>, která dělá přesně to samé, ale rovnou z názvů souborů.
        /// 
        /// Všechny XML soubory musí mít stejné období exportu (datum od, datum do) a spoje musí být disjunktně rozděleny,
        /// přičemž nesmí být roztrženy oběhy. Duplicitní záznamy číselníkových položek (linky, zastávky atd.) budou sloučeny
        /// </summary>
        /// <param name="processNonpublicTrips">True, pokud mají být načteny a zpracovány též neveřejné a manipulační spoje.</param>
        /// <param name="aswXmlFiles">Načtené XML soubory (lze načíst pomocí <see cref="AswXmlSerializer.Deserialize(string)"/>).</param>
        /// <returns>Načtená databáze</returns>
        public static TheAswDatabase Construct(bool processNonpublicTrips, params Tuple<string, DavkaJR>[] aswXmlFiles)
        {
            var resultDb = new TheAswDatabase();
            var processor = new XmlConnectionsProcessor();
            foreach (var file in aswXmlFiles)
            {
                processor.Process(resultDb, file.Item2, Path.GetFileNameWithoutExtension(file.Item1), processNonpublicTrips);
            }

            // Route traffic type setter
            foreach (var routeVersion in resultDb.Lines.GetAllItemsVersions())
            {
                routeVersion.InferTrafficTypeFromTrips(Loggers.DataLoggerInstance);
            }

            // Trip block resolver
            foreach (var file in resultDb.FeedFiles)
            {
                file.Trips.ResolveBlocksAndRuns(Loggers.DataLoggerInstance);
            }

            return resultDb;
        }
    }
}
