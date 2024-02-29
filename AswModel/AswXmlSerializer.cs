using System.Xml;
using System.Xml.Serialization;
using JR_XML_EXP;

namespace AswModel
{
    /// <summary>
    /// Zajišťuje načítání z XML exportu z ASW JŘ
    /// </summary>
    public static class AswXmlSerializer
    {
        /// <summary>
        /// Načte XML soubor s exportem z ASW JŘ. Může jít o soubor s oběhy nebo jen zastávkami.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static DavkaJR Deserialize(string fileName)
        {
            var xmlSerializer = new XmlSerializer(typeof(DavkaJR));
            var xmlReader = XmlReader.Create(fileName);
            return (DavkaJR)xmlSerializer.Deserialize(xmlReader);
        }
    }
}
