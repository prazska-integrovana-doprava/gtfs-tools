using CsvSerializer;
using System.Collections.Generic;

namespace StopProcessor
{
    /// <summary>
    /// Překládá názvy se zkratkami na názvy bez zkratek (v CSV souboru má vypsaná rozvinutí jednotlivých zkratek)
    /// </summary>
    public class FullNamesProvider
    {
        private List<RenamePair> renamePairs;

        /// <summary>
        /// Načte data z CSV souboru obsahujícího dvojice (zkratka, plný název)
        /// </summary>
        /// <param name="reader"></param>
        public void Load(string fullNamesFileName)
        {
            var renamePairsRaw = CsvFileSerializer.DeserializeFile<RenamePair>(fullNamesFileName, ';');
            renamePairs = new List<RenamePair>();
            
            foreach (var renamePair in renamePairsRaw)
            {
                renamePair.LongVersion = renamePair.LongVersion.Replace('+', ' ');
                
                renamePairs.Add(renamePair);

                // pokud začíná malým písmenem, tak ještě verzi s velkým písmenem
                if (char.IsLower(renamePair.ShortVersion[0]))
                {
                    renamePairs.Add(new RenamePair
                    {
                        ShortVersion = char.ToUpper(renamePair.ShortVersion[0]) + renamePair.ShortVersion.Substring(1),
                        LongVersion = char.ToUpper(renamePair.LongVersion[0]) + renamePair.LongVersion.Substring(1),
                    });
                }
            }
        }

        /// <summary>
        /// Vrátí plný název zastávky bez zkratek
        /// </summary>
        /// <param name="shortName">Název se zkratkami</param>
        public string Resolve(string shortName)
        {
            var result = shortName;

            foreach(var renamePair in renamePairs)
            {
                result = result.Replace(renamePair.ShortVersion, renamePair.LongVersion);
            }

            return result.TrimEnd();
        }
    }
}
