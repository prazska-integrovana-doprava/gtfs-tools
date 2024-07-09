using AswModel;
using CommonLibrary;
using GtfsLogging;
using GtfsModel.Enumerations;
using GtfsModel.Functions;
using JR_XML_EXP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace StopProcessor
{
    class Program
    {
        // zastávky vlaků, kterém má být přidán přídomek "(vlak)".
        // POZOR, je potřeba ručně synchronizovat se souborem Zmeny.xml
        static string[] conflictedTrainStopNames = new[] { "Hostivice", "Chýně", "Hovorčovice", "Zeleneč", "Dřísy", "Středokluky", "Čisovice", "Měchenice",
            "Světice", "Kamenný Přívoz", "Jeneč", "Bečváry", "Tuklaty", "Klučov", "Úžice", "Loděnice", "Rostoklaty", "Tatce", "Ostrá", "Stratov", "Sadská",
            "Dobrovíz", "Zlonín", "Tišice", "Všetaty", "Úholičky", "Nelahozeves", "Vraňany", "Klínec", "Malá Hraštice", "Mokrovraty", "Krhanice", "Podlešín",
            "Kamenné Zboží", "Brandýsek", "Dřetovice", "Zákolany", "Malý Újezd", "Neuměřice", "Olovnice", "Ovčáry", "Vrbčany", "Zvoleněves", "Žabonosy",
            "Byšice", "Cítov", "Dolní Beřkovice", "Horní Počaply", "Všejany", "Liběchov", "Velký Borek", "Chocerady", "Chvatěruby", "Kouřim", "Lázně Toušeň",
            "Chlumín", "Čachovice", "Kanina", "Mšeno", "Mochov", "Luštěnice", "Jíkev", "Oskořínek", "Bratkovice", "Dlouhá Lhota", "Nová Ves pod Pleší", "Cerhenice"};
        // u Nové Vsi pod Pleší a Cerhenic je zvláštnost, že jako jediná je opravdu v jednom uzlu s tou autobusovou zastávkou a dá i smysl tam přestupovat

        static ICommonLogger stopLog;
        static ICommonLogger stopGroupLog;

        static void Main(string[] args)
        {
            var config = Configuration.Load(args);
            InitLoggers(config.LogFolder);
            Console.WriteLine($"Načítání souboru: {config.FullNamesTranslationsFile} ..");
            var fullNamesProvider = new FullNamesProvider();
            fullNamesProvider.Load(config.FullNamesTranslationsFile);

            Console.WriteLine($"Načítání souboru: {config.StopsFile} ..");
            DavkaJR xmlStopList;
            var db = LoadXmlStopData(config, fullNamesProvider, out xmlStopList);
            Console.WriteLine($"Načítání GTFS ze složky: {config.GtfsFolder} ..");
            LoadGtfsData(db, config);
            Console.WriteLine($"Třídění dle názvů ..");
            db.InitStopsByName();
            FillStopTrafficTypes(db);
            Console.WriteLine($"Kontrola ..");
            FillAndCheckStopNames(db, fullNamesProvider);
            CheckIdosNamesAndCisAreUnique(db);

            Console.WriteLine($"Generování mapování OIS ..");
            var oisMapping = CreateOisMapping(db);

            Console.WriteLine($"Ukládání souborů ..");
            var stopsSorted = db.ToList();
            stopsSorted.Sort((first, second) => first.UniqueName.CompareTo(second.UniqueName));
            var stopsByNameWithMetadata = StopsByNameWithMetadata.FromStopList(stopsSorted);
            SaveXml(Path.Combine(config.StopsOutputFolder, "StopsByName.xml"), stopsByNameWithMetadata);
            SaveJson(Path.Combine(config.StopsOutputFolder, "stops.json"), stopsByNameWithMetadata, true);
            SaveJson(Path.Combine(config.StopsOutputFolder, "stops.min.json"), stopsByNameWithMetadata, false);

            // TODO zrušit, smazat zničit včetně proměnné xmlStopList a vlastnosti NazevUnikatni a XmlStop třídy Zastavka
            // (při načítání dat se v xmlStopList vyplní UniqueName, který se musí uložit)
            xmlStopList.Zastavky.RemoveAll(s => !s.IsUsed);
            RowBase.Serialize(xmlStopList, true, Path.Combine(config.StopsOutputFolder, "Zastavky.xml"));
                        
            SaveJson(Path.Combine(config.StopsOutputFolder, "oisMapping.json"), oisMapping, true);

            CloseLoggers();
        }

        private static void InitLoggers(string logFolder)
        {
            var logFactory = new LogWriterFactory(logFolder);
            stopLog = new CommonLogger(logFactory.CreateWriterToFile("StopProcessor_Common"));
            stopGroupLog = new CommonLogger(logFactory.CreateWriterToFile("StopProcessor_Groups"));
        }

        private static void CloseLoggers()
        {
            stopLog.Close();
            stopGroupLog.Close();
        }

        // načte data z GTFS a XML
        private static StopDatabase LoadXmlStopData(Configuration config, FullNamesProvider fullNamesProvider, out DavkaJR xmlStopList)
        {
            // 1. načtení dat o zastávkách z XML
            xmlStopList = AswXmlSerializer.Deserialize(config.StopsFile);
            var stopDb = new StopDatabase();
            foreach (var xmlStop in xmlStopList.Zastavky)
            {
                var stop = new Stop()
                {
                    NodeId = xmlStop.CUzlu,
                    StopId = xmlStop.CZast,
                    PlatformCode = xmlStop.Stanoviste ?? "",
                    Name2 = xmlStop.Nazev2,
                    IdosName = xmlStop.Nazev7,
                    IdosCategoryNumber = xmlStop.kIDOS,
                    GpsLatitude = xmlStop.Lat,
                    GpsLongitude = xmlStop.Lng,
                    SjtskX = (float)xmlStop.sX,
                    SjtskY = (float)xmlStop.sY,
                    ZoneId = xmlStop.TarifniPasma ?? "",
                    CisNumber = xmlStop.CisloCIS,
                    DistrictCode = xmlStop.SPZ ?? "",
                    Municipality = xmlStop.NazevObce ?? "",
                    IsPublic = xmlStop.Verejna,
                    WheelchairBoarding = xmlStop.BezbarierovostNestanovena ? WheelchairBoarding.Unknown : (xmlStop.Bezbarierova ? WheelchairBoarding.Possible : WheelchairBoarding.NotPossible),
                    XmlStop = xmlStop
                };

                if (conflictedTrainStopNames.Contains(stop.Name2) && stop.IsTrain)
                {
                    xmlStop.NazevUnikatni = $"{stop.IdosName} (vlak)";
                }
                else
                {
                    xmlStop.NazevUnikatni = stop.IdosName;
                }
                
                if (!stopDb.AddStop(stop))
                {
                    stopLog.Log(LogMessageType.INFO_STOP_DUPLICATE, $"Zastávka {stop} již v databázi je, pravděpodobně jde o novější verzi");
                }
            }

            return stopDb;
        }

        public static void LoadGtfsData(StopDatabase stopDb, Configuration config)
        {
            // 2. načtení zastávek z GTFS a vytvoření mapy GTFS ID -> XML ID
            var gtfsFeed = GtfsFeedSerializer.DeserializeFeed(config.GtfsFolder);
            foreach (var gtfsStop in gtfsFeed.Stops)
            {
                if (gtfsStop.LocationType != LocationType.Stop || gtfsStop.Id.StartsWith("T"))
                    continue; // není zastávka, anebo je vlaková, kterou ale nemáme v ASW

                int nodeId = gtfsStop.AswNodeId;
                int stopId = gtfsStop.AswStopId;
                if (nodeId != 0 && stopId != 0)
                {
                    if (gtfsStop.ZoneId == "-" && stopId >= 300)
                        continue; // vlaková zastávka bez pásma, ty ignorujeme (ale autobusové naopak necháváme - mezikrajská linka)

                    var stop = stopDb.FindStop(nodeId, stopId);
                    if (stop != null)
                    {
                        stop.IsUsed = true;
                        stop.XmlStop.IsUsed = true;
                        stop.GtfsIds.Add(gtfsStop.Id);
                        stopDb.StopsByGtfsId.Add(gtfsStop.Id, stop);

                        if (stop.CisNumber == 0)
                        {
                            stopLog.Log(LogMessageType.WARNING_STOP_ZERO_CIS, $"Zastávka {stop} má nulové číslo CIS, přestože je veřejná a použitá ({gtfsStop.Id}).");
                        }
                    }
                    else
                    {
                        stopLog.Log(LogMessageType.ERROR_STOP_MISSING_IN_GTFS, $"Zastávka {gtfsStop.Id} (-> {nodeId}/{stopId}) - není v XML");
                    }
                }
                else
                {
                    stopLog.Log(LogMessageType.ERROR_STOP_WRONG_GTFS_ID, $"Zastávka {gtfsStop.Id} - chyba parsování ID");
                }
            }
        
            // 3. načtení spojů a jejich zastavení a přidání odjezdů k jednotlivým zastávkám (pro určení projíždějících linek)
            var routes = gtfsFeed.Routes.ToDictionary(route => route.Id);
            var trips = gtfsFeed.Trips.ToDictionary(trip => trip.Id);
            var calendars = gtfsFeed.Calendar.ToDictionary(cal => cal.Id);
            foreach (var gtfsTripStopTimes in gtfsFeed.StopTimes.GroupBy(st => st.TripId))
            {
                var gtfsTripStopTimesArray = gtfsTripStopTimes.ToArray();
                foreach (var gtfsStopTime in gtfsTripStopTimesArray.Take(gtfsTripStopTimesArray.Length - 1).Where(
                    st => st.DropOffType != DropOffType.None))
                {
                    Stop stop;
                    if (!stopDb.StopsByGtfsId.TryGetValue(gtfsStopTime.StopId, out stop))
                    {
                        continue; // nebyla načtena (těžko říct proč, ale už by to mělo být reportováno
                    }

                    var gtfsTrip = trips[gtfsStopTime.TripId];
                    if (gtfsTrip.IsExceptional != 0)
                        continue; // výjezdy, zátahy a přejezdy neberem

                    var gtfsCalendar = calendars[gtfsTrip.ServiceId];
                    if ((gtfsCalendar.StartDate - DateTime.Now.Date).Days > 3)
                        continue; // spoj jede až moc za dlouho
                        // NOTE: v XML budou všechny zastávky, které se vyskytují v GTFS feedu (tedy i ty platné za více než 3 dny - tyto pouze nebudou mít vyplněné linky)
                        // (takto se to načítá ve fázi 2 výše)

                    var gtfsRoute = routes[gtfsTrip.RouteId];
                    var lineNumber = ParseId(gtfsRoute.Id, 'L');
                    if (lineNumber == 0)
                    {
                        stopLog.Log(LogMessageType.ERROR_ROUTE_WRONG_GTFS_ID, $"Linka {gtfsRoute.Id} - chyba parsování ID");
                        continue;
                    }

                    var passingRouteRecord = stop.PassingRoutes.GetValueOrDefault(lineNumber)?.GetValueOrDefault((int)gtfsTrip.DirectionId);
                    if (passingRouteRecord == null)
                    {
                        passingRouteRecord = new PassingRoute(lineNumber, (int) gtfsTrip.DirectionId, gtfsRoute);
                        stop.PassingRoutes.GetValueAndAddIfMissing(passingRouteRecord.LineNumber, new Dictionary<int, PassingRoute>())
                            .Add((int)gtfsTrip.DirectionId, passingRouteRecord);
                    }

                    if (!string.IsNullOrEmpty(gtfsStopTime.StopHeadsign))
                    {
                        passingRouteRecord.AddHeadsign(gtfsStopTime.StopHeadsign);
                    }
                    else
                    {
                        passingRouteRecord.AddHeadsign(gtfsTrip.Headsign);
                    }
                }
            }
        }

        private static void FillStopTrafficTypes(StopDatabase db)
        {
            // 4. propagace druhů dopravy projíždějících linek na zastávky a skupiny
            foreach (var stopGroup in db)
            {
                var groupTypes = new HashSet<TrafficType>();
                var groupMetroRoutes = new HashSet<string>();

                foreach (var stop in stopGroup.Stops)
                {
                    var stopTypes = new HashSet<TrafficType>();
                    var stopMetroRoutes = new HashSet<string>();
                    foreach (var passingRoute in stop.PassingRoutesFlat)
                    {
                        stopTypes.Add(passingRoute.Type);
                        groupTypes.Add(passingRoute.Type);
                        if (passingRoute.Type == TrafficType.Metro)
                        {
                            stopMetroRoutes.Add(passingRoute.Name);
                            groupMetroRoutes.Add(passingRoute.Name);
                        }
                    }

                    stop.MainTrafficType = SelectMainTrafficType(stopTypes, stopMetroRoutes);
                }

                stopGroup.MainTrafficType = SelectMainTrafficType(groupTypes, groupMetroRoutes);
            }
        }

        private static TrafficTypeExtended SelectMainTrafficType(HashSet<TrafficType> trafficTypes, HashSet<string> metroRoutes)
        {
            if (trafficTypes.Contains(TrafficType.Metro))
            {
                if (metroRoutes.Contains("A") && metroRoutes.Contains("B"))
                {
                    return TrafficTypeExtended.MetroAB;
                }
                else if (metroRoutes.Contains("B") && metroRoutes.Contains("C"))
                {
                    return TrafficTypeExtended.MetroBC;
                }
                else if (metroRoutes.Contains("A") && metroRoutes.Contains("C"))
                {
                    return TrafficTypeExtended.MetroAC;
                }
                else if (metroRoutes.Contains("B"))
                {
                    return TrafficTypeExtended.MetroB;
                }
                else if (metroRoutes.Contains("C"))
                {
                    return TrafficTypeExtended.MetroC;
                }
                else
                {
                    return TrafficTypeExtended.MetroA;
                }
            }
            else if (trafficTypes.Contains(TrafficType.Tram))
            {
                return TrafficTypeExtended.Tram;
            }
            else if (trafficTypes.Contains(TrafficType.Rail))
            {
                return TrafficTypeExtended.Train;
            }
            else if (trafficTypes.Contains(TrafficType.Trolleybus))
            {
                return TrafficTypeExtended.Trolleybus;
            }
            else if (trafficTypes.Contains(TrafficType.Bus))
            {
                return TrafficTypeExtended.Bus;
            }
            else if (trafficTypes.Contains(TrafficType.Funicular))
            {
                return TrafficTypeExtended.Funicular;
            }
            else if (trafficTypes.Contains(TrafficType.Ferry))
            {
                return TrafficTypeExtended.Ferry;
            }
            else
            {
                return TrafficTypeExtended.Undefined;
            }
        }

        private static IEnumerable<OisMappingRecord> CreateOisMapping(StopDatabase db)
        {
            foreach (var stopGroup in db)
            {
                var oisNumbers = new HashSet<int>();

                foreach (var stop in stopGroup.Stops)
                {
                    if (stop.PassingRoutesFlat.Any(r => r.Type == TrafficType.Tram) && stop.XmlStop.CisloOIS != stopGroup.NodeNumber)
                    {
                        oisNumbers.Add(stop.XmlStop.CisloOIS);
                    }
                }

                foreach (var oisNumber in oisNumbers)
                {
                    yield return new OisMappingRecord()
                    {
                        OisNumber = oisNumber,
                        NodeNumber = stopGroup.NodeNumber,
                        StopName = stopGroup.Name2,
                    };
                }
            }
        }

        private static int ParseId(string gtfsStopId, char before)
        {
            var substr = new string(gtfsStopId.SkipWhile(ch => ch != before).Skip(1).TakeWhile(ch => char.IsDigit(ch)).ToArray());
            int result;
            int.TryParse(substr, out result);
            return result;
        }

        private static void SaveXml(string fileName, StopsByNameWithMetadata stopsByName)
        {
            var xmlWriter = XmlWriter.Create(fileName, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });

            xmlWriter.WriteStartDocument();
            var xmlSerializer = new XmlSerializer(typeof(StopsByNameWithMetadata));
            xmlSerializer.Serialize(xmlWriter, stopsByName);

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        private static void SaveJson(string fileName, object obj, bool indented)
        {
            var writer = new StreamWriter(fileName);
            var settings = new JsonSerializerSettings();
            string json = JsonConvert.SerializeObject(obj, indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None, settings);
            writer.WriteLine(json);
            writer.Close();
        }

        // doplní položky pro skupiny zastávek (podle názvu) a zkontroluje, že platí podmínky
        private static void FillAndCheckStopNames(IEnumerable<StopCollectionForName> stopsByName, FullNamesProvider fullNamesProvider)
        {
            foreach (var group in stopsByName)
            {
                group.NodeNumber = AssertAllEqualsAndGet(group, s => s.NodeId, "Ve skupině se liší ID uzlu");
                group.CisNumber = AssertAllEqualsAndGet(group, s => s.CisNumber, "Ve skupině se liší CIS číslo");
                group.IdosCategory = AssertAllEqualsAndGet(group, s => s.IdosCategoryNumber, "Ve skupině se liší číslo kategorie IDOS");
                group.FullName = fullNamesProvider.Resolve(group.Name2);
                var indexOfDot = group.FullName.IndexOf('.');
                if (indexOfDot >= 0 && (indexOfDot >= group.FullName.Length - 1 || !char.IsDigit(group.FullName[indexOfDot + 1])))
                {
                    stopLog.Log(LogMessageType.INFO_STOP_FULL_NAME_CONTAINS_ABBREVS, $"Plný název {group.FullName} stále obsahuje zkratku? (tečka v názvu, za kterou nenásleduje číslice).");
                }

                // TODO výhledově UniqueName zrušit?
                if (conflictedTrainStopNames.Contains(group.Name2) && group.IsTrain)
                    group.UniqueName = $"{group.IdosName} (vlak)";
                else
                    group.UniqueName = group.IdosName;

                if (group.IsTrain && group.IdosCategory != 600003)
                {
                    stopLog.Log(LogMessageType.WARNING_STOP_GROUP_CONFLICTED_TRAIN_FLAG, $"Skupina zastávek {group.UniqueIdentification} je vlaková, ale kategorie IDOS je {group.IdosCategory}");
                }
                else if (!group.IsTrain && group.IdosCategory != 301003)
                {
                    stopLog.Log(LogMessageType.WARNING_STOP_GROUP_TRAIN_FLAG_BAD_IDOS_CATEGORY, $"Skupina zastávek {group.UniqueIdentification} je nevlaková, ale kategorie IDOS je {group.IdosCategory}");
                }

                if (group.DistrictCode == "")
                {
                    stopLog.Log(LogMessageType.WARNING_STOP_GROUP_MISSING_DISTRICT_CODE, $"Skupina zastávek {group.UniqueIdentification} nemá zadaný kód okresu.");
                }

                group.Municipality = AssertAllEqualsAndGet(group, s => s.Municipality, "Ve skupině se liší název obce");

                var commonIdosNameLength = group.IdosName.Length;
                AssertAllEqualsAndGet(group, s => new string(s.IdosName.Take(commonIdosNameLength).ToArray()), "Ve skupině se liší společná část IDOS názvu!");

                if (group.Stops.All(s => s.IdosName != group.IdosName))
                {
                    stopGroupLog.Log(LogMessageType.INFO_STOP_GROUP_NOT_REPRESENTED, $"Skupina zastávek {group.UniqueIdentification} má společný IDOS název {group.IdosName}, ale žádná ze zastávek tento název nenese. Přesvědčte se, že CRWS tento název bude znát.");
                }
            }
        }

        private static void CheckIdosNamesAndCisAreUnique(IEnumerable<StopCollectionForName> stopsByName)
        {
            var stopGroupsByCis = new Dictionary<int, StopCollectionForName>();
            var trainStopGroupsByIdosName = new Dictionary<string, StopCollectionForName>();
            var busStopGroupsByIdosName = new Dictionary<string, StopCollectionForName>();
            var stopByUniqueName = new Dictionary<string, StopCollectionForName>();

            foreach (var stopGroup in stopsByName)
            {
                if (!CheckUniqueAndAdd(stopGroupsByCis, stopGroup.CisNumber, stopGroup))
                {
                    stopLog.Log(LogMessageType.WARNING_STOP_GROUPS_CONFLICTED_CIS_ID, $"CIS ID {stopGroup.CisNumber} - konfliktní skupiny zastávek: {stopGroupsByCis[stopGroup.CisNumber].UniqueIdentification} vs. {stopGroup.UniqueIdentification}");
                }

                if (!stopGroup.IsTrain)
                {
                    if (!CheckUniqueAndAdd(busStopGroupsByIdosName, stopGroup.IdosName, stopGroup))
                    {
                        stopLog.Log(LogMessageType.WARNING_STOP_GROUPS_CONFLICTED_NAME, $"Název {stopGroup.IdosName} (bus) - konfliktní skupiny zastávek: {busStopGroupsByIdosName[stopGroup.IdosName].UniqueIdentification} vs. {stopGroup.UniqueIdentification}");
                    }
                }
                else
                {
                    if (!CheckUniqueAndAdd(trainStopGroupsByIdosName, stopGroup.IdosName, stopGroup))
                    {
                        stopLog.Log(LogMessageType.WARNING_STOP_GROUPS_CONFLICTED_NAME, $"Název {stopGroup.IdosName} (vlak) - konfliktní skupiny zastávek: {trainStopGroupsByIdosName[stopGroup.IdosName].UniqueIdentification} vs. {stopGroup.UniqueIdentification}");
                    }                    
                }

                if (!CheckUniqueAndAdd(stopByUniqueName, stopGroup.UniqueName, stopGroup))
                {
                    stopLog.Log(LogMessageType.WARNING_STOP_GROUPS_CONFLICTED_UNIQUE_NAME, $"Unikátní název {stopGroup.UniqueName} - konfliktní skupiny zastávek: {stopByUniqueName[stopGroup.UniqueName].UniqueIdentification} vs. {stopGroup.UniqueIdentification}");
                }
            }
        }

        // vrátí true, pokud je vše OK, false, pokud již záznam se stejným klíčem v kolekci je
        private static bool CheckUniqueAndAdd<TKey>(Dictionary<TKey, StopCollectionForName> collection, TKey key, StopCollectionForName stopGroup)
        {
            if (collection.ContainsKey(key))
            {
                return false;
            }
            else
            {
                collection.Add(key, stopGroup);
                return true;
            }
        }

        private class ValueAndCount<T>
        {
            public T Value { get; set; }
            public int Count { get; set; }

            public ValueAndCount(T val)
            {
                Value = val;
            }
        }

        private static T AssertAllEqualsAndGet<T>(StopCollectionForName group, Func<Stop, T> selector, string message)
        {
            var values = group.Stops.Select(selector);
            var allEquals = AllEquals(values);
            stopGroupLog.Assert(allEquals, LogMessageType.WARNING_STOP_GROUPS_INCONSISTENT, message + ": " + group.UniqueIdentification);

            if (allEquals)
            {
                // všechny jsou stejné, vrátím první
                return values.FirstOrDefault();
            }
            else
            {
                // jsou různé, vyhraje přesila
                var uniqueValues = values.Distinct().Select(val => new ValueAndCount<T>(val)).ToArray();
                foreach (var value in values)
                {
                    for (int i = 0; i < uniqueValues.Length; i++)
                    {
                        if (value.Equals(uniqueValues[i].Value))
                        {
                            uniqueValues[i].Count++;
                        }
                    }
                }

                return uniqueValues.OrderByDescending(val => val.Count).First().Value;
            }
        }

        private static bool AllEquals<T>(IEnumerable<T> items)
        {
            if (!items.Any())
                return true;

            var firstValue = items.First();
            return items.All(value => value != null ? value.Equals(firstValue) : false);
        }

    }
}
