using CommonLibrary;
using CzpttModel;
using GtfsLogging;
using GtfsModel.Enumerations;
using System.Linq;
using TrainsEditor.CommonLogic;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Zastavení vlaku ve stanici
    /// </summary>
    class StationTime : GtfsModel.Extended.StopTime
    {
        /// <summary>
        /// Kód zastávky, Pro SŽDC jde o číselník SR 70 bez kontrolky.
        /// </summary>
        public int StationCode { get; set; }
        
        /// <summary>
        /// Název stanice (pro debugovací účely)
        /// </summary>
        public string StationName { get; set; }
        
        /// <summary>
        /// True, pokud vlak může odjet v čase příjezdu, nebo kdykoliv po výstupu a nástupu cestujících
        /// (jinými slovy znamená, že departure time je spíše orientační) 
        /// </summary>
        public bool CanDepartSooner { get; set; }

        /// <summary>
        /// Číslo, kterým je vlak označený (dvou až pětimístné, může se v průběhu trasy měnit)
        /// </summary>
        public int TrainNumberOnDeparture { get; set; }

        /// <summary>
        /// Typ vlaku na odjezdu ze stanice (rychlík / osobák / ...)
        /// </summary>
        public CommercialTrafficType TrainTypeOnDeparture { get; set; }

        /// <summary>
        /// Textové označení linky (např. S1), které vlak náleží na odjezdu z této stanice (může se v průběhu trasy měnit)
        /// </summary>
        public TrainLineInfo TrainLineOnDeparture { get; set; }

        /// <summary>
        /// True, pokud je vlak v dané stanici bezbariérově přístupný (neřeší stav nástupišť, pouze jestli je ve vlaku řazen vůz schopný přepravy vozíčkáře)
        /// </summary>
        public bool IsWheelchairAccessible { get; set; }

        /// <summary>
        /// False, pokud odjíždí ze stanice jako neveřejný vlak (ale může jako veřejný přijíždět)
        /// </summary>
        public bool IsPublicOnDeparture { get; set; }

        /// <summary>
        /// True, pokud je vlak z této stanice veden náhradní dopravou
        /// </summary>
        public bool IsSubstituteTransportOnDeparture { get; set; }

        /// <summary>
        /// Jde o veřejné zastavení (spoj zde obsluhuje cestující) a zároveň jde o zastávku známou v ASW JŘ
        /// </summary>
        public bool IsInIntegratedArea => ((TrainStop)Stop).IsFromAsw; // && ((TrainStop)Stop).ZoneId != "-"; 

        /// <summary>
        /// Vytvoří záznam o průjezdu stanicí. Pokud je neveřejný, nebo jinak divný, vrací null
        /// </summary>
        /// <param name="location">Záznam o zastavení z XML</param>
        /// <param name="czpttMessage">Záznam o celém vlaku</param>
        /// <param name="isWheelchairAccessible">True, pokud je ve vlaku řazen vůz schopný přepravovat vozíčkáře</param>
        /// <param name="prevLocation">Předchozí záznam o poloze nebo null, pokud jde o první stanici</param>
        /// <param name="stopDb">Databáze stanic zastávek a dalších bodů</param>
        /// <param name="isFirstOrLastStation">True, pokud jde o první nebo poslední stanici, jinak false</param>
        /// <param name="loaderLog">Logování</param>
        public static StationTime Create(CZPTTLocation location, CZPTTCISMessage czpttMessage, bool isWheelchairAccessible,
            CZPTTLocation prevLocation, StationDatabase stopDb, bool isFirstOrLastStation, ICommonLogger loaderLog)
        {
            if (location.TimingAtLocation == null || !location.IsInPublicPart(prevLocation))
            {
                return null; // nejsou žádné informace o čase průjezdu nebo jsme v manipulačním nájezdu/zátahu
            }

            // načtení průjezdních časů
            var arrivalTime = location.GetLocationArrivalTime();
            var departureTime = location.GetLocationDepartureTime();
            if (arrivalTime == null && departureTime == null)
            {
                loaderLog.Log(LogMessageType.WARNING_TRAIN_MSG_UNDEF_TIMING, $"Není definován čas odjezdu ani příjezdu v bodě {location}", czpttMessage);
                return null;
            }
            else if (arrivalTime == null)
            {
                arrivalTime = departureTime;
            }
            else if (departureTime == null)
            {
                departureTime = arrivalTime;
            }

            bool stopsHere = location.TrainStopsHere(prevLocation, isFirstOrLastStation);
            bool requestStop = location.ContainsAcitivity(TrainActivity.RequestStopActivityCode);

            var result = new StationTime()
            {
                ArrivalTime = arrivalTime.Value,
                DepartureTime = departureTime.Value,
                TrainLineOnDeparture = location.GetLineInfo(),
                DropOffType = !stopsHere ? DropOffType.None : requestStop ? DropOffType.DriverRequest : DropOffType.Regular,
                PickupType = !stopsHere ? PickupType.None : requestStop ? PickupType.DriverRequest : PickupType.Regular,
                StationCode = location.Location.LocationPrimaryCode,
                StationName = location.Location.PrimaryLocationName,
                CanDepartSooner = location.TrainActivity.Any(act => act.TrainActivityType == TrainActivity.DepartureEqualsToArrivalTimeCode
                                    || act.TrainActivityType == TrainActivity.DepartsASAPActivityCode),
                TrainNumberOnDeparture = location.OperationalTrainNumber % 100000,
                TrainTypeOnDeparture = CommercialTrafficType.CommercialTrafficTypes.GetValueOrDefault(location.CommercialTrafficType),
                TripOperationType = TripOperationType.Regular,
                BikesAllowed = BikeAccessibility.Possible,
                IsPublicOnDeparture = location.IsPublicOnDeparture(),
                IsSubstituteTransportOnDeparture = location.IsAlternativeTransportOnDeparture(),
                IsWheelchairAccessible = isWheelchairAccessible,
            };

            // zastavení jen pro výstup/nástup?
            if (location.ContainsAcitivity(TrainActivity.StopsForUnboardingOnlyActivityCode))
            {
                result.PickupType = PickupType.None;
            }
            else if (location.ContainsAcitivity(TrainActivity.StopsForBoardingOnlyActivityCode))
            {
                result.DropOffType = DropOffType.None;
            }

            // vlak může odjet dřív
            if (location.ContainsAcitivity(TrainActivity.DepartureEqualsToArrivalTimeCode) || location.ContainsAcitivity(TrainActivity.DepartsASAPActivityCode))
            {
                result.DepartureTime = result.ArrivalTime;
            }

            // přepis kódu zastávky
            // TODO nemělo by být potřeba, už je přepis ve StationDatabase
            //int rewrStationCode;
            //if (Program.RewriteStations.TryGetValue(result.StationCode, out rewrStationCode))
            //{
            //    result.StationCode = rewrStationCode;
            //}

            result.Stop = stopDb.FindInAswOrCis(location.Location.CountryCodeISO, result.StationCode, location.Location.PrimaryLocationName);
            if (result.Stop.GtfsId == null && location.Location.CountryCodeISO == LocationIdent.CountryCodeCZ)
            {
                loaderLog.Log(LogMessageType.WARNING_TRAIN_MISSING_STOP, $"Dopravní bod {result.StationCode} ({result.StationName}) nebyl nalezen v žádném číselníku.");
            }

            return result;
        }

        public override string ToString()
        {
            return $"{TrainNumberOnDeparture} at {Stop.Name} at {DepartureTime}";
        }
    }

}
