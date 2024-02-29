using CsvSerializer.Attributes;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CsvSerializer
{
    /// <summary>
    /// Ukládá záznamy do CSV souboru
    /// </summary>
    /// <typeparam name="T">Typ záznamu.</typeparam>
    internal class CsvRecordSerializer<T>
    {
        // všichni oatributovaní membeři seřazení podle GtfsFieldAttribute.Order
        private MemberAndAttribute[] MembersOrdered;
        
        private TextWriter Writer;

        private char separator;
        private string dateTimeFormat;
        private string decimalNumberFormat;

        public CsvRecordSerializer(TextWriter writer, MemberAndAttribute[] membersOrdered, char separator, string dateTimeFormat, string decimalNumberFormat)
        {
            MembersOrdered = membersOrdered;
            Writer = writer;
            this.separator = separator;
            this.dateTimeFormat = dateTimeFormat;
            this.decimalNumberFormat = decimalNumberFormat;
        }

        /// <summary>
        /// Zapíše hlavičku a všechny zadané záznamy do souboru
        /// </summary>
        public void WriteHeader()
        {
            Writer.WriteLine(string.Join(separator.ToString(), FirstRow));
        }

        /// <summary>
        /// Zapíše záznam do streamu
        /// </summary>
        public void WriteRecord(T record)
        {
            var row = SerializeRecord(record);

            for (int i = 0; i < row.Length; i++)
            {
                // pokud je tam čárka a není to v uvozovkách, tak to dát do uvozovek
                if (row[i].Contains(separator))
                {
                    if (!row[i].StartsWith("\"") && !row[i].EndsWith("\""))
                    {
                        row[i] = Quote(row[i]);
                    }
                }
            }

            Writer.WriteLine(string.Join(separator.ToString(), row));
        }

        // názvy sloupečků (první řádek souboru)
        private string[] FirstRow
        {
            get
            {
                return MembersOrdered.Select(fa => fa.Attribute.Name).ToArray();
            }
        }

        // ouvozovkuje řetězec, myslí i na uvozovky uprostřed, ze kterých se stanou dvouuvozovky ""
        protected static string Quote(string toBeQuoted)
        {
            // uvozovky -> ""
            if (toBeQuoted.Contains("\""))
            {
                toBeQuoted.Replace("\"", "\"\"");
            }

            return $"\"{toBeQuoted}\"";
        }

        // zestringuje jeden záznam do řádku v souboru
        private string[] SerializeRecord(T record)
        {
            var result = new string[MembersOrdered.Length];

            int i = 0;
            foreach(var member in MembersOrdered)
            {
                object value = ReadFieldValue(member, record);
                result[i] = SerializeFieldValue(member, value);
                
                if ((member.Attribute.PostProcess & CsvFieldPostProcess.Quote) == CsvFieldPostProcess.Quote && !string.IsNullOrEmpty(result[i]))
                {
                    result[i] = Quote(result[i]);
                }

                i++;
            }

            return result;
        }

        // přečte pomocí reflection daný field/property z instance
        private object ReadFieldValue(MemberAndAttribute member, T record)
        {
            object value;
            if (member.MemberInfo.MemberType == MemberTypes.Field)
            {
                value = ((FieldInfo)member.MemberInfo).GetValue(record);
            }
            else if (member.MemberInfo.MemberType == MemberTypes.Property)
            {
                value = ((PropertyInfo)member.MemberInfo).GetValue(record);
            }
            else
            {
                throw new Exception("Unknown member");
            }

            return value;
        }

        // převede hodnotu do textové podoby
        private string SerializeFieldValue(MemberAndAttribute member, object value)
        {
            if (member.Attribute.DefaultValue != null)
            {
                if (member.Attribute.DefaultValue.Equals(value))
                {
                    return "";
                }
            }

            var underlyingTypeIfNullable = Nullable.GetUnderlyingType(member.FieldType);

            if (member.FieldType.IsEnum)
            {
                return ((int)value).ToString();
            }
            else if (value != null && (underlyingTypeIfNullable?.IsEnum).GetValueOrDefault())
            {
                return ((int)value).ToString();
            }
            else if (member.FieldType.Equals(typeof(bool)))
            {
                return ((bool)value) ? "1" : "0";
            }
            else if (member.FieldType.Equals(typeof(float)))
            {
                return ((float)value).ToString(decimalNumberFormat, CultureInfo.InvariantCulture);
            }
            else if (member.FieldType.Equals(typeof(double)))
            {
                return ((double)value).ToString(decimalNumberFormat, CultureInfo.InvariantCulture);
            }
            else if (member.FieldType.Equals(typeof(DateTime)))
            {
                return ((DateTime)value).ToString(dateTimeFormat);
            }
            else
            {
                return value != null ? value.ToString() : "";
            }
        }
    }
}
