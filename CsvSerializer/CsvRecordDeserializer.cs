using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CsvSerializer
{
    /// <summary>
    /// Načítá záznamy z CSV souboru
    /// </summary>
    /// <typeparam name="T">Typ záznamu</typeparam>
    internal class CsvRecordDeserializer<T> where T : new()
    {
        private TextReader reader;

        // může obsahovat NULL položky, pokud jsou ve vstupním souboru neznámé sloupce
        private List<MemberAndAttribute> membersOrdered;

        private char separator;
        private CultureInfo cultureInfo;
        private string dateTimeFormat;

        public CsvRecordDeserializer(TextReader reader, char separator, CultureInfo cultureInfo, string dateTimeFormat)
        {
            this.reader = reader;
            this.separator = separator;
            this.cultureInfo = cultureInfo;
            this.dateTimeFormat = dateTimeFormat;
        }

        /// <summary>
        /// Přečte ze souboru hlavičku a připraví si metadata (který sloupeček co obsahuje)
        /// </summary>
        /// <param name="members">Seřazený seznam položek (odpovídá sloupcům v souboru)</param>
        public void ReadHeader(MemberAndAttribute[] members)
        {
            var membersDictionary = members.ToDictionary(m => m.Attribute.Name);
            membersOrdered = new List<MemberAndAttribute>();

            var headers = reader.ReadLine().Split(separator);
            
            foreach (var header in headers)
            {
                MemberAndAttribute member;
                if (membersDictionary.TryGetValue(header, out member))
                {
                    membersOrdered.Add(member);
                }
                else
                {
                    // sloupec, který neznáme; může jít o chybu, ale spíš jsou to jen nějaké extra informace, které nepotřebujeme
                    // musíme vložit null, aby při načítání řádku jsme věděli, že máme hodnotu přeskočit
                    membersOrdered.Add(null);
                }
            }
        }

        /// <summary>
        /// Přečte řádek a načte ho do instance za použití předem načtené identifikace sloupců.
        /// Je-li zavoláno před voláním <see cref="ReadHeader(MemberAndAttribute[])"/>, vyhazuje <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <returns>Načtený řádek</returns>
        public T ReadRecord()
        {
            if (membersOrdered == null)
            {
                throw new InvalidOperationException("File headers not initialized.");
            }

            var result = new T();
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                return default(T);
            }

            var row = line.SplitWithRespectToQuotes(separator).ToArray();
            if (row.Length < membersOrdered.Count)
            {
                throw new FormatException("Line is too short");
            }

            for (int i = 0; i < membersOrdered.Count; i++)
            {
                if (membersOrdered[i] == null)
                    continue;

                var value = DeserializeFieldValue(membersOrdered[i], row[i]);
                WriteFieldValue(membersOrdered[i], result, value);
            }

            return result;
        }

        // zapíše hodnotu do instance
        private void WriteFieldValue(MemberAndAttribute member, T result, object value)
        {
            if (member.MemberInfo.MemberType == MemberTypes.Field)
            {
                ((FieldInfo)member.MemberInfo).SetValue(result, value);
            }
            else if (member.MemberInfo.MemberType == MemberTypes.Property)
            {
                ((PropertyInfo)member.MemberInfo).SetValue(result, value);
            }
            else
            {
                throw new Exception("Unknown member");
            }
        }

        // načte a správně přetypuje string hodnotu načtenou ze souboru
        private object DeserializeFieldValue(MemberAndAttribute member, string str)
        {
            if (member.Attribute.DefaultValue != null && str == "")
            {
                return member.Attribute.DefaultValue;
            }

            return DeserializeValueOfType(member.FieldType, str);
        }

        // načte hodnotu daného typu, umí se rekurzit u známých generických typů
        private object DeserializeValueOfType(Type fieldType, string str)
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType(fieldType);
            if (nullableUnderlyingType != null)
            {
                // nullable typ - načteme hodnotu a zabalíme ji do nullable
                if (str != "")
                {
                    var underlyingValue = DeserializeValueOfType(nullableUnderlyingType, str);
                    return Activator.CreateInstance(fieldType, underlyingValue);
                }
                else
                {
                    return Activator.CreateInstance(fieldType);
                }
            }

            if (fieldType.IsEnum)
            {
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return Enum.Parse(fieldType, str);
                }
                else
                {
                    return Enum.Parse(fieldType, "0");
                }
            }
            else if (fieldType.Equals(typeof(string)))
            {
                return str;
            }
            else if (fieldType.Equals(typeof(bool)))
            {
                return str.Trim() != "0";
            }
            else if (fieldType.Equals(typeof(int)))
            {
                return int.Parse(str);
            }
            else if (fieldType.Equals(typeof(long)))
            {
                return long.Parse(str);
            }
            else if (fieldType.Equals(typeof(float)))
            {
                return float.Parse(str, cultureInfo);
            }
            else if (fieldType.Equals(typeof(double)))
            {
                return double.Parse(str, cultureInfo);
            }
            else if (fieldType.Equals(typeof(DateTime)))
            {
                return DateTime.ParseExact(str, dateTimeFormat, cultureInfo);
            }
            else if (typeof(ICsvSerializable).IsAssignableFrom(fieldType))
            {
                var instance = (ICsvSerializable)Activator.CreateInstance(fieldType);
                instance.LoadFromString(str);
                return instance;
            }
            else
            {
                throw new FormatException($"Unsupported record type ${fieldType.Name} for value '${str}'");
            }
        }
    }
}
