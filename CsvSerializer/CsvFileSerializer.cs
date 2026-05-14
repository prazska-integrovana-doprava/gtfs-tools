using CsvSerializer.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
            var membersOrdered = GetFieldAttributes<T>().OrderBy(ma => ma.Attribute.Order).ToList();
            if (membersOrdered.Any(m => m.Attribute.ColumnPresence == CsvColumnPresence.OmitColumntIfEmpty))
            {
                // musí předem naenumerovat data
                var collectionAsArray = collection.ToArray();
                foreach (var member in membersOrdered.Where(m => m.Attribute.ColumnPresence == CsvColumnPresence.OmitColumntIfEmpty).ToArray()) // to array tam je, abychom si udělali kopii, protože budeme mazat z původní kolekce
                {
                    bool allEmpty = true;
                    foreach (var row in collectionAsArray)
                    {
                        object value = CsvRecordSerializer<T>.ReadFieldValue(member, row);
                        if (value != null && (member.Attribute.DefaultValue == null || !member.Attribute.DefaultValue.Equals(value)))
                        {
                            // neprázdná hodnota
                            allEmpty = false;
                            break;
                        }
                    }

                    if (allEmpty)
                    {
                        membersOrdered.Remove(member);
                    }
                }
            }

            var writer = new StreamWriter(outputFileName);

            var gtfsWriter = new CsvRecordSerializer<T>(writer, membersOrdered.ToArray(), separator, dateTimeFormat, decimalNumberFormat);
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
        /// <param name="encoding">Kódování souboru (výchozí hodnota null použije UTF8).</param>
        /// <param name="containsHeader">True, pokud na prvním řádku souboru je hlavička s názvy sloupců (pokusí se namapovat podle atributů třídy)</param>
        /// <returns>Kolekce záznamů načtená ze souboru</returns>
        public static List<T> DeserializeFile<T>(string inputFileName, char separator = DefaultSeparator, CultureInfo cultureInfo = null, string dateTimeFormat = DefaultDateTimeFormat, Encoding encoding = null, bool containsHeader = true, string lineSeparator = "") where T : new()
        {
            if (!File.Exists(inputFileName))
                return new List<T>();

            var reader = new StreamReader(inputFileName, encoding ?? Encoding.UTF8);
            return Deserialize<T>(reader, inputFileName, separator, cultureInfo, dateTimeFormat, containsHeader, lineSeparator);
        }

        /// <summary>
        /// Načte kolekci záznamů ze souboru. Pokud soubor neexistuje, vrací prázdnou kolekci. Pokud je soubor chybný, nebo nejde přečíst, vyhazuje výjimku
        /// </summary>
        /// <typeparam name="T">Typ záznamu</typeparam>
        /// <param name="reader">Soubor (stream), který má být načten</param>
        /// <param name="separator">Oddělovač záznamů na řádce</param>
        /// <param name="cultureInfo">V jakém formátu jsou v souboru uložena čísla a datum (výchozí hodnota null použije <see cref="CultureInfo.InvariantCulture"/>).</param>
        /// <param name="dateTimeFormat">Formát data</param>
        /// <param name="containsHeader">True, pokud na prvním řádku souboru je hlavička s názvy sloupců (pokusí se namapovat podle atributů třídy)</param>
        /// <returns>Kolekce záznamů načtená ze souboru</returns>
        public static List<T> Deserialize<T>(TextReader reader, string fileName, char separator = DefaultSeparator, CultureInfo cultureInfo = null, string dateTimeFormat = DefaultDateTimeFormat, bool containsHeader = true, string lineSeparator = "") where T : new()
        {
            var members = GetFieldAttributes<T>().ToArray();

            var result = new List<T>();
            T current;
            var csvReader = new CsvRecordDeserializer<T>(reader, separator, cultureInfo ?? CultureInfo.InvariantCulture, dateTimeFormat, lineSeparator, fileName);

            if (containsHeader)
            {
                csvReader.ReadHeader(members);
            }
            else
            {
                csvReader.InferHeader(members);
            }

            while ((current = csvReader.ReadRecord()) != null)
            {
                result.Add(current);
            }

            reader.Close();
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
