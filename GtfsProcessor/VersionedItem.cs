using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    // TODO možná trochu overkill, když se používá pouze pro TripPersistentDb, ale funguje to, tak do toho nehrabu

    /// <summary>
    /// Reprezentuje datumové verze jednoho objektu, klíčem je <see cref="DateTime"/>.
    /// </summary>
    /// <typeparam name="T">Typ verzovaného objektu</typeparam>
    public class VersionedItemByDate<T> : VersionedItemBase<DateTime, T> where T : class
    {
        public VersionedItemByDate() : base()
        {
        }

        public VersionedItemByDate(T value, DateTime dayFrom) : base(value, dayFrom)
        {
        }
    }


    public class VersionedItemBase<K, T> where K : struct, IComparable<K>, IEquatable<K> where T : class
    {
        // záznam o jedné verzi
        private class Version
        {
            public K DayFrom { get; set; }
            public T Value { get; set; }
        }

        private List<Version> versions;

        public VersionedItemBase()
        {
            versions = new List<Version>();
        }

        public VersionedItemBase(T value, K dayFrom)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            versions = new List<Version>() { new Version() { DayFrom = dayFrom, Value = value } };
        }

        // najde verzi pro dané datum
        private int FindVersionIndex(K referenceDate)
        {
            for (int i = versions.Count - 1; i >= 0; i--)
            {
                if (versions[i].DayFrom.CompareTo(referenceDate) <= 0)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Přidá novou verzi objektu. Pokud již ke stejnému datu existuje, vyhodí výjimku.
        /// </summary>
        /// <param name="value">Objekt (verze)</param>
        /// <param name="dayFrom">Datum, od kdy platí</param>
        public void AddVersion(T value, K dayFrom)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            int targetIndex = FindVersionIndex(dayFrom);
            if (targetIndex >= 0 && versions[targetIndex].DayFrom.Equals(dayFrom))
                throw new InvalidOperationException("Cannot have two versions for the same date");

            // +1, našlo nám verzi začínající před zadaným dnem, chceme přidat ZA tuto verzi
            versions.Insert(targetIndex + 1, new Version() { DayFrom = dayFrom, Value = value });
        }

        /// <summary>
        /// Vrací true, pokud již v seznamu verzí existuje verze začínající zadaným datem <paramref name="dayFrom"/>.
        /// </summary>
        /// <param name="dayFrom">Datum počátku platnosti dané verze</param>
        /// <returns>True, pokud již existuje, jinak false.</returns>
        public bool AlreadyContainsVersion(K dayFrom)
        {
            int targetIndex = FindVersionIndex(dayFrom);
            return (targetIndex >= 0 && versions[targetIndex].DayFrom.Equals(dayFrom));
        }

        /// <summary>
        /// Smaže verzi pro dané datum (případně nejbližší nižší)
        /// </summary>
        /// <param name="date">Referenční datum</param>
        public void RemoveVersion(K date)
        {
            int i = FindVersionIndex(date);
            versions.RemoveAt(i);
        }

        /// <summary>
        /// Vrátí verzi pro dané referenční datum. Lze zadat null, pokud mi stačí libovolná verze,
        /// v takovém případě vrací <see cref="GetFirstVersion"/>.
        /// </summary>
        /// <param name="date">Referenční datum</param>
        /// <returns>Verze objektu (nebo null pokud neexistuje)</returns>
        public T GetVersion(K? date)
        {
            if (date == null)
            {
                return GetFirstVersion();
            }
            else
            {
                int targetIndex = FindVersionIndex(date.Value);
                if (targetIndex >= 0)
                    return versions[targetIndex].Value;
                else
                    return default(T);
            }
        }

        /// <summary>
        /// Vrací první datumovou verzi.
        /// </summary>
        /// <param name="filterPredicate">Podmínka na první vrácený záznam</param>
        /// <returns>První verze zastávky, pokud žádná není, nebo žádná neodpovídá filtru, vrací default(T).</returns>
        public T GetFirstVersion(Func<T, bool> filterPredicate = null)
        {
            if (filterPredicate != null)
                return versions.FirstOrDefault(ver => filterPredicate(ver.Value))?.Value;
            else
                return versions.FirstOrDefault()?.Value;
        }
        
        /// <summary>
        /// Vrátí všechny verze objektu.
        /// </summary>
        /// <returns>Všechny verze objektu</returns>
        public IEnumerable<T> AllVersions()
        {
            return versions.Select(ver => ver.Value);
        }
    }
}
