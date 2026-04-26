using CommonLibrary;
using CommonLibrary.DotNet48;
using CsvSerializer;
using CsvSerializer.Attributes;
using CzpttModel;
using GtfsLogging;
using GtfsModel.Enumerations;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainsEditor.GtfsExport;
using TrainsEditor.SystemDescriptionModel;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Databáze vlakových stanic a zastávek
    /// </summary>
    class StationDatabase
    {
        private class SR70StationRaw
        {
            //[CsvField("SR70", 1)]
            [CsvField("Evidenční číslo", 1)]
            public int Number { get; set; }

            [CsvField("Název 20", 2)]
            public string Name { get; set; }

            //[CsvField("GPS X", 16)]
            [CsvField("GPS E (DMS)", 25)]
            public string GpsE { get; set; }

            //[CsvField("GPS Y", 17)]
            [CsvField("GPS N (DMS)", 24)]
            public string GpsN { get; set; }
        }

        /// <summary>
        /// Stanice a zastávky, které jsou v číselníku PID v ASW JŘ. Indexováno dvoumístným kódem státu + pětimístným CIS číslem (bez kontrolní číslice).
        /// </summary>
        public Dictionary<string, TrainStop> StopsFromSystem { get; private set; }

        /// <summary>
        /// Všechny stanice a zastávky z číselníku SR 70 SŽDC. Indexováno dvoumístným kódem státu + pětimístným CIS číslem (bez kontrolní číslice).
        /// </summary>
        public Dictionary<string, TrainStop> AllStops { get; private set; }

        /// <summary>
        /// Všechny zastávky, které byly použity
        /// </summary>
        public IEnumerable<TrainStop> UsedStops
        {
            get
            {
                // ten Distinct() tam musí být, protože některé zastávky referencujeme z více IDček
                // nepoužívá se AllStops, protože takhle je budeme mít správně seřazené (nejdřív ASW, pak ostatní)
                return StopsFromSystem.Values.Where(s => s.IsUsed).Concat(AllStops.Values.Where(s => s.IsUsed)).Distinct();
            }
        }


        private static ICommonLogger log = Loggers.SystemDataLoaderLoggerInstance;

        protected StationDatabase(Dictionary<string, TrainStop> stops, Dictionary<string, TrainStop> allStops)
        {
            StopsFromSystem = stops;
            AllStops = allStops;
        }

        /// <summary>
        /// Hledá zastávku primárně v <see cref="StopsFromSystem"/> a pokud tam není, vrací z <see cref="AllStops"/>. Pokud ani tam není, vytvoří záznam pouze s názvem.
        /// </summary>
        /// <param name="countryCode">Kód státu (pro CZ použít <see cref="LocationIdent.CountryCodeCZ"/>)</param>
        /// <param name="cisId">Pětimístné číslo CIS (bez kontrolky)</param>
        /// <param name="locationName">Název lokace (pro případ, že nebude v seznamu, abychom mohli přidat).</param>
        public TrainStop FindInAswOrCis(string countryCode, int cisId, string locationName)
        {
            var result = AllStops.GetValueOrDefault(countryCode + cisId);
            if (result == null)
            {
                result = new TrainStop()
                {
                    Name = locationName,
                    PrimaryLocationCode = cisId,
                };

                AllStops.Add(countryCode + cisId, result);
            }

            return result;
        }

        /// <summary>
        /// Sestaví databázi. Načte zastávky z ASW i z číselníku SŽDC.
        /// </summary>
        /// <param name="aswStops">Obsah číselníku zastávek z ASW</param>
        /// <param name="sr70fileName">Soubor CSV obsahující číselník SR 70 SŽDC</param>
        /// <returns>Databáze s načtenými stanicemi</returns>
        public static StationDatabase CreateStationDb(AswModel.Extended.StopDatabase aswStops, string sr70fileName)
        {
            // I. číselník SR 70
            var allTrainStopsDictionary = LoadSR70Data(sr70fileName);

            // II. stanice a zastávky z číselníku ASW
            var trainStopsFromAsw = new Dictionary<string, TrainStop>();
            foreach (var aswStop in aswStops)
            {
                var aswStopFirstVersion = aswStop.FirstVersion();
                if (aswStopFirstVersion.StopId < 300 || aswStopFirstVersion.StopId >= 400)
                {
                    // není vlaková stanice
                    continue;
                }

                var (countryCode, stationNumber) = TranslateCisNumber(aswStopFirstVersion.CisNumber);
                var trainStop = new TrainStop()
                {
                    AswNodeId = aswStopFirstVersion.NodeId,
                    AswStopId = aswStopFirstVersion.StopId,
                    GtfsId = $"U{aswStopFirstVersion.NodeId}Z{aswStopFirstVersion.StopId}",
                    Name = aswStopFirstVersion.Name,
                    Position = new GpsCoordinates()
                    {
                        GpsLatitude = aswStopFirstVersion.Position.GpsLatitude,
                        GpsLongitude = aswStopFirstVersion.Position.GpsLongitude,
                    },
                    ZoneId = aswStopFirstVersion.PidZoneId,
                    ZoneRegionType = aswStopFirstVersion.ZoneRegionType,
                    WheelchairBoarding = FromAswWheelchairAccessibility(aswStopFirstVersion.WheelchairAccessibility),
                    PrimaryLocationCode = stationNumber,
                    AllTransferIcons = GetTransferIcons(aswStopFirstVersion.TransferAttributes).ToArray(),
                    IsIntegrated = true
                };

                var stopAlreadyPresent = trainStopsFromAsw.GetValueOrDefault(countryCode + stationNumber);
                if (stopAlreadyPresent == null)
                {
                    trainStopsFromAsw.Add(countryCode + stationNumber, trainStop);
                }
                else
                {
                    if (stopAlreadyPresent.AswNodeId != trainStop.AswNodeId || stopAlreadyPresent.Name != trainStop.Name || stopAlreadyPresent.ZoneId != trainStop.ZoneId
                        || !stopAlreadyPresent.Position.Equals(trainStop.Position))
                    {
                        log.Log(LogMessageType.WARNING_TRAIN_STOP_CONFLICT, $"Stanice {trainStop.Name} - konfliktní záznamy (liší se název, uzel, pásmo nebo pozice)");
                    }
                }
            }

            // data načtená z ASW přebíjí data ze SR70
            foreach (var aswStopRecord in trainStopsFromAsw)
            {
                if (allTrainStopsDictionary.ContainsKey(aswStopRecord.Key))
                {
                    allTrainStopsDictionary[aswStopRecord.Key] = aswStopRecord.Value;
                }

            }

            return new StationDatabase(trainStopsFromAsw, allTrainStopsDictionary);
        }


        /// <summary>
        /// Sestaví databázi. Načte zastávky z konfiguračního souboru i z číselníku SŽDC.
        /// </summary>
        /// <param name="aswStops">Data zastávek</param>
        /// <param name="sr70fileName">Soubor CSV obsahující číselník SR 70 SŽDC</param>
        /// <returns>Databáze s načtenými stanicemi</returns>
        public static StationDatabase CreateStationDb(IEnumerable<Station> stationData, string sr70fileName)
        {
            // I. číselník SR 70
            var allTrainStopsDictionary = LoadSR70Data(sr70fileName);

            // II. stanice a zastávky z číselníku ASW
            var trainStopsFromStationData = new Dictionary<string, TrainStop>();
            foreach (var station in stationData)
            {
                var (countryCode, stationNumber) = TranslateCisNumber(station.CisNumber);
                var recordFromSR70 = allTrainStopsDictionary.GetValueOrDefault(countryCode + stationNumber);
                var hasPosition = station.GpsLatitude != 0 && station.GpsLongitude != 0;
                if (recordFromSR70 == null && (!hasPosition || string.IsNullOrEmpty(station.Name)))
                {
                    log.Log(LogMessageType.WARNING_TRAIN_STOP_MISSING_DATA, $"Záznam o zastávce {countryCode + stationNumber} nemá název nebo pozici a nelze je doplnit ze SR70, protože tam zastávka není");
                    continue;
                }

                var trainStop = new TrainStop()
                {
                    GtfsId = station.CisNumber.ToString(),
                    Name = station.Name ?? recordFromSR70.Name,
                    Position = hasPosition ? new GpsCoordinates()
                    {
                        GpsLatitude = station.GpsLatitude,
                        GpsLongitude = station.GpsLongitude,
                    } : recordFromSR70.Position,
                    ZoneId = station.Zones,
                    WheelchairBoarding = station.WheelchairAccessible ? WheelchairBoarding.Possible : station.WheelchairAccessibilityNotSet ? WheelchairBoarding.Unknown : WheelchairBoarding.Unknown,
                    PrimaryLocationCode = stationNumber,
                    CisId = station.CisNumber,
                    IsIntegrated = true
                };

                var stopAlreadyPresent = trainStopsFromStationData.GetValueOrDefault(countryCode + stationNumber);
                if (stopAlreadyPresent == null)
                {
                    trainStopsFromStationData.Add(countryCode + stationNumber, trainStop);
                    allTrainStopsDictionary[countryCode + stationNumber] = trainStop;
                }
                else
                {
                    if (stopAlreadyPresent.PrimaryLocationCode != trainStop.PrimaryLocationCode || stopAlreadyPresent.Name != trainStop.Name || stopAlreadyPresent.ZoneId != trainStop.ZoneId
                        || !stopAlreadyPresent.Position.Equals(trainStop.Position))
                    {
                        log.Log(LogMessageType.WARNING_TRAIN_STOP_CONFLICT, $"Stanice {trainStop.Name} - konfliktní záznamy (liší se název, uzel, pásmo nebo pozice)");
                    }
                }
            }

            return new StationDatabase(trainStopsFromStationData, allTrainStopsDictionary);
        }

        /// <summary>
        /// Přeloží CIS číslo na ISO kód státu a číslo stanice
        /// </summary>
        /// <param name="cis">CIS číslo zastávky</param>
        public static (string countryCode, int stationNumber) TranslateCisNumber(int cis)
        {
            var countryNumber = cis / 100000;
            return (
                countryNumber == 54 ? LocationIdent.CountryCodeCZ : countryNumber == 51 ? LocationIdent.CountryCodePL : "",
                cis % 100000
                );
        }

        /// <summary>
        /// Aplikuje pravidla přepisu, když SŽ někde používá jiný čísla, než očekáváme
        /// </summary>
        /// <param name="rewriteRules">Stanice uložené v číselníku ASW jinak, než jsou použity v datech SŽDC zde mohou mít uvedeny aliasy (stanice je pak v databázi pod všemi známými identifikátory)</param>
        public void ApplyRewriteRules(IDictionary<string, string> rewriteRules)
        {
            // IIa. rewrite pravidla (kde máme jiná CIS čísla)
            foreach (var rewriteRule in rewriteRules)
            {
                var knownStop = AllStops.GetValueOrDefault(rewriteRule.Value);
                if (knownStop == null)
                {
                    log.Log(LogMessageType.WARNING_TRAIN_STOP_REWRITE_NOT_APPLIED, $"Stanice s kódem {rewriteRule.Value} nebyla načtena. Pravidlo přepisu na {rewriteRule.Key} nebylo použito.");
                    continue;
                }

                if (StopsFromSystem.ContainsKey(rewriteRule.Key))
                {
                    log.Log(LogMessageType.WARNING_TRAIN_STOP_REWRITE_NOT_APPLIED, $"Stanice s kódem {rewriteRule.Key} již byla načtena. Pravidlo přepisu z {rewriteRule.Value} na {rewriteRule.Key} nebylo použito.");
                    continue;
                }

                StopsFromSystem.Add(rewriteRule.Key, knownStop);
                AllStops[rewriteRule.Key] = knownStop;
            }
        }

        private static Dictionary<string, TrainStop> LoadSR70Data(string sr70fileName)
        {
            var allTrainStops = CsvFileSerializer.DeserializeFile<SR70StationRaw>(sr70fileName, ';');
            var allTrainStopsDictionary = new Dictionary<string, TrainStop>();
            foreach (var trainStopRec in allTrainStops)
            {
                var cisId = trainStopRec.Number / 10;
                var stop = new TrainStop()
                {
                    GtfsId = $"T{cisId}",
                    Name = trainStopRec.Name,
                    Position = PositionFromString(trainStopRec.GpsE, trainStopRec.GpsN),
                    PrimaryLocationCode = cisId
                };

                if (stop.Position.GpsLatitude == 0 || stop.Position.GpsLongitude == 0)
                {
                    log.Log(LogMessageType.WARNING_TRAIN_STOP_ZERO_COORDINATES, $"Stanice/dopravní bod {trainStopRec.Name} (ID {cisId}) má nulovou některou ze souřadnic, nebude používána");
                }

                allTrainStopsDictionary.Add(LocationIdent.CountryCodeCZ + cisId, stop);
            }

            return allTrainStopsDictionary;
        }

        private static GpsCoordinates PositionFromString(string lon, string lat)
        {
            return new GpsCoordinates()
            {
                GpsLatitude = PositionFromStringSingleCoordinate(lat),
                GpsLongitude = PositionFromStringSingleCoordinate(lon),
            };
        }

        private static readonly CultureInfo czCulture = CultureInfo.GetCultureInfo(1029); // česká kultura, protože to číslo obsahuje čárky TODO předělat na invariantní?

        // parsuje ohavný ČD stringy z formátu N48°01'13,1  "
        private static double PositionFromStringSingleCoordinate(string str)
        {
            // ty Replace zařizují, že když je někde vynechaná číslice, znamená to nulu (aby se to pak zparsovalo)
            var numbers = str.Substring(1).Split(new[] { '°', '\'', '"' });
            return ParseIntAllowEmptyString(numbers[0]) + ParseIntAllowEmptyString(numbers[1]) / 60.0 + double.Parse(numbers[2], czCulture) / 3600.0;
        }

        // parse intu, co podporuje i empty/whitespace string, jež považuje za nulu
        private static int ParseIntAllowEmptyString(string numstr)
        {
            if (string.IsNullOrWhiteSpace(numstr))
            {
                return 0;
            }
            else
            {
                return int.Parse(numstr);
            }
        }

        // Transformace mezi ASW a GTFS popisem bezbariérovosti
        private static GtfsModel.Enumerations.WheelchairBoarding FromAswWheelchairAccessibility(AswModel.Extended.WheelchairAccessibility wheelchairAccessibility)
        {
            return wheelchairAccessibility == AswModel.Extended.WheelchairAccessibility.Accessible ? GtfsModel.Enumerations.WheelchairBoarding.Possible
                : wheelchairAccessibility == AswModel.Extended.WheelchairAccessibility.Undefined ? GtfsModel.Enumerations.WheelchairBoarding.Unknown
                : GtfsModel.Enumerations.WheelchairBoarding.NotPossible;
        }

        // převod přestupních ikonek z ASW do GTFS modelu
        private static IEnumerable<TransferIcons> GetTransferIcons(AswModel.Extended.TransferAttributes aswTransferAttributes)
        {
            if (aswTransferAttributes.IsTransferToMetroA) yield return TransferIcons.MetroA;
            if (aswTransferAttributes.IsTransferToMetroB) yield return TransferIcons.MetroB;
            if (aswTransferAttributes.IsTransferToMetroC) yield return TransferIcons.MetroC;
            if (aswTransferAttributes.IsTransferToMetroD) yield return TransferIcons.MetroD;
            if (aswTransferAttributes.IsTransferToTrain) yield return TransferIcons.Train;
            if (aswTransferAttributes.IsTransferToSbahn) yield return TransferIcons.Sbahn;
            if (aswTransferAttributes.IsTransferToFunicular) yield return TransferIcons.Funicular;
            if (aswTransferAttributes.IsTransferToFerry) yield return TransferIcons.Ferry;
            if (aswTransferAttributes.IsTransferToAirport) yield return TransferIcons.Airport;
            if (aswTransferAttributes.IsTransferToTram) yield return TransferIcons.Tramway;
            if (aswTransferAttributes.IsTransferToTrolleybus) yield return TransferIcons.Trolleybus;
            if (aswTransferAttributes.IsTransferToBus) yield return TransferIcons.Bus;
        }
    }
}
