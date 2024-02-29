using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Popis aktivity vlaku ve stanici + známé konstanty
    /// </summary>
    [XmlRoot]
    public class TrainActivity
    {
        /// <summary>
        /// Normální veřejné zastavení
        /// </summary>
        public const string StopsForBoardingAndUnboarding = "0001";

        /// <summary>
        /// Zastavení jen pro nástup
        /// </summary>
        public const string StopsForBoardingOnlyActivityCode = "0028";

        /// <summary>
        /// Zastavení jen pro výstup
        /// </summary>
        public const string StopsForUnboardingOnlyActivityCode = "0029";

        /// <summary>
        /// Zastavení jen na znamení
        /// </summary>
        public const string RequestStopActivityCode = "0030";

        /// <summary>
        /// Vlak může odjet již v čase příjezdu, ale ne před ním
        /// </summary>
        public const string DepartureEqualsToArrivalTimeCode = "0031";

        /// <summary>
        /// Nečeká na žádné přípoje
        /// </summary>
        public const string DoesNotWaitForConnections = "0033";

        /// <summary>
        /// Odjezd ihned po výstupu.
        /// Většinou se užívá ke konci jízdy vlaku, vlak může pokračovat okamžitě poté, co všichni cestující vystoupili.
        /// </summary>
        public const string DepartsASAPActivityCode = "0032";

        /// <summary>
        /// Zastavuje až ode dne vyhlášení
        /// </summary>
        public const string StopsAfterDeclaration = "CZ01";

        /// <summary>
        /// Pobyt kratší než 1/2 min.
        /// </summary>
        public const string StopsForLessThanHalfMinuteActivityCode = "CZ02";

        /// <summary>
        /// Zastavení jen z dopravních důvodů
        /// </summary>
        public const string StopsOnlyForTrafficReasonsActivityCode = "0002";

        /// <summary>
        /// Nezveřejněné zastavení
        /// </summary>
        public const string PrivateStopActivityCode = "CZ13";


        /// <summary>
        /// Kód aktivity (úkonu) ve stanici.
        /// </summary>
        [XmlElement]
        public string TrainActivityType { get; set; }
    }
}
