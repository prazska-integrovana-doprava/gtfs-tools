using System.Collections.Generic;

namespace CommonLibrary
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Vrátí hodnotu, je-li v dictionary, jinak null. Odpovídá operaci [] s tím rozdílem, že v případě nepřítomnosti prvku v kolekci
        /// nevyvolává výjimku, ale vrací null.
        /// </summary>
        /// <param name="dict">Dictionary</param>
        /// <param name="key">Klíč k hledané hodnotě</param>
        /// <returns>Nalezená hodnota z dictionary nebo null, pokud žádná není.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return GetValueOrDefault(dict, key, default(TValue));
        }

        /// <summary>
        /// Vrátí hodnotu, je-li v dictionary, jinak <paramref name="defaultValue"/>.
        /// Odpovídá operaci [] s tím rozdílem, že v případě nepřítomnosti prvku v kolekci nevyvolává výjimku, ale vrací <paramref name="defaultValue"/>.
        /// </summary>
        /// <param name="dict">Dictionary</param>
        /// <param name="key">Klíč k hledané hodnotě</param>
        /// <param name="defaultValue">Výchozí hodnota, která se vrátí, pokud prvek v dictionary není.</param>
        /// <returns>Nalezená hodnota z dictionary nebo <paramref name="defaultValue"/>, pokud žádná není.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            TValue result;
            if (dict.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Vrátí hodnotu pro daný klíč, je-li v dictionary. Pokud není, založí nový záznam s <paramref name="defaultValue"/> a vrátí ten.
        /// </summary>
        /// <param name="dict">Dictionary</param>
        /// <param name="key">Klíč k hledané hodnotě</param>
        /// <param name="defaultValue">Výchozí hodnota, která se do dictionary uloží, pokud v ní klíč ještě není</param>
        /// <returns>Nalezená hodnota z dictionary nebo <paramref name="defaultValue"/></returns>
        public static TValue GetValueAndAddIfMissing<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            TValue result;
            if (dict.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                dict.Add(key, defaultValue);
                return defaultValue;
            }
        }

        /// <summary>
        /// Vloží do dictionary několik hodnot.
        /// </summary>
        /// <param name="dict">Dictionary</param>
        /// <param name="itemsToAdd">Prvky, které mají být vloženy</param>
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> itemsToAdd)
        {
            foreach (var item in itemsToAdd)
            {
                dict.Add(item.Key, item.Value);
            }
        }
    }
}
