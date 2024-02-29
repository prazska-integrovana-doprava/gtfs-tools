using CommonLibrary;
using CzpttModel;
using System.Collections.Generic;
using System.Linq;
using TrainsEditor.ExportModel;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Metody pro práci s <see cref="CZPTTLocation"/>.
    /// </summary>
    static class LocationExtensions
    {
        /// <summary>
        /// Vrátí <see cref="CZPTTLocation.CommercialTrafficType"/> již převedené z čísla do známé pojmenované hodnoty (nebo null, kdyby hodnota nebyla známá)
        /// </summary>
        public static CommercialTrafficType GetCommercialTrafficTypeInfo(this CZPTTLocation location)
        {
            return CommercialTrafficType.CommercialTrafficTypes.GetValueOrDefault(location.CommercialTrafficType);
        }

        /// <summary>
        /// Vrátí druh a číslo vlaku v dané lokaci (např. Os 8321)
        /// </summary>
        public static string GetTrainTypeAndNumber(this CZPTTLocation location)
        {
            var trafficType = GetCommercialTrafficTypeInfo(location)?.Abbr;
            if (location.OperationalTrainNumber > 0)
            {
                if (trafficType != null)
                {
                    return $"{trafficType} {location.OperationalTrainNumber}";
                }
                else
                {
                    return location.OperationalTrainNumber.ToString();
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Vrátí info o lince platné v dané lokaci
        /// </summary>
        public static TrainLineInfo GetLineInfo(this CZPTTLocation location)
        {
            var nsparam = location.NetworkSpecificParameter.FirstOrDefault(nsp => nsp.Name == "CZPassengerServiceNumber");
            if (!string.IsNullOrEmpty(nsparam?.Value))
            {
                if (int.TryParse(nsparam.Value, out int trnum))
                {
                    return TrainLineInfo.TrainLineNumberToName(trnum);
                }
                else
                {
                    return new TrainLineInfo(TrainLineType.Pid, nsparam.Value, 0);
                }
            }

            return TrainLineInfo.UndefinedLineInfoInstance;
        }

        /// <summary>
        /// Změní linku v dané lokaci
        /// </summary>
        /// <param name="lineName">Název linky. Může být buď kód linky podle číselníku SŽ, toleruje se ale i libovolný string</param>
        public static void SetLineName(this CZPTTLocation location, string lineName)
        {
            var nsparam = location.NetworkSpecificParameter.FirstOrDefault(nsp => nsp.Name == "CZPassengerServiceNumber");
            if (nsparam == null)
            {
                nsparam = new NameAndValuePair() { Name = "CZPassengerServiceNumber" };
                location.NetworkSpecificParameter.Add(nsparam);
            }

            nsparam.Value = lineName;
        }

        /// <summary>
        /// Vrátí čas příjezdu do dané lokace nebo null, pokud lokace nemá čas příjezdu
        /// </summary>
        public static Time? GetLocationArrivalTime(this CZPTTLocation location)
        {
            var arrivalTiming = location.TimingAtLocation?.Timing?.FirstOrDefault(t => t.TimingQualifierCode == TimingAtLocation.TimingQualifierCodeEnum.Arrival);
            if (arrivalTiming != null)
            {
                return Time.FromDateTime(arrivalTiming.Time, arrivalTiming.Offset);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Vrátí čas odjezdu z dané lokace nebo null, pokud lokace nemá čas odjezdu
        /// </summary>
        public static Time? GetLocationDepartureTime(this CZPTTLocation location)
        {
            var departureTiming = location.TimingAtLocation?.Timing?.FirstOrDefault(t => t.TimingQualifierCode == TimingAtLocation.TimingQualifierCodeEnum.Departure);
            if (departureTiming != null)
            {
                return Time.FromDateTime(departureTiming.Time, departureTiming.Offset);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Vrátí čas odjezdu v dané lokaci, je-li definován. Pokud ne, vrátí čas příjezdu. Pokud ani ten není definován, vrátí null.
        /// </summary>
        public static Time? GetLocationDefaultTime(this CZPTTLocation location)
        {
            return GetLocationDepartureTime(location) ?? GetLocationArrivalTime(location);
        }

        /// <summary>
        /// Nastaví čas ve stanici
        /// </summary>
        /// <param name="timingQualifier">Který čas nastavujeme</param>
        /// <param name="value">Hodnota času, může být i null, pak to znamená, že chceme čas smazat</param>
        public static void SetLocationTime(this CZPTTLocation location, TimingAtLocation.TimingQualifierCodeEnum timingQualifier, Time? value)
        {
            if (value.HasValue)
            {
                if (location.TimingAtLocation == null)
                {
                    location.TimingAtLocation = new TimingAtLocation();
                }

                if (location.TimingAtLocation.Timing == null)
                {
                    location.TimingAtLocation.Timing = new List<TimingAtLocation.TimingType>();
                }

                var timeRecord = location.TimingAtLocation.Timing.FirstOrDefault(t => t.TimingQualifierCode == timingQualifier);
                if (timeRecord == null)
                {
                    timeRecord = new TimingAtLocation.TimingType() { TimingQualifierCode = timingQualifier };
                    location.TimingAtLocation.Timing.Add(timeRecord);
                }

                timeRecord.Offset = value.Value.DaysOffset;
                timeRecord.Time = value.Value.ToDateTime();
            }
            else
            {
                // chceme záznam smazat
                if (location.TimingAtLocation == null)
                {
                    return;
                }

                if (location.TimingAtLocation.Timing == null)
                {
                    return;
                }

                var timeRecord = location.TimingAtLocation.Timing.FirstOrDefault(t => t.TimingQualifierCode == timingQualifier);
                if (timeRecord != null)
                {
                    location.TimingAtLocation.Timing.Remove(timeRecord);
                    if (!location.TimingAtLocation.Timing.Any())
                    {
                        location.TimingAtLocation = null;
                    }
                }
            }
        }

        /// <summary>
        /// Vrátí název lokace jak je uveden v souboru a k němu čas (preferovaně odjezdu, případně příjezdu)
        /// </summary>
        public static string GetLocationNameAndTime(this CZPTTLocation location)
        {
            return $"{location.Location.PrimaryLocationName} {GetLocationDefaultTime(location)?.ModuloDay().ToStringWithoutSeconds()}";
        }

        /// <summary>
        /// Vrací true, pokud mezi aktivitami ve stanici je některá ze zadaných <paramref name="activities"/>
        /// </summary>
        public static bool ContainsAcitivity(this CZPTTLocation location, params string[] activities)
        {
            return location.TrainActivity.Any(activity => activities.Any(activity2 => activity.TrainActivityType == activity2));
        }

        /// <summary>
        /// Vrátí kód stanice a před ním dva znaky kódující stát (např. CZ5454127)
        /// </summary>
        public static string GetLocationWithCountryCode(this CZPTTLocation location)
        {
            return location.Location.CountryCodeISO + location.Location.LocationPrimaryCode;
        }

        /// <summary>
        /// Vrací true, pokud zde vlak staví pro veřejnost, jinak false.
        /// </summary>
        /// <param name="prevLocation">Předchozí lokace</param>
        /// <param name="isFirstOrLastStation">Je první nebo poslední zastavení v souboru</param>
        public static bool TrainStopsHere(this CZPTTLocation location, CZPTTLocation prevLocation, bool isFirstOrLastStation)
        {
            var arrivalTime = GetLocationArrivalTime(location);
            var departureTime = GetLocationDepartureTime(location);
            return
                location.IsInPublicPart(prevLocation) &&
                (
                    ContainsAcitivity(location, TrainActivity.StopsForBoardingAndUnboarding) 
                    || ContainsAcitivity(location, TrainActivity.StopsForLessThanHalfMinuteActivityCode)
                    || (arrivalTime != null && departureTime != null && departureTime > arrivalTime)
                    || isFirstOrLastStation
                )
                && !ContainsAcitivity(location, TrainActivity.StopsOnlyForTrafficReasonsActivityCode)
                && !ContainsAcitivity(location, TrainActivity.StopsAfterDeclaration)
                && !CorrectionConfig.IgnoredStations.Contains(location.Location.LocationPrimaryCode);

            // TODO správně by mělo stačit
            //bool doesNotStopHere = !ContainsAcitivity(location, TrainActivity.StopsForBoardingAndUnboarding) && !isFirstOrLastStation
            //    || Program.IgnoredStations.Contains(location.LocationPrimaryCode);
        }

        /// <summary>
        /// Místo vlaku odjíždí ze stanice náhradní doprava
        /// </summary>
        /// <param name="location">Zastavení ve stanici</param>
        public static bool IsAlternativeTransportOnDeparture(this CZPTTLocation location)
        {
            return location.NetworkSpecificParameter.FirstOrDefault(par => par.Name == "CZAlternativeTransport")?.Value == "1";
        }

        /// <summary>
        /// Nastaví vlastnost odjezdu jako náhradní dopravy
        /// </summary>
        /// <param name="location">Zastavení ve stanici</param>
        /// <param name="value">True, pokud místo vlaku ze stancie odjíždí náhradní doprava</param>
        public static void SetAlternativeTransportOnDeparture(this CZPTTLocation location, bool value)
        {
            var param = location.NetworkSpecificParameter.FirstOrDefault(par => par.Name == "CZAlternativeTransport");
            if (param != null)
            {
                param.Value = value ? "1" : "0";
            }
            else if (value)
            {
                location.NetworkSpecificParameter.Add(new NameAndValuePair()
                {
                    Name = "CZAlternativeTransport",
                    Value = "1",
                });
            }
        }

        /// <summary>
        /// Vrací true, pokud do vlaku lze v lokaci nastoupit
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static bool IsPublicOnDeparture(this CZPTTLocation location)
        {
            return CorrectionConfig.AcceptedTrafficTypes.Contains(location.TrafficType) && location.TrainType == 1;
        }

        /// <summary>
        /// Vrací true, pokud je stanice ve veřejné části vlaku (jde do vlaku nastoupit, anebo jde o výstupní stanici).
        /// </summary>
        /// <param name="prevLocation">Předchozí lokace</param>
        public static bool IsInPublicPart(this CZPTTLocation location, CZPTTLocation prevLocation)
        {
            return location.IsPublicOnDeparture() || (prevLocation != null && prevLocation.IsPublicOnDeparture());
        }

        /// <summary>
        /// Načte data o lokaci z databáze zastávek
        /// </summary>
        public static TrainStop GetAdditionalData(this CZPTTLocation location, StationDatabase stationDb)
        {
            return stationDb.FindInAswOrCis(location.Location.CountryCodeISO, location.Location.LocationPrimaryCode, location.Location.PrimaryLocationName);
        }
    }
}
