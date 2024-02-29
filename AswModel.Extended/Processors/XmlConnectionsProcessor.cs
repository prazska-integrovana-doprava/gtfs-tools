using System.Collections.Generic;
using JR_XML_EXP;
using System;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává soubor s XML feedem z ASW JŘ <see cref="DavkaJR"/>. K tomu používá ostatní processory.
    /// 
    /// Výsledek se ukládá do jedné <see cref="TheAswDatabase"/>. Prvky, které nemají unikátní ID v rámci celé databáze ASW JŘ, ale pouze
    /// v rámci jednoho souboru (např. spoje), se ukládají po souborech do <see cref="TheAswDatabase.FeedFiles"/>
    /// </summary>
    class XmlConnectionsProcessor
    {
        /// <summary>
        /// Uloží do databáze <paramref name="db"/> prvky z feedu. Feed může být kompletní, může ale klidně obsahovat pouze jednu trakci, nebo souvislou část sítě
        /// (lze dělit až na oběhy, ale oběhy musí být pospolu). Metodu lze volat opakovaně a přinačítat do databáze další data, všechny soubory ale musí mít
        /// shodné <see cref="DavkaJR.DatumOd"/> a <see cref="DavkaJR.DatumDo"/>
        /// 
        /// Objekty, které mají unikátní ID v rámci celé databáze ASW JŘ jsou uloženy přímo v <see cref="TheAswDatabase"/>.
        /// Objekty, které takové unikátní ID nemají a mají unikátní ID pouze v rámci jednoho feed souboru jsou uloženy do <see cref="TheAswDatabase.FeedFiles"/>,
        /// pro každou <see cref="DavkaJR"/> vznikne v <see cref="TheAswDatabase.FeedFiles"/> jeden záznam.
        /// </summary>
        /// <param name="db">Databáze, kam se mají prvky přidat. Může již obsahovat některé načtené položky z dřívějška.</param>
        /// <param name="dataRoot">Načtený XML export z ASW JŘ</param>
        /// <param name="fileName">Název souboru, který načítáme (čistě pro informaci)</param>
        /// <param name="processNonpublicTrips">True, pokud mají být zpracovány i neveřejné a manipulační spoje</param>
        public void Process(TheAswDatabase db, DavkaJR dataRoot, string fileName, bool processNonpublicTrips)
        {
            var lastDay = (dataRoot.DatumDo - dataRoot.DatumOd).Days;
            if (db.GlobalStartDate == new DateTime())
            {
                db.GlobalStartDate = dataRoot.DatumOd;
                db.GlobalLastDay = lastDay;
            }
            else if (db.GlobalStartDate != dataRoot.DatumOd || db.GlobalLastDay != lastDay)
            {
                throw new Exception($"Nekompatibilní období exportů. Konflikt: {db.GlobalStartDate}+{db.GlobalLastDay} vs. {dataRoot.DatumOd}+{lastDay} (soubor {fileName})");
            }

            ProcessElements(new AgencyProcessor(db), dataRoot.Dopravci);
            ProcessElements(new TariffSystemProcessor(db), dataRoot.IntegrovaneSystemy);
            ProcessElements(new StopProcessor(db), dataRoot.Zastavky); // potřebuje integrovane systemy
            ProcessElements(new LineProcessor(db), dataRoot.Linky); // potřebuje dopravce
            ProcessElements(new VehicleTypeProcessor(db), dataRoot.TypyVozu);
            ProcessElements(new GraphProcessor(db), dataRoot.Grafikony);
            ProcessElements(new ShapeProcessor(db), dataRoot.Trasy); // potřebuje zastávky

            var feedFile = new AswSingleFileFeed(fileName);
            var tripSequenceProcessor = new RunProcessor(feedFile, db);
            ProcessElements(tripSequenceProcessor, dataRoot.Obehy);
            ProcessElements(new RemarksProcessor(feedFile, db), dataRoot.Poznamky); // potřebuje zastávky a linky
            ProcessElements(new TripProcessor(feedFile, db, tripSequenceProcessor.RunByTripId, processNonpublicTrips), dataRoot.Spoje); // potřebuje zastávky, linky, typy vozů, poznámky, grafy, oběhy i trasy
            // zastavení se načítají rovnou ze spojů

            db.FeedFiles.Add(feedFile);
        }

        private void ProcessElements<T>(IProcessor<T> processor, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                processor.Process(item);
            }
        }
    }
}
