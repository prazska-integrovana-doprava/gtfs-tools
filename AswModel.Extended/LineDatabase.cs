using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Databáze linek (a spojů). Indexováno číslem linky.
    /// </summary>
    public class LineDatabase : BaseCollectionOfVersionedItems<int, Route>
    {
        /// <summary>
        /// Vrátí od každé linky první verzi, která má nějaké spoje
        /// </summary>
        public IEnumerable<Route> GetUsedLinesFirstVersions()
        {
            foreach (var routeVersions in GetAllItems())
            {
                var firstNonEmpty = routeVersions.AllVersions().FirstOrDefault(r => r.PublicTrips.Any());
                if (firstNonEmpty != null)
                    yield return firstNonEmpty;
            }
        }
    }
}
