using CommonLibrary;
using CsvSerializer;
using CsvSerializer.Attributes;
using CzpttModel;
using GtfsLogging;
using GtfsModel.Extended;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainsEditor.GtfsExport;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Databáze vlakových stanic a zastávek
    /// </summary>
    class StationDatabase
    {
        private class SR70StationRaw
        {
            [CsvField("SR70", 1)]
            public int Number { get; set; }

            [CsvField("Název 20", 2)]
            public string Name { get; set; }

            [CsvField("GPS X", 16)]
            public string GpsX { get; set; }

            [CsvField("GPS Y", 17)]
            public string GpsY { get; set; }
        }

        /// <summary>
        /// Stanice a zastávky, které jsou v číselníku PID v ASW JŘ. Indexováno dvoumístným kódem státu + pětimístným CIS číslem (bez kontrolní číslice).
        /// </summary>
        public Dictionary<string, TrainStop> StopsFromAsw { get; private set; }

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
                return StopsFromAsw.Values.Where(s => s.IsUsed).Concat(AllStops.Values.Where(s => s.IsUsed)).Distinct();
            }
        }


        private static ICommonLogger log = Loggers.AswDataLoaderLoggerInstance;

        protected StationDatabase(Dictionary<string, TrainStop> stops, Dictionary<string, TrainStop> allStops)
        {
            StopsFromAsw = stops;
            AllStops = allStops;
        }

        /// <summary>
        /// Hledá zastávku primárně v <see cref="StopsFromAsw"/> a pokud tam není, vrací z <see cref="AllStops"/>. Pokud ani tam není, vytvoří záznam pouze s názvem.
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
        /// <param name="rewriteRules">Stanice uložené v číselníku ASW jinak, než jsou použity v datech SŽDC zde mohou mít uvedeny aliasy (stanice je pak v databázi pod všemi známými identifikátory)</param>
        /// <returns>Databáze s načtenými stanicemi</returns>
        public static StationDatabase CreateStationDb(AswModel.Extended.StopDatabase aswStops, string sr70fileName, IDictionary<int, int> rewriteRules)
        {
            // I. stanice a zastávky z číselníku ASW
            var trainStopsFromAsw = new Dictionary<string, TrainStop>();
            foreach (var aswStop in aswStops)
            {
                var aswStopFirstVersion = aswStop.FirstVersion();
                if (aswStopFirstVersion.StopId < 300 || aswStopFirstVersion.StopId >= 400)
                {
                    // není vlaková stanice
                    continue;
                }

                var cis = aswStopFirstVersion.CisNumber % 100000; // odstranit "úvodní" 54
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
                    ZoneIds = aswStopFirstVersion.Zones,
                    ZoneId = aswStopFirstVersion.PidZoneId,
                    ZoneRegionType = aswStopFirstVersion.ZoneRegionType,
                    WheelchairBoarding = FromAswWheelchairAccessibility(aswStopFirstVersion.WheelchairAccessibility),
                    PrimaryLocationCode = cis
                };

                var stopAlreadyPresent = trainStopsFromAsw.GetValueOrDefault(LocationIdent.CountryCodeCZ + cis);
                if (stopAlreadyPresent == null)
                {
                    trainStopsFromAsw.Add(LocationIdent.CountryCodeCZ + cis, trainStop);
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

            // Ia. rewrite pravidla (kde máme jiná CIS čísla)
            foreach (var rewriteRule in rewriteRules)
            {
                var knownStop = trainStopsFromAsw.GetValueOrDefault(LocationIdent.CountryCodeCZ + rewriteRule.Value);
                if (knownStop == null)
                {
                    log.Log(LogMessageType.WARNING_TRAIN_STOP_REWRITE_NOT_APPLIED, $"Stanice s CIS kódem {rewriteRule.Value} nebyla načtena. Pravidlo přepisu na {rewriteRule.Key} nebylo použito.");
                    continue;
                }

                if (trainStopsFromAsw.ContainsKey(LocationIdent.CountryCodeCZ + rewriteRule.Key))
                {
                    log.Log(LogMessageType.WARNING_TRAIN_STOP_REWRITE_NOT_APPLIED, $"Stanice s CIS kódem {rewriteRule.Key} již byla načtena. Pravidlo přepisu z {rewriteRule.Value} na {rewriteRule} nebylo použito.");
                    continue;
                }

                trainStopsFromAsw.Add(LocationIdent.CountryCodeCZ + rewriteRule.Key, knownStop);
            }

            // II. číselník SR 70
            var allTrainStops = CsvFileSerializer.DeserializeFile<SR70StationRaw>(sr70fileName, ';');
            var allTrainStopsDictionary = new Dictionary<string, TrainStop>();
            foreach (var trainStopRec in allTrainStops)
            {
                var cisId = trainStopRec.Number / 10;
                var stop = new TrainStop()
                {
                    GtfsId = $"T{cisId}",
                    Name = trainStopRec.Name,
                    Position = PositionFromString(trainStopRec.GpsX, trainStopRec.GpsY),
                    PrimaryLocationCode = cisId
                };

                // pokud už známe z ASW, použijeme tuto verzi
                if (trainStopsFromAsw.ContainsKey(LocationIdent.CountryCodeCZ +  cisId))
                {
                    stop = trainStopsFromAsw[LocationIdent.CountryCodeCZ + cisId];
                }

                if (stop.Position.GpsLatitude == 0 || stop.Position.GpsLongitude == 0)
                {
                    log.Log(LogMessageType.WARNING_TRAIN_STOP_ZERO_COORDINATES, $"Stanice/dopravní bod {trainStopRec.Name} (ID {cisId}) má nulovou některou ze souřadnic, nebude používána");
                }

                allTrainStopsDictionary.Add(LocationIdent.CountryCodeCZ + cisId, stop);
            }

            return new StationDatabase(trainStopsFromAsw, allTrainStopsDictionary);
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
    }
}
