using System.Collections.Generic;
using System.Text;

namespace CsvSerializer
{
    public static class StringExtensions
    {
        /// <summary>
        /// Rozdělí řetězec podle delimiteru. Pokud obsahuje text v uvozovkách, uvozovky odstraní a jejich obsah neinterpretuje.
        /// </summary>
        /// <param name="str">Řetězec</param>
        /// <param name="delimiter">Oddělovač</param>
        public static IEnumerable<string> SplitWithRespectToQuotes(this string str, char delimiter)
        {
            bool inQuote = false;
            var buffer = new StringBuilder();

            foreach (var ch in str)
            {
                if (ch == '"')
                {
                    inQuote = !inQuote;
                }
                else if (ch == delimiter && !inQuote)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }
                else
                {
                    buffer.Append(ch);
                }
            }

            yield return buffer.ToString();
        }
    }
}
