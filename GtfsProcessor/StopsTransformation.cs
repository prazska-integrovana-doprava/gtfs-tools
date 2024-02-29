using AswModel.Extended;
using CommonLibrary;
using GtfsLogging;
using GtfsProcessor.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Generuje GTFS data zastávek z ASW dat (zastávky, stanice).
    /// 
    /// Zastávky se použijí pouze ty, přes které jezdí nějaké spoje. Zároveň jedna zastávka v ASW může mít odraz ve více záznamech v GTFS,
    /// pokud se projíždějící linky liší pásmy (zastávka v Praze pro linku MHD v pásmu P a pro příměsto v pásmo 0/B). Mapování zastávky z ASW
    /// na zastávku/zastávky v GTFS popisuje <see cref="StopVariantsMapping"/>. Více záznamů může také vzniknout, pokud se zastávce v průběhu
    /// feedu mění některá klíčová vlastnost (název, pásmo, ...) - v ASW je to uloženo jako dvě verze, do GTFS to půjde jako různé zastávky.
    /// 
    /// Pak se generují stanice, ty vyrábíme pro stanice metra (sjednotíme všechny sloupky, které jsou nástupištěm metra pod jednu stanici).
    /// 
    /// Vstupy do metra a další věci se zde neřeší.
    /// </summary>
    class StopsTransformation
    {
        // Data o stanici + odkazy na ASW data o jejích dětech
        private struct StationAndStops
        {
            public GtfsModel.Extended.Station Station;
            public IList<Stop> StopsInStationRaw;
        }

        private TheAswDatabase db;
        private ICommonLogger log;

        public StopsTransformation(TheAswDatabase db, ICommonLogger log)
        {
            this.db = db;
            this.log = log;
        }

        /// <summary>
        /// Generuje všechna GTFS data zastávek (zastávky z ASW a vygenerované stanice k metru). V parametru také vrací mapování ASW zastávek
        /// na tato GTFS data.
        /// </summary>
        /// <param name="stopsMapping">Vrací mapování zastávek z ASW na GTFS.</param>
        /// <returns>Zastávky z ASW + vygenerované stanice</returns>
        public IEnumerable<GtfsModel.Extended.BaseStop> CollectAllStopsAndStations(out Dictionary<Stop, StopVariantsMapping> stopsMapping)
        {
            // Ke každé zastávce si uložíme typy linek, které ji využívají (městské / příměstské / vlakové).
            // Přítomnost zastávky v této dictionary značí, že je využitá.
            // Pokud je zastávky v ASW datech vícekrát (mění se v průběhu nějaký její parametr), operujeme s těmi to záznamy samostatně
            stopsMapping = new Dictionary<Stop, StopVariantsMapping>();
            foreach (var trip in db.GetAllPublicTrips())
            {
                foreach (var stopTime in trip.PublicStopTimes)
                {
                    if (trip.LineType != AswLineType.UndefinedTransport)
                    {
                        stopsMapping.GetValueAndAddIfMissing(stopTime.Stop, new StopVariantsMapping()).UsedVariants.Add(trip.LineType);
                    }
                    else
                    {
                        log.Log(LogMessageType.ERROR_ROUTE_USED_BUT_UNDEF_TRANSPORT_TYPE, $"Spoj {trip.TripId} linky {trip.Route.LineName} je veřejný, ale linka nemá zadaný typ, co je zač.", trip);
                    }
                }
            }

            // řadíme primárně podle čísla uzlu, sekundárně podle čísla sloupku a terciárně podle počátku platnosti
            // - řazení podle ID zastávky je kvůli hezkému pořadí ve výstupním souboru
            // - řazení podle počátku platnosti je kvůli hezkému pořadí i kvůli přidělování GTFS ID viz níže
            var usedStopsAllVersionsOrdered = stopsMapping.Keys.OrderBy(s => s.ServiceAsBits.GetFirstDayOfService()).OrderBy(s => s.NodeId * 10000 + s.StopId).ToArray();

            // vygenerujeme si stanice (GTFS reprezentace + metadata)
            // TODO dočasně vyřazeno, protože dáváme stanice ručně 
            //  - kdybychom si to rozmysleli, stačí odkomentovat
            //  - kdybychom si byli jisti, že to tak stačí, stačí smazat (vč. metody GenerateStations)
            //  - ale do budoucna bychom spíš rádi řešili dohromady s vlaky (jak?)
            var stationsData = Enumerable.Empty<StationAndStops>(); // GenerateStations(usedStopsAllVersionsOrdered).ToList();

            // vytvoříme si mapu ze zastávky na owner stanici, doplníme stanicím polohu a provedeme kontrolu konzistence údajů o stanici vs. zastávce
            var stopStations = new Dictionary<Stop, GtfsModel.Extended.Station>();
            //foreach (var stationData in stationsData)
            //{
            //    // pozice stanice je průměrem pozic všech zastávek
            //    stationData.Station.Position = new GtfsModel.Extended.GpsCoordinates(stationData.StopsInStationRaw.Select(s => s.Position.GpsLatitude).Average(), stationData.StopsInStationRaw.Select(s => s.Position.GpsLongitude).Average());

            //    // připravíme si odkaz z každé zastávky ve stanici na jejich rodiče
            //    foreach (var stop in stationData.StopsInStationRaw)
            //    {
            //        stopStations.Add(stop, stationData.Station);
            //    }

            //    // kontrola konzistence
            //    foreach (var stop in stationData.Station.StopsInStation)
            //    {
            //        log.Assert(stationData.Station.Name == stop.Name, LogMessageType.WARNING_STOP_NAME_NEQ_STATION, $"Název zastávky {stop} se neshoduje s názvem stanice {stationData}.");
            //        log.Assert(stationData.Station.WheelchairBoarding == stop.WheelchairBoarding, LogMessageType.INFO_STOP_WHEELCHAIR_NEQ_STATION, $"Bezbariérovost zastávky {stop} se neshoduje s bezbariérovostí stanice {stationData}.");
            //        log.Assert(stationData.Station.ZoneId == stop.ZoneId, LogMessageType.INFO_STOP_ZONE_ID_NEQ_STATION, $"Pásmové zařazení zastávky {stop} se neshoduje s pásmovým zařazením stanice {stationData}.");
            //    }
            //}

            // vygenerujeme záznamy o zastávkách (a stanicím zastávky přidáme)
            var stops = new List<GtfsModel.Extended.Stop>();
            var usedGtfsIds = new HashSet<string>(); // pro předcházení kolizím v ID zastávek (viz dokumentace metody GenerateGtfsStopVariant)
            foreach (var stop in usedStopsAllVersionsOrdered) // pole je setříděné i podle platnosti záznamu, takže máme zaručeno, že GTFS ID první zastávky kolizní nebude
            {
                var ownerStation = stopStations.GetValueOrDefault(stop);
                var gtfsStops = GenerateGtfsStops(stopsMapping[stop], stop, ownerStation, usedGtfsIds);
                if (ownerStation != null)
                {
                    ownerStation.StopsInStation.AddRange(gtfsStops);
                }

                stops.AddRange(gtfsStops);
                foreach (var gtfsId in gtfsStops.Select(s => s.GtfsId))
                {
                    if (!usedGtfsIds.Add(gtfsId))
                        throw new InvalidOperationException("Duplicate GTFS ID for stop");
                }
            }
            
            // až zde jsou všechna data komplet, můžeme vrátit
            return stationsData.Select<StationAndStops, GtfsModel.Extended.BaseStop>(sd => sd.Station).Concat(stops);
        }

        /// <summary>
        /// Vygeneruje na základě zastávkových sloupků data o stanicích.
        /// 
        /// Stanice tvoříme pro každou stanici metra podle názvu, přestupní stanice mají společný záznam, protože se jmenují stejně
        /// </summary>
        /// <param name="usedStopsAllVersions">Všechny využité záznamy o zastávkách</param>
        private IEnumerable<StationAndStops> GenerateStations(IEnumerable<Stop> usedStopsAllVersions)
        {
            // stanice tvoříme podle názvů, nodeStations je tedy indexováno názvem stanice metra
            var nodeStations = new Dictionary<string, StationAndStops>();

            foreach (var stop in usedStopsAllVersions.Where(s => s.IsMetro))
            {
                StationAndStops stationAndStops;
                if (!nodeStations.TryGetValue(stop.CommonName, out stationAndStops))
                {
                    // zakládáme stanici pro tento název
                    stationAndStops = new StationAndStops()
                    {
                        Station = new GtfsModel.Extended.Station()
                        {
                            Name = stop.CommonName, // používáme CommonName, protože to neobsahuje linku v názvu přestupních stanic
                            GtfsId = $"U{stop.NodeId}S1",
                            WheelchairBoarding = FromAswWheelchairAccessibility(stop.WheelchairAccessibility),
                            ZoneId = stop.PidZoneId,
                            AswNodeId = stop.NodeId,
                            // Position a StopsInStation ještě doplníme
                        },
                        StopsInStationRaw = new List<Stop>(),
                    };

                    nodeStations.Add(stop.CommonName, stationAndStops);
                }

                stationAndStops.StopsInStationRaw.Add(stop);
                log.Assert(stationAndStops.Station.Name == stop.CommonName, LogMessageType.WARNING_STOP_NAME_NEQ_STATION, $"Název zastávky {stop} se neshoduje s názvem stanice {stationAndStops.Station}.");
                log.Assert(stationAndStops.Station.WheelchairBoarding == FromAswWheelchairAccessibility(stop.WheelchairAccessibility), LogMessageType.INFO_STOP_WHEELCHAIR_NEQ_STATION, $"Bezbariérovost zastávky {stop} se neshoduje s bezbariérovostí stanice {stationAndStops.Station}.");
                log.Assert(stationAndStops.Station.ZoneId == stop.PidZoneId, LogMessageType.INFO_STOP_ZONE_ID_NEQ_STATION, $"Pásmové zařazení zastávky {stop} se neshoduje s pásmovým zařazením stanice {stationAndStops.Station}.");
            }

            foreach (var station in nodeStations.Values)
                yield return station;

        }

        /// <summary>
        /// Generuje GTFS záznamy pro zastávku z ASW. Může vrátit až dva záznamy, pokud zastávkou projíždí zároveň městské a příměstské linky
        /// (jeden záznam pro pásmo P a druhý pro neP).
        /// 
        /// Zároveň vyplní varianty do <paramref name="stopsMapping"/>.
        /// </summary>
        /// <param name="aswStop">Záznam z ASW</param>
        /// <param name="stopsMapping">Typy linek, které zastávkou projíždějí (městské, příměstské, ...) - na výstupu jim budou přiřazeny GTFS zastávky</param>
        /// <param name="ownerStation">Stanice, pod kterou zastávky patří</param>
        /// <param name="usedGtfsIds">GTFS ID, která už byla použita, kvůli předcházení kolizím (viz dokumentace <see cref="GenerateGtfsStopVariant(Stop, AswLineType, GtfsModel.Extended.Station, bool)"/>)</param>
        private IEnumerable<GtfsModel.Extended.Stop> GenerateGtfsStops(StopVariantsMapping stopsMapping, Stop aswStop, GtfsModel.Extended.Station ownerStation, HashSet<string> usedGtfsIds)
        {
            // vygenerujeme záznamy pro všechny varianty zastávek, ale vrátíme pak jen ty použité

            // záznam pro pásmo P
            if (stopsMapping.UsedVariants.Contains(AswLineType.PraguePublicTransport))
            {
                stopsMapping.PraguePublicTransportStop = GenerateGtfsStopVariant(aswStop, AswLineType.PraguePublicTransport, ownerStation, usedGtfsIds);
            }

            // záznam bez pásma P
            if (stopsMapping.UsedVariants.Contains(AswLineType.RegionalTransport) || stopsMapping.UsedVariants.Contains(AswLineType.SuburbanTransport))
            {
                stopsMapping.SuburbanTransportStop = GenerateGtfsStopVariant(aswStop, AswLineType.SuburbanTransport, ownerStation, usedGtfsIds);
            }

            // záznam se všemi pásmy (pro divné linky a také pro náhradní dopravu za vlaky)
            // používáme pokud možno regionální variantu, pražskou jen pokud má zastávka výhradně pásmo P (takto by to mělo být pro vlaky ideál a u ostatních linek je to buřt)
            if (stopsMapping.UsedVariants.Contains(AswLineType.RailTransport) || stopsMapping.UsedVariants.Contains(AswLineType.SpecialTransport))
            {
                if (aswStop.PidZoneId == "P")
                {
                    stopsMapping.UniversalStop = stopsMapping.PraguePublicTransportStop ?? GenerateGtfsStopVariant(aswStop, AswLineType.PraguePublicTransport, ownerStation, usedGtfsIds);
                }
                else
                {
                    stopsMapping.UniversalStop = stopsMapping.SuburbanTransportStop ?? GenerateGtfsStopVariant(aswStop, AswLineType.SuburbanTransport, ownerStation, usedGtfsIds);
                }
            }

            return stopsMapping.GetUsedStopVariants();
        }

        /// <summary>
        /// Generuje jeden GTFS záznam zastávky. Podle <paramref name="usageType"/> má buď jen pásmo P, nebo naopak všechna pásma kromě P, nebo všechna pásma jak jsou.
        /// Loguje, takže je dobré nevytvářet záznamy zastávek, které nebudou potřeba (kvůli false alarmům), anebo ten mechanismus nějak upravit
        /// </summary>
        /// <param name="aswStop">Záznam z ASW</param>
        /// <param name="usageType">Typ linky, pro kterou zastávku generujeme (městská / příměstská)</param>
        /// <param name="parentStation">Stanice, pod kterou zastávka patří</param>
        /// <param name="usedGtfsIds">GTFS ID, která už byla použita, kvůli předcházení kolizím. Prakticky může ke kolizi dojít, pokud je zastávka v ASW ve více verzích (tj.
        /// v průběhu feedu se mění například její název, nebo jiný klíčový parametr). Pak bude zastávka roztržena do dvou GTFS záznamů a právě druhá zastávka bude mít kolizní GTFS ID.
        /// V případě kolize první zpracovávaná zastávka dostane klasické GTFS ID ve tvaru UxZy a následné kolizní už budou mít v ID za podtržítkem datum počátku platnosti záznamu.</param>
        private GtfsModel.Extended.Stop GenerateGtfsStopVariant(Stop aswStop, AswLineType usageType, GtfsModel.Extended.Station parentStation, HashSet<string> usedGtfsIds)
        {
            var gtfsId = $"U{aswStop.NodeId}Z{aswStop.StopId}";
            var zoneId = aswStop.PidZoneId;
            if (usageType == AswLineType.PraguePublicTransport)
            {
                gtfsId = $"{gtfsId}P";

                if (!zoneId.Contains("P"))
                {
                    log.Log(LogMessageType.WARNING_STOPTIME_MISSING_STOP_VARIANT, $"Zastávka {aswStop} je využita městskou linkou, avšak nemá pásmo P, doplňuji pásmo P.");
                }

                zoneId = "P";
            }
            else if (usageType == AswLineType.RegionalTransport || usageType == AswLineType.SuburbanTransport)
            {
                zoneId = zoneId.Replace("P,", "");
                zoneId = zoneId.Replace(",P", "");
                zoneId = zoneId.Replace("P", "");
                if (string.IsNullOrWhiteSpace(zoneId))
                {
                    log.Log(LogMessageType.WARNING_STOPTIME_MISSING_STOP_VARIANT, $"Zastávka {aswStop} je využita příměstskou nebo regionální linkou, avšak nemá žádné jiné pásmo než P, pravděpodobně nebude fungovat výpočet jízdného.");
                }
            }

            if (usedGtfsIds.Contains(gtfsId))
                gtfsId = $"{gtfsId}_{aswStop.ValidityStartDate.AsDateTime(db.GlobalStartDate):yyMMdd}";

            var resultStop = new GtfsModel.Extended.Stop()
            {
                AswNodeId = aswStop.NodeId,
                AswStopId = aswStop.StopId,
                GtfsId = gtfsId,
                Name = aswStop.Name,
                ParentStation = parentStation,
                PlatformCode = aswStop.PlatformCode,
                Position = new GpsCoordinates(aswStop.Position.GpsLatitude, aswStop.Position.GpsLongitude),
                WheelchairBoarding = FromAswWheelchairAccessibility(aswStop.WheelchairAccessibility),
                ZoneId = zoneId,
                ZoneRegionType = aswStop.ZoneRegionType,
            };

            if (aswStop.IsMetro)
            {
                resultStop.Name = aswStop.CommonName; // nechceme, aby přestupní stanice obsahovaly přípis čísla linky, název2 toto splňuje
                // fix nástupiště na číslo koleje
                if (resultStop.PlatformCode == "M1" || resultStop.PlatformCode == "M3")
                    resultStop.PlatformCode = "1";
                if (resultStop.PlatformCode == "M2" || resultStop.PlatformCode == "M4")
                    resultStop.PlatformCode = "2";
            }

            return resultStop;
        }

        // Transformace mezi ASW a GTFS popisem bezbariérovosti
        private GtfsModel.Enumerations.WheelchairBoarding FromAswWheelchairAccessibility(WheelchairAccessibility wheelchairAccessibility)
        {
            return wheelchairAccessibility == WheelchairAccessibility.Accessible ? GtfsModel.Enumerations.WheelchairBoarding.Possible
                : wheelchairAccessibility == WheelchairAccessibility.Undefined ? GtfsModel.Enumerations.WheelchairBoarding.Unknown
                : GtfsModel.Enumerations.WheelchairBoarding.NotPossible;
        }
    }
}
