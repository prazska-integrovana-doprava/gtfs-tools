using System.Collections.Generic;

namespace AswModel.Extended
{
    /// <summary>
    /// Databáze objektů, které mají unikátní ID pouze v rámci jednoho exportního souboru (spoje a poznámky).
    /// Tyto objekty jsou v každém exportu číslovány vždy od nuly a proto pokud načítáme feed <see cref="TheAswDatabase"/>
    /// z více souborů dohromady, nejde dát úplně vše do kupy, aniž bychom nějak ručně upravovali IDčka například
    /// přičítáním vysokých řádů. Proto jsou tyto objekty zde a instance této třídy je v <see cref="TheAswDatabase"/>
    /// pro každý načtený soubor zvlášť.
    /// 
    /// Objekty, které mají unikátní ID v rámci celé databáze ASW JŘ jsou uloženy přímo v <see cref="TheAswDatabase"/>
    /// </summary>
    public class AswSingleFileFeed
    {
        /// <summary>
        /// Název nebo jiné označení souboru, ze kterého data pochází
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// Databáze spojů. Struktura viz třída <see cref="TripDatabase"/>.
        /// </summary>
        public TripDatabase Trips { get; private set; }

        /// <summary>
        /// Poznámky ke spojům
        /// </summary>
        public IDictionary<int, Remark> Remarks { get; private set; }
                
        public AswSingleFileFeed(string fileId)
        {
            FileId = fileId;
            Trips = new TripDatabase();
            Remarks = new Dictionary<int, Remark>();
        }
    }
}
