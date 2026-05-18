using CommonLibrary;
using GtfsLogging;
using JdfModel;
using System.Globalization;
using System.Text;

namespace JdfToGtfsProcessor.Stops
{
    /// <summary>
    /// Komplet databáze GTFS zastávek. Na začátku načte data, pak jak chodí stop times se ještě zpřesňuje.
    /// 
    /// Myšlenka je, že máme pro každou kombinaci CIS číslo + nástupiště + zóny vlastní GTFS zastávku. Zóny zjistíme
    /// až při načítání zastavení, takže si děláme jen factory pro zastávky a pak jak chodí zóny, vytváříme instance
    /// </summary>
    internal class StopDatabase
    {
        // indexováno číslem zastávky a nástupištěm
        private Dictionary<int, Dictionary<string, StopData>> stopDataDictionary;

        // indexováno číslem zastávky a nástupištěm - může být více záznamů lišící se tarifním pásmem
        public Dictionary<int, StopCollectionForCisNumber> StopsToGtfsMapping;

        public HashSet<int> IgnoredStopsDueToError;

        public StopDatabase(Dictionary<int, Dictionary<string, StopData>> stopDataDictionary)
        {
            this.stopDataDictionary = stopDataDictionary;
            StopsToGtfsMapping = new Dictionary<int, StopCollectionForCisNumber>();
            IgnoredStopsDueToError = new HashSet<int>();
        }

        /// <summary>
        /// Načte data o zastávkách a obohatí je o nástupiště, jež už zná z konstruktoru
        /// </summary>
        /// <param name="stops"></param>
        public void CreateStopDatabase(IEnumerable<Stop> stops, DateTime feedStartDate, ISimpleLogger log)
        {
            foreach (var stop in stops)
            {
                if (stop.NamePart3 == "CLO")
                {
                    // virtuální hraniční přechod, ignorujeme
                    continue;
                }

                if (StopsToGtfsMapping.ContainsKey(stop.StopId))
                {
                    if (StopsToGtfsMapping[stop.StopId].StopName != stop.StopName)
                    {
                        if (feedStartDate <= DateTime.Now.Date)
                        {
                            log.Log($"Zastávka {stop} má jiný název, než dříve uložený název {StopsToGtfsMapping[stop.StopId].StopName}. Používám nový název, protože feed už platí (nebo platil v minulosti).");
                            StopsToGtfsMapping[stop.StopId].StopName = stop.StopName;
                        }
                        else
                        {
                            log.Log($"Zastávka {stop} má jiný název, než dříve uložený název {StopsToGtfsMapping[stop.StopId].StopName}. Nový název bude ignorován, protože platnost JDF je až od {feedStartDate}.");
                        }
                    }

                    continue;
                }

                if (stopDataDictionary.TryGetValue(stop.StopId, out var thisStopDataForAllPlatforms))
                {
                    if (!thisStopDataForAllPlatforms.Any())
                    {
                        log.Log($"Zastávka {stop} nemá v databázi zastávek ZAST_ODIS.xml žádná nástupiště. Nejde tedy zjistit její stanoviště a bez toho nemůže být zařazena do GTFS, proto bude přeskočena.");
                        IgnoredStopsDueToError.Add(stop.StopId);
                        continue;
                    }

                    var avgPosition = new GpsCoordinates()
                    {
                        // Average je safe, výše ověřujeme, že je přítomno alespoň 1 nástupiště
                        GpsLatitude = thisStopDataForAllPlatforms.Values.Select(pl => pl.Coordinates.GpsLatitude).Average(),
                        GpsLongitude = thisStopDataForAllPlatforms.Values.Select(pl => pl.Coordinates.GpsLongitude).Average(),
                    };

                    var gtfsStopsForCis = new StopCollectionForCisNumber(stop.StopName, new StopPlatform(new GtfsStopFactory
                    {
                        GtfsId = stop.StopId.ToString(),
                        Name = stop.StopName,
                        CisId = stop.StopId,
                        Position = avgPosition
                    }));
                    StopsToGtfsMapping.Add(stop.StopId, gtfsStopsForCis);

                    foreach (var thisStopData in thisStopDataForAllPlatforms.Values)
                    {
                        var stopId = stop.StopId.ToString() + "_" + RemoveDiacritics(thisStopData.PlatformCode);
                        gtfsStopsForCis.AllPlatforms[thisStopData.PlatformCode] = new StopPlatform(new GtfsStopFactory
                        {
                            GtfsId = stopId,
                            Name = stop.StopName,
                            CisId = stop.StopId,
                            Position = new GpsCoordinates()
                            {
                                GpsLatitude = thisStopData.Coordinates.GpsLatitude,
                                GpsLongitude = thisStopData.Coordinates.GpsLongitude,
                            },
                            PlatformCode = thisStopData.PlatformCode
                        });
                    }
                }
                else
                {
                    log.Log($"Zastávka {stop} nebyla nalezena v databázi zastávek ZAST_ODIS.xml. Bez GPS souřadnice nemůže být zařazena do GTFS, proto bude přeskočena.");
                    IgnoredStopsDueToError.Add(stop.StopId);
                }
            }
        }

        /// <summary>
        /// Zajistí záznam pro daný stop time. Dohledá zastávku podle CIS čísla a nástupiště. Pokud není zastávka pro CIS číslo, vrátí null.
        /// Pokud je zastávka pro CIS číslo, ale není pro konkrétní nástupiště, vrátí univerzální zastávku pro "CIS uzel".
        /// Pokud má spoj v zastávce jinou zónu, než měly předchozí, vytvoří i speciální verzi zastávky s danou zónou
        /// </summary>
        /// <param name="stopTime">Zastavení JDF</param>
        /// <param name="zone">Zóny spoje v zastávce</param>
        /// <returns>GTFS stop na míru nebo null, pokud nešlo najít</returns>
        public GtfsModel.Extended.Stop? GetStopForStopTime(StopTime stopTime, string zone, ISimpleLogger missingPlatformCodeLog)
        {
            var stopsForCis = StopsToGtfsMapping.GetValueOrDefault(stopTime.StopId);
            if (stopsForCis == null)
            {
                return null;
            }

            var platform = stopsForCis.AllPlatforms.GetValueOrDefault(stopTime.PlatformCode);
            if (platform == null)
            {
                if (!stopsForCis.UniversalPlatformWasUsed)
                {
                    // chybu vypisujeme jen při prvním výskytu
                    missingPlatformCodeLog.Log($"Zastavení {stopTime} - nenalezena GTFS zastávka (chybí stanoviště '{stopTime.PlatformCode}'). Používám obecnou zastávku {stopTime.StopId} bez stanoviště.");
                }
                
                platform = stopsForCis.UniversalPlatform;
                stopsForCis.UniversalPlatformWasUsed = true;
            }

            return platform.GetStopForZone(zone);
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
