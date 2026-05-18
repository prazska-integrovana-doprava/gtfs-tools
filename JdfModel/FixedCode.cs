using CsvSerializer.Attributes;

namespace JdfModel
{
    /// <summary>
    /// Pevnykod.txt
    /// </summary>
    public class FixedCode
    {
        /// <summary>
        /// Číslo pevného kódu - povinné (max. pětimístné) číslo
        /// </summary>
        [CsvField("", 1, CsvFieldPostProcess.Quote)]
        public string CodeId { get; set; }

        /// <summary>
        /// Označení pevného kódu - povinný text, max 1 znak z tabulky pevných kódů <see cref="FixedCodes"/>
        /// </summary>
        [CsvField("", 2, CsvFieldPostProcess.Quote)]
        public char CodeChar { get; set; }

        public override string ToString()
        {
            return CodeChar.ToString();
        }
    }

    /// <summary>
    /// Možné hodnoty pro <see cref="FixedCode.CodeChar"/>
    /// </summary>
    public static class FixedCodes
    {
        /// <summary>
        /// Jede v pracovních dnech
        /// </summary>
        public const char OperatesOnWorkdays = 'X';

        /// <summary>
        /// Jede v neděli a státem uznané svátky
        /// </summary>
        public const char OperatesOnSundaysAndHolidays = '+';

        /// <summary>
        /// Jede v pondělí
        /// </summary>
        public const char OperatesOnMonday = '1';

        /// <summary>
        /// Jede v úterý
        /// </summary>
        public const char OperatesOnTuesday = '2';

        /// <summary>
        /// Jede ve středu
        /// </summary>
        public const char OperatesOnWednesday = '3';

        /// <summary>
        /// Jede ve čtvrtek
        /// </summary>
        public const char OperatesOnThursday = '4';

        /// <summary>
        /// Jede v pátek
        /// </summary>
        public const char OperatesOnFriday = '5';

        /// <summary>
        /// Jede v sobotu
        /// </summary>
        public const char OperatesOnSaturday = '6';

        /// <summary>
        /// Jede v neděli
        /// </summary>
        public const char OperatesOnSunday = '7';

        /// <summary>
        /// K jízdence je možné zakoupit místenku
        /// </summary>
        public const char SeatReservationAvailable = 'R';

        /// <summary>
        /// Spoj je možné použít jen s místenkou
        /// </summary>
        public const char SeatReservationRequired = '#';

        /// <summary>
        /// Spoj zastávkou projíždí
        /// </summary>
        public const char PassesThroughStop = '|';

        /// <summary>
        /// Spoj jede po jiné trase
        /// </summary>
        public const char AlternativeRoute = '<';

        /// <summary>
        /// Spoj s bezbariérově přístupným vozidlem
        /// </summary>
        public const char AccessibleVehicle = '@';

        /// <summary>
        /// Bezbariérově přístupná zastávka
        /// </summary>
        public const char AccessibleStop = '@';

        /// <summary>
        /// Spoj s možností občerstvení
        /// </summary>
        public const char RefreshmentsOnBoard = '%';

        /// <summary>
        /// Občerstvení nebo restaurace v objektu zastávky
        /// </summary>
        public const char RefreshmentsAtStop = '%';

        /// <summary>
        /// Veřejné WC v objektu zastávky
        /// </summary>
        public const char PublicToiletAtStop = 'W';

        /// <summary>
        /// Veřejné WC s bezbariérovým přístupem v objektu zastávky
        /// </summary>
        public const char AccessiblePublicToiletAtStop = 'w';

        /// <summary>
        /// Spoj zastavuje jen na znamení nebo požádání
        /// </summary>
        public const char StopOnRequest = 'x';

        /// <summary>
        /// Spoj zastavuje jen pro vystupování
        /// </summary>
        public const char AlightingOnly = '(';

        /// <summary>
        /// Spoj zastavuje jen pro nastupování
        /// </summary>
        public const char BoardingOnly = ')';

        /// <summary>
        /// Spoj jede jen na objednání
        /// Spoj zastávku obsluhuje jen na objednání
        /// </summary>
        public const char PhoneRequestStopOnly = 'T';

        /// <summary>
        /// Zastávka s možností přestupu na linkovou dopravu
        /// </summary>
        public const char TransferToBus = 'b';

        /// <summary>
        /// Zastávka s možností přestupu na metro
        /// </summary>
        public const char TransferToMetro = 'U';

        /// <summary>
        /// Zastávka u přístaviště osobní lodní dopravy
        /// </summary>
        public const char TransferToFerry = 'S';

        /// <summary>
        /// Zastávka u veřejného letiště
        /// </summary>
        public const char TransferToAirport = 'J';

        /// <summary>
        /// Zastávka u parkoviště systému „Park and Ride“
        /// </summary>
        public const char ParkAndRide = 'P';
    }
}
