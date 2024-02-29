using CsvSerializer.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CsvSerializer
{
    /// <summary>
    /// Zajišťuje serializaci a deserializaci CSV souborů.
    /// </summary>
    /// <remarks>
    /// Serializovat je možné pouze položky primitivních typů <see cref="int"/>, <see cref="string"/>, <see cref="Enum"/>
    /// a třídy implementující <see cref="ICsvSerializable{T}"/>
    /// </remarks>
    public static class CsvFileSerializer
    {
        // oddělovač sloupečků
        internal const char DefaultSeparator = ',';

        internal const string DefaultDateTimeFormat = "yyyyMMdd";
        internal const string DefaultDecimalNumberFormat = "0.000000";

        /// <summary>
        /// Serializuje kolekci záznamů do souboru.
        /// </summary>
        /// <typeparam name="T">Typ záznamu.</typeparam>
        /// <param name="outputFileName">Soubor, kam mají být data uložena</param>
        /// <param name="collection">Záznamy k uložení</param>
        /// <param name="separator">Oddělovač záznamů na řádce</param>
        /// <param name="dateTimeFormat">Formát data</param>
        public static void SerializeFile<T>(string outputFileName, IEnumerable<T> collection, char separator = DefaultSeparator, string dateTimeFormat = DefaultDateTimeFormat, string decimalNumberFormat = DefaultDecimalNumberFormat)
        {
            var membersOrdered = GetFieldAttributes<T>().OrderBy(ma => ma.Attribute.Order).ToArray();
            var writer = new StreamWriter(outputFileName);

            var gtfsWriter = new CsvRecordSerializer<T>(writer, membersOrdered, separator, dateTimeFormat, decimalNumberFormat);
            gtfsWriter.WriteHeader();
            foreach (var record in collection)
            {
                gtfsWriter.WriteRecord(record);
            }

            writer.Close();
        }

        /// <summary>
        /// Načte kolekci záznamů ze souboru. Pokud soubor neexistuje, vrací prázdnou kolekci. Pokud je soubor chybný, nebo nejde přečíst, vyhazuje výjimku
        /// </summary>
        /// <typeparam name="T">Typ záznamu</typeparam>
        /// <param name="inputFileName">Soubor, který má být načten</param>
        /// <param name="separator">Oddělovač záznamů na řádce</param>
        /// <param name="cultureInfo">V jakém formátu jsou v souboru uložena čísla a datum (výchozí hodnota null použije <see cref="CultureInfo.InvariantCulture"/>).</param>
        /// <param name="dateTimeFormat">Formát data</param>
        /// <returns>Kolekce záznamů načtená ze souboru</returns>
        public static List<T> DeserializeFile<T>(string inputFileName, char separator = DefaultSeparator, CultureInfo cultureInfo = null, string dateTimeFormat = DefaultDateTimeFormat) where T : new()
        {
            var members = GetFieldAttributes<T>().ToArray();
            if (!File.Exists(inputFileName))
                return new List<T>();

            var reader = new StreamReader(inputFileName);

            var result = new List<T>();
            T current;
            var gtfsReader = new CsvRecordDeserializer<T>(reader, separator, cultureInfo != null ? cultureInfo : CultureInfo.InvariantCulture, dateTimeFormat);
            gtfsReader.ReadHeader(members);
            while ((current = gtfsReader.ReadRecord()) != null)
            {
                result.Add(current);
            }

            return result;
        }

        // projde membery třídy a u kterých nalezne GtfsField atribut, tak ty vezme a seřadí
        private static IEnumerable<MemberAndAttribute> GetFieldAttributes<T>()
        {
            var attributes = new List<MemberAndAttribute>();

            foreach (var member in typeof(T).GetMembers())
            {
                var memberAndAttribute = new MemberAndAttribute()
                {
                    MemberInfo = member,
                    Attribute = member.GetCustomAttribute<CsvFieldAttribute>()
                };

                if (memberAndAttribute.Attribute != null)
                {
                    attributes.Add(memberAndAttribute);
                }

            }

            // pokud se po zavolání distinct na ordery počet sníží, tak tam byly nějaké stejné
            // je to trochu komplikované, ale funkční
            if (attributes.Select(ma => ma.Attribute.Order).Distinct().Count() != attributes.Count)
            {
                throw new ArgumentException($"Invalid class {typeof(T)}. At least two members have identical order which may result in nondeterministic output.");
            }
            
            return attributes;
        }

    }
}
