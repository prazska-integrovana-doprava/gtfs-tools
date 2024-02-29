using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Schraňuje všechny záznamy stejného objektu, které se liší v datech platnosti (bitmapě). Uložené záznamy se nesmí platností překrývat.
    /// 
    /// Používá se pro zastávky <see cref="Stop"/>, linky <see cref="Route"/> a trasy <see cref="ShapeFragment"/> (obecně ASW objekty s platností).
    /// Například když se zastávka do 1.12. jmenuje X a od 2.12. se jmenuje Y, jsou ve zdrojových datech dva záznamy lišící se kalendářem
    /// platnosti <see cref="ServiceDaysBitmap"/>. Zde jsou tyto záznamy pohromadě.
    /// 
    /// Instance tedy obsahuje vždy minimálně jeden záznam (a nejčastěji je to právě jeden s bitmapou od začátku do konce).
    /// 
    /// Obsahuje i metody na slučování shodných verzí. Často se totiž u objektů mění jen bezvýznamné parametry a nechceme objekty zbytečně trhat
    /// (například kvůli tomu vyrábět duplicitní záznamy zastávek nebo linek v GTFS). Nebo se objekt mění na pár dní a pak zpět, ovšem export ASW
    /// vyexportuje tři verze, kde první a třetí jsou vlastně jedna a tatáž. Opět s cílem záznamy zbytečně netrhat.
    /// </summary>
    public class VersionedItemByBitmap<T> where T : class, IVersionableByBitmap
    {
        // řazeno podle prvního dne platnosti
        private SortedList<RelativeDate, T> versions;

        /// <summary>
        /// Vytvoří novou instanci s novým záznamem (pokud se vytvoření nezdaří, vrátí null).
        /// </summary>
        /// <param name="initVersion">Iniciální verze</param>
        /// <returns>Objekt s iniciální verzí nebo null</returns>
        public static VersionedItemByBitmap<T> Construct(T initVersion)
        {
            var result = new VersionedItemByBitmap<T>();
            if (result.AddVersion(initVersion))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        protected VersionedItemByBitmap()
        {
            versions = new SortedList<RelativeDate, T>();
        }

        /// <summary>
        /// Přidá záznam. Ten se nesmí platností překrývat s jiným již uloženým, jinak operace skončí neúspěchem.
        /// </summary>
        /// <param name="item">Položka</param>
        /// <returns>True, pokud byl přidán, false pokud nikoliv (kvůli překryvu s již existující položkou)</returns>
        public bool AddVersion(T item)
        {
            if (item.ServiceAsBits.IsEmpty || FindByAnyServiceDay(item.ServiceAsBits).Any())
            {
                // záznam by se překrýval s nějakým již existujícím
                return false;
            }

            versions.Add(item.ServiceAsBits.GetFirstDayOfService(), item);
            return true;
        }
                
        /// <summary>
        /// Pokusí se položku zamergovat do nějaké již existující (ponechá původní položku a jen jí rozšíří kalendář). Pokud se to nepodaří, normálně ji přidá.
        /// </summary>
        /// <param name="item">Položka k přidání nebo zamergování</param>
        /// <param name="equalityComparer">Delegát, který porovnává záznamy</param>
        /// <param name="mergeFunction">Delegát, který ze dvou záznamů vybere (a případně doupraví) jeden</param>
        /// <returns>True, pokud se podařilo záznam přidat/zamergeovat, jinak false (kvůli překryvu s již existující položkou)</returns>
        public bool AddOrMergeVersion(T item, Func<T, T, bool> equalityComparer, Func<T, T, T> mergeFunction)
        {
            // kontrolu musíme provést již nyní, ne později, jinak se může stát, že záznam odebereme, prodloužíme a už nepřidáme, čímž přijdeme i o původní záznam
            if (item.ServiceAsBits.IsEmpty || FindByAnyServiceDay(item.ServiceAsBits).Any())
            {
                // záznam by se překrýval s nějakým již existujícím
                if (FindByAnyServiceDay(item.ServiceAsBits).All(sd => item.ServiceAsBits.IsSubsetOf(sd.ServiceAsBits) && equalityComparer(sd, item)))
                {
                    // ale má to stejnou platnost a je to shodné nebo podmnožinové se záznamem, co přidáváme, tak to budeme tolerovat (zřejmě jde o identický záznam z více souborů)
                    // podmnožiny musíme povolit kvůli tomu, že se záznamy mergují, čili zpětně už nepoznáme, jestli je záznam s něčím shodný
                    return true;
                }

                return false;
            }

            foreach (var version in versions)
            {
                if (equalityComparer(item, version.Value))
                {
                    // musíme odebrat a přidat znovu, aby se to správně zatřídilo
                    versions.Remove(version.Key);
                    var mergedResult = mergeFunction(version.Value, item);
                    mergedResult.ExtendServiceDays(item.ServiceAsBits); // lepší je přimergovat kalendář k současné položce
                    return AddVersion(mergedResult);
                }
            }

            // není s čím zamergovat, přidáme jako nový
            return AddVersion(item);
        }

        /// <summary>
        /// Vrátí záznam, který platností pokrývá celou zadanou bitmapu. Z principu, že záznamy se nesmí překrývat, může být takový záznam nejvýše jeden.
        /// </summary>
        /// <param name="bitmap">Bitmapa platnosti</param>
        /// <returns>Záznam, který je nadmnožinou zadané bitmapy, pokud takový existuje, jinak null.</returns>
        public T FindByServiceDays(ServiceDaysBitmap bitmap)
        {
            foreach (var version in versions)
            {
                if (bitmap.IsSubsetOf(version.Value.ServiceAsBits))
                {
                    return version.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Vrátí záznam (do <paramref name="result"/>), který platností pokrývá zadanou bitmapu. Z principu, že záznamy se nesmí překrývat, může být takový záznam nejvýše jeden.
        /// Pokud takový záznam existuje, metoda jej zapíše do <paramref name="result"/> a vrátí true.
        /// Pokud takový záznam neexistuje, metoda zapíše do <paramref name="result"/> záznam, který platí v první den platnosti <paramref name="bitmap"/> a vrátí false.
        /// Pokud ani takový záznam neexistuje, metoda zapíše do <paramref name="result"/> záznam, který nejblíže předchází prvnímu dni platnosti <paramref name="bitmap"/> a vrátí false.
        /// V případě, že bitmapa začíná před první verzí, bude v <paramref name="result"/> první verze a vrací se false.
        /// </summary>
        /// <param name="bitmap">Bitmapa platnosti</param>
        /// <param name="result">Záznam, který nejlépe odpovídá zadanému datu.</param>
        /// <returns>True, pokud záznam odpovídá platností bitmapy, false, pokud byl pouze odvozen.</returns>
        public bool FindByServiceDaysTolerant(ServiceDaysBitmap bitmap, out T result)
        {
            result = FindByServiceDays(bitmap);
            if (result != null)
            {
                return true;
            }

            // nenašli jsme na přímou shodu, zkusíme tedy najít vyhovující záznam jako ten, který platil jako poslední v začátku platnosti zadané bitmapy
            var date = bitmap.GetFirstDayOfService();
            T prev = null;
            foreach (var version in versions)
            {
                if (version.Value.ServiceAsBits.GetFirstDayOfService() > date)
                {
                    // záznamy jsou seřazené, takže je-li tato podmínka splněna, tak všechny další záznamy jsou platné až po zadaném datu,
                    // čili to znamená, že záznam se zadanou platností se v seznamu nenachází
                    if (prev != null)
                    {
                        result = prev;
                        return false;
                    }
                    else
                    {
                        result = FirstVersion();
                        return false;
                    }
                }

                if (version.Value.ServiceAsBits[date])
                {
                    result = version.Value;
                    return false;
                }
            }

            // pokud jsme se dostali sem, tak poslední záznam nedosahuje platnosti až do 'date', ale je nejblíže, tedy jej vrátíme
            result = versions.Last().Value;
            return false;
        }

        /// <summary>
        /// Vrátí všechny záznamy, které jsou platné v alespoň jeden ze dnů zadaných bitmapou.
        /// </summary>
        /// <param name="bitmap">Bitmapa platnosti</param>
        /// <returns>Všechny záznamy s neprázdným průnikem se zadanou bitmapou</returns>
        public IEnumerable<T> FindByAnyServiceDay(ServiceDaysBitmap bitmap)
        {
            foreach (var version in versions)
            {
                if (!version.Value.ServiceAsBits.Intersect(bitmap).IsEmpty)
                {
                    yield return version.Value;
                }
            }
        }

        /// <summary>
        /// Odvodí pro verzi maximální bitmapu, na kterou by šla platnost objektu protáhnout, aniž by byla v kolizi s ostatními verzemi,
        /// přitom se snaží nevytvářet díry, tedy rozšiřuje pouze k nejbližším sousedům (předchozí a následující verzi).
        /// </summary>
        /// <param name="version">Verze, musí být obsažena, jinak se vyhodí výjimka</param>
        /// <returns>Teoretická maximální bitmapa</returns>
        public ServiceDaysBitmap ExtendBitmapToMaximum(T version)
        {
            var bitmapCopy = new ServiceDaysBitmap(version.ServiceAsBits);
            var versionsList = AllVersions().ToList();
            var indexOfVersion = versionsList.IndexOf(version);

            var lastDayOfPreviousIndex = indexOfVersion > 0 ? (int) versionsList[indexOfVersion - 1].ServiceAsBits.GetLastDayOfService() : -1;
            var firstDayOfNextIndex = indexOfVersion < versionsList.Count - 1 ? (int) versionsList[indexOfVersion + 1].ServiceAsBits.GetFirstDayOfService() : bitmapCopy.Length;

            for (int i = lastDayOfPreviousIndex + 1; i < firstDayOfNextIndex; i++)
            {
                bitmapCopy[i] = true;
            }

            return bitmapCopy;
        }

        /// <summary>
        /// Omezí bitmapu tak, aby ji bylo možné vložit jako novou verzi (čili odstraní všechny konflikty s již platnými záznamy)
        /// </summary>
        /// <param name="bitmap">Bitmapa k omezení</param>
        /// <returns>Podmnožina zadané bitmapy, kterou lze přidat, nebo prázdná bitmapa</returns>
        public ServiceDaysBitmap LimitBitmapToMaximumAllowed(ServiceDaysBitmap bitmap)
        {
            var bitmapCopy = new ServiceDaysBitmap(bitmap);
            foreach (var version in versions)
            {
                bitmapCopy = bitmapCopy.Subtract(bitmap);
            }

            return bitmapCopy;
        }

        /// <summary>
        /// Vrátí všechny verze objektu seřazené podle začátku platnosti
        /// </summary>
        public IEnumerable<T> AllVersions()
        {
            return versions.Values;
        }

        /// <summary>
        /// Vrátí první verzi (platnou nejdříve). Je zaručeno, že objekt obsahuje vždy alespoň jednu verzi, takže volání nemůže skončit neúspěchem.
        /// </summary>
        /// <returns></returns>
        public T FirstVersion()
        {
            return versions.Values.First();
        }
    }
}
