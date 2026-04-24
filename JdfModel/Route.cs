using CsvSerializer.Attributes;
using System;

namespace JdfModel
{
    /// <summary>
    /// Linky.txt
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Číslo linky - povinné šestimístné číslo
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public int RouteId { get; set; }

        /// <summary>
        /// Název linky - povinný text
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public string RouteDescription { get; set; }

        /// <summary>
        /// IČ dopravce - povinné osmimístné číslo, vazba na ID dopravce
        /// </summary>
        [CsvField("", 3, CsvFieldPostProcess.Quote)]
        public string AgencyId { get; set; }

        /// <summary>
        /// Typ linky - povinný znak, některá z konstant z <see cref="RouteTypes"/>
        /// </summary>
        [CsvField("", 4, CsvFieldPostProcess.Quote)]
        public char RouteType { get; set; }

        /// <summary>
        /// Dopravní prostředek - povinný znak, některá z konstant z <see cref="RouteTrafficTypes"/>
        /// </summary>
        [CsvField("", 5, CsvFieldPostProcess.Quote)]
        public char TrafficType { get; set; }

        /// <summary>
        /// Výlukový JŘ - povinný znak 0/1
        /// </summary>
        [CsvField("", 6, CsvFieldPostProcess.Quote)]
        public bool IsExceptional { get; set; }

        /// <summary>
        /// Seskupení spojů - povinný znak 0/1
        /// </summary>
        [CsvField("", 7, CsvFieldPostProcess.Quote)]
        public bool Unused7 { get; set; }

        /// <summary>
        /// Použití označníků - povinný znak 0/1
        /// </summary>
        [CsvField("", 8, CsvFieldPostProcess.Quote)]
        public bool Unused8 { get; set; }

        /// <summary>
        /// Jednosměrný JŘ - povinný znak 0/1
        /// </summary>
        [CsvField("", 9, CsvFieldPostProcess.Quote)]
        public bool IsOneDirectional { get; set; }

        /// <summary>
        /// Rezerva - nepovinný text
        /// </summary>
        [CsvField("", 10, CsvFieldPostProcess.Quote)]
        public string Unused10 { get; set; }

        /// <summary>
        /// Číslo licence - nepovinný text
        /// </summary>
        [CsvField("", 11, CsvFieldPostProcess.Quote)]
        public string Unused11 { get; set; }

        /// <summary>
        /// Platnost lic. od - nepovinné datum
        /// </summary>
        [CsvField("", 12, CsvFieldPostProcess.Quote)]
        public DateTime? Unused12 { get; set; }

        /// <summary>
        /// Platnost lic. do - nepovinné datum
        /// </summary>
        [CsvField("", 13, CsvFieldPostProcess.Quote)]
        public DateTime? Unused13 { get; set; }

        /// <summary>
        /// Platnost JŘ od - povinné datum
        /// </summary>
        [CsvField("", 14, CsvFieldPostProcess.Quote)]
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// Platnost JŘ do - povinné datum
        /// </summary>
        [CsvField("", 15, CsvFieldPostProcess.Quote)]
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Rozlišení dopravce - povinné číslo, vazba na rozlišení dopravce
        /// </summary>
        [CsvField("", 16, CsvFieldPostProcess.Quote)]
        public int AgencyVersion { get; set; }

        /// <summary>
        /// Rozlišení linky - povinné číslo
        /// </summary>
        [CsvField("", 17, CsvFieldPostProcess.Quote)]
        public int RouteVersion { get; set; }

        public override string ToString()
        {
            return $"{RouteId}";
        }
    }

    /// <summary>
    /// Typy pro <see cref="Route.RouteType"/>
    /// </summary>
    public static class RouteTypes
    {
        /// <summary>
        /// Městská
        /// </summary>
        public const char Urban = 'A';

        /// <summary>
        /// Městská s obsluhou příměstských oblastí
        /// </summary>
        public const char Suburban = 'B';

        /// <summary>
        /// Mezinárodní - s vyloučenou vnitrostátní přepravou
        /// </summary>
        public const char InternationalOnly = 'N';

        /// <summary>
        /// Mezinárodní - s povolenou vnitrostátní přepravou
        /// </summary>
        public const char InternationalAndNational = 'P';

        /// <summary>
        /// Vnitrostátní - vnitrokrajská
        /// </summary>
        public const char Regional = 'V';

        /// <summary>
        /// Vnitrostátní - mezikrajská
        /// </summary>
        public const char Interregional = 'Z';

        /// <summary>
        /// Vnitrostátní - dálková
        /// </summary>
        public const char NationalDistant = 'D';
    }

    /// <summary>
    /// Druh dopravy pro <see cref="Route.TrafficType"/>
    /// </summary>
    public static class RouteTrafficTypes
    {
        /// <summary>
        /// Autobus
        /// </summary>
        public const char Bus = 'A';

        /// <summary>
        /// Tramvaj
        /// </summary>
        public const char Tram = 'E';

        /// <summary>
        /// Lanová dráha
        /// </summary>
        public const char FunicularOrCable = 'L';

        /// <summary>
        /// Metro
        /// </summary>
        public const char Metro = 'M';

        /// <summary>
        /// Přívoz
        /// </summary>
        public const char Ferry = 'P';

        /// <summary>
        /// Trolejbus
        /// </summary>
        public const char Trolleybus = 'T';
    }
}
