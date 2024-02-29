using CommonLibrary;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AswModel.Extended
{
    /// <summary>
    /// Obecná kolekce objektů verzovaných pomocí bitmapy. Používá se pro linky, zastávky, trasy, prostě obecně věci, co mají v ASW JŘ platnosti.
    /// </summary>
    public class BaseCollectionOfVersionedItems<TKey, TValue> : IEnumerable<VersionedItemByBitmap<TValue>>, IEnumerable where TValue : class, IVersionableByBitmap
    {
        protected Dictionary<TKey, VersionedItemByBitmap<TValue>> items;

        public BaseCollectionOfVersionedItems()
        {
            items = new Dictionary<TKey, VersionedItemByBitmap<TValue>>();
        }

        /// <summary>
        /// Přidá položku (linku/zastávku/etc.) do databáze. Položka nesmí již být v databázi přítomná, ani v jiné verzi, jinak je vyhozena výjimka
        /// </summary>
        /// <param name="key">Identifikátor objektu (číslo linky/zastávky/etc.)</param>
        /// <param name="item">Nový objekt (linka/zastávka/etc.)</param>
        /// <returns>True, pokud se podařilo verzi vytvořit a objekt přidat, jinak false</returns>
        public bool AddFirstVersion(TKey key, TValue item)
        {
            var itemVersioned = VersionedItemByBitmap<TValue>.Construct(item);
            if (itemVersioned != null)
            {
                items.Add(key, itemVersioned);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Přidá novou verzi objektu (linky/zastávky/etc.). Pokud objekt ještě není v databázi, vytvoří nový záznam. Pokud již je v databázi a existuje
        /// verze shodná (podle <paramref name="equalityCompareru"/>), pouze je prodloužena její platnost. Pokud shodná verze neexistuje, přidá se nová.
        /// </summary>
        /// <param name="key">Identifikátor objektu (číslo linky/zastávky/etc.)</param>
        /// <param name="item">Nový objekt (resp. verze linky/zastávky/etc.)</param>
        /// <param name="equalityComparer">Funkce porovnávající dvě instance objektu na shodu. Lze zadat null, pak se použije Equals.</param>
        /// <param name="mergeFunction">Funkce, která ze dvou záznamů definuje jeden společný (přijme vždy dva objekty, které <paramref name="equalityComparer"/> prohlásí za shodné). Lze zadat null, pak se vždy použije první objekt.</param>
        /// <returns>True, pokud byl objekt přidán, false pokud nikoliv (kvůli překryvu platností, který není povolen).</returns>
        public bool AddOrMergeVersion(TKey key, TValue item, Func<TValue, TValue, bool> equalityComparer = null, Func<TValue, TValue, TValue> mergeFunction = null)
        {
            if (equalityComparer == null) equalityComparer = DefaultEqualityComparer;
            if (mergeFunction == null) mergeFunction = DefaultMergeFunction;

            var versions = Find(key);

            if (versions != null)
            {
                return versions.AddOrMergeVersion(item, equalityComparer, mergeFunction);
            }
            else
            {
                return AddFirstVersion(key, item);
            }
        }

        // výchozí funkce pro AddOrMergeVersion parametr equalityComparer
        private static bool DefaultEqualityComparer(TValue first, TValue second)
        {
            return first.Equals(second);
        }

        // výchozí funkce pro AddOrMergeVersion parametr mergeFunction
        private static TValue DefaultMergeFunction(TValue first, TValue second)
        {
            return first;
        }

        /// <summary>
        /// Najde objekt (linku/zastávku/etc.) podle klíče (číslo linky/zastávky/etc.). Vrací kolekci všech verzí objektu nebo null,
        /// pokud není objekt v databázi přítomen.
        /// </summary>
        /// <param name="key">Identifikátor objektu (číslo linky/zastávky/etc.)</param>
        /// <returns>Nalezený objekt (linka/zastávka/etc.) nebo null, pokud v databázi není</returns>
        public VersionedItemByBitmap<TValue> Find(TKey key)
        {
            VersionedItemByBitmap<TValue> version;
            if (items.TryGetValue(key, out version))
            {
                return version;
            }

            return null;
        }

        /// <summary>
        /// Najde konkrétní verzi objektu (linky/zastávky/etc.), která platí pro zadanou bitmapu, podle klíče (číslo linky/zastávky/etc.).
        /// Vrací konkrétní verzi objektu nebo null, pokud objekt v dané verzi neexistuje.
        /// </summary>
        /// <param name="key">Identifikátor objektu (číslo linky/zastávky/etc.)</param>
        /// <param name="bitmap">Bitmapa, ve které dny musí objekt (resp. jeho konkrétní verze) platit</param>
        /// <returns>Nalezený objekt (konkrétní verze linky/zastávky/etc.), nebo null, pokud není v databázi, nebo žádná verze nesplňuje</returns>
        public TValue Find(TKey key, ServiceDaysBitmap bitmap)
        {
            var versions = Find(key);
            if (versions != null)
            {
                return versions.FindByServiceDays(bitmap);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Najde konkrétní verzi objektu (linky/zastávky/etc.), která platí pro zadanou bitmapu, podle klíče (číslo linky/zastávky/etc.).
        /// Do <paramref name="result"/> ukládá konkrétní verzi objektu. Pokud neexistuje hledaná verze, ale najde se jiná vhodná (viz <see cref="VersionedItemByBitmap{T}.FindByServiceDaysTolerant(ServiceDaysBitmap, out T)"/>),
        /// uloží se do <paramref name="result"/> tato verze a vrací se false. Pokud neexistuje žádná verze, vrací se false a <paramref name="result"/> obsahuje null
        /// </summary>
        /// <param name="key">Identifikátor objektu (číslo linky/zastávky/etc.)</param>
        /// <param name="bitmap">Bitmapa, ve které dny musí objekt (resp. jeho konkrétní verze) platit</param>
        /// <param name="result">Výsledek, kam se ukládá nalezený objekt</param>
        /// <returns>True, pokud byl nalezen objekt platný po celou zadanou bitmapu, jinak false.</returns>
        public bool FindOrDefault(TKey key, ServiceDaysBitmap bitmap, out TValue result)
        {
            var versions = Find(key);
            if (versions != null)
            {
                return versions.FindByServiceDaysTolerant(bitmap, out result);
            }
            else
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Odstraní záznam
        /// </summary>
        /// <param name="key">Identifikátor objektu (číslo linky/zastávky/etc.)</param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            return items.Remove(key);
        }

        /// <summary>
        /// Vrátí všechny objekty v databázi
        /// </summary>
        public IEnumerable<VersionedItemByBitmap<TValue>> GetAllItems()
        {
            return items.Values;
        }

        /// <summary>
        /// Vrátí všechny verze všech objektů v databázi
        /// </summary>
        public IEnumerable<TValue> GetAllItemsVersions()
        {
            foreach (var item in GetAllItems())
            {
                foreach (var version in item.AllVersions())
                {
                    yield return version;
                }
            }
        }

        public IEnumerator<VersionedItemByBitmap<TValue>> GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
