using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLibrary
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Ověří, zda kolekce obsahuje alespoň dva prvky.
        /// </summary>
        /// <typeparam name="T">Typ prvku kolekce</typeparam>
        /// <param name="collection">Kolekce</param>
        /// <returns>True, pokud obsahuje alespoň dva prvky, jinak false.</returns>
        public static bool Any2<T>(this IEnumerable<T> collection)
        {
            int num = 0;
            foreach (var item in collection)
            {
                ++num;
                if (num == 2)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Ověří, zda kolekce obsahuje alespoň dva prvky splňující podmínku (nemusí být v kolekci za sebou).
        /// </summary>
        /// <typeparam name="T">Typ prvku kolekce</typeparam>
        /// <param name="collection">Kolekce</param>
        /// <param name="predicate">Podmínka, kterou musí oba prvky splňovat</param>
        /// <returns>True, pokud obsahuje alespoň dva prvky, jinak false.</returns>
        public static bool Any2<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int num = 0;
            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    ++num;
                    if (num == 2)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ověří, zda kolekce obsahuje alespoň dva prvky splňující podmínku (nemusí být v kolekci za sebou).
        /// </summary>
        /// <typeparam name="T">Typ prvku kolekce</typeparam>
        /// <param name="collection">Kolekce</param>
        /// <param name="predicateFirst">Podmínka, kterou musí splňovat první prvek</param>
        /// <param name="predicateSecond">Podmínka, kterou musí splňovat druhý prvek</param>
        /// <returns>True, pokud obsahuje alespoň dva prvky, jinak false.</returns>
        public static bool Any2<T>(this IEnumerable<T> collection, Func<T, bool> predicateFirst, Func<T, bool> predicateSecond)
        {
            int num = 0;
            foreach (var item in collection)
            {
                if (num == 0 && predicateFirst(item) || num == 1 && predicateSecond(item))
                {
                    ++num;
                    if (num == 2)
                        return true;
                }
            }

            return false;
        }

        public interface ICompareAndMerge<T>
        {
            /// <summary>
            /// Porovná dva prvky a pokud jsou shodné, vrací true.
            /// </summary>
            bool AreIdentical(T first, T second);

            /// <summary>
            /// Sloučí dva prvky do prvního z nich. First následně bude dále žít, second by měl být zapomenut.
            /// Předpokládá se, že oba prvky jsou stejné (<see cref="AreIdentical(T, T)"/> vrátí true).
            /// </summary>
            void MergeSecondIntoFirst(T first, T second);
        }

        /// <summary>
        /// Sloučí stejné položky v seznamu. Vrací nový seznam se sloučenými položkami.
        /// </summary>
        public static IEnumerable<T> MergeIdentical<T>(this IEnumerable<T> collection, ICompareAndMerge<T> compareAndMerge)
        {
            var remainingItems = new List<T>(collection);

            while (remainingItems.Any())
            {
                var item = remainingItems.First();
                remainingItems.RemoveAt(0);

                var identicalItems = remainingItems.Where(t => compareAndMerge.AreIdentical(item, t)).ToArray();
                foreach (var identicalItem in identicalItems)
                {
                    remainingItems.Remove(identicalItem);
                    compareAndMerge.MergeSecondIntoFirst(item, identicalItem);
                }

                yield return item;
            }
        }

        /// <summary>
        /// Všechny výskyty položky <paramref name="toReplace"/> jsou v seznamu nahrazeny položkou <paramref name="replaceBy">.
        /// </summary>
        public static void Replace<T>(this IList<T> list, T toReplace, T replaceBy)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(toReplace))
                {
                    list[i] = replaceBy;
                }
            }
        }
    }
}
