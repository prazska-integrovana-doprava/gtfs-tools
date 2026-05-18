using CsvSerializer.Attributes;

namespace JdfModel
{
    /// <summary>
    /// LinExt.txt
    /// </summary>
    public class RouteExt
    {
        /// <summary>
        /// Číslo linky - povinné šestimístné číslo
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int RouteId { get; set; }

        /// <summary>
        /// Pořadí - povinné číslo, pořadí v rámci linky
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public int Unused2 { get; set; }

        /// <summary>
        /// Kód dopravy - povinný text z číselníku MHD CIS JŘ
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public int Unused3 { get; set; }
        
        /// <summary>
        /// Označení linky - povinný text, místní označení linky
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public string RouteName { get; set; }

        /// <summary>
        /// Preference označení - povinný znak 0/1
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public bool RouteNameIsPrefered { get; set; }

        /// <summary>
        /// Rezerva - nepovinný text
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public string Unused6 { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public int RouteVersion { get; set; }

        public override string ToString()
        {
            return $"{RouteName} ({RouteId})";
        }
    }
}
