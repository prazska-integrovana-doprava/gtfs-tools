using System.IO;
using System.Xml.Serialization;

namespace TrainsEditor.EditorLogic
{
    /// <summary>
    /// Pomocná třída, která umí serializovat XML do stringu a deserializovat XML ze stringu
    /// </summary>
    static class XmlSerializeToObjectHelper
    {
        /// <summary>
        /// Serializuje objekt do jeho XML reprezentace. Objekt musí být opatřen potřebnými atributy.
        /// </summary>
        public static string SerializeObject<T>(this T toSerialize)
        {
            var xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        /// <summary>
        /// Deserializuje zadaný řetězec do instance objektu typu <typeparamref name="T"/>. Typ musí být opatřen potřebnými atributy.
        /// </summary>
        public static T DeserializeObject<T>(string xmlData)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));

            using (var textReader = new StringReader(xmlData))
            {
                var result = xmlSerializer.Deserialize(textReader);
                return (T)result;
            }
        }
    }
}
