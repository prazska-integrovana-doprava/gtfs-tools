using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using GtfsLogging;
using CommonLibrary;
using GtfsProcessor.DataClasses;
using GtfsProcessor.Logging;

namespace GtfsProcessor
{
    /// <summary>
    /// Určuje k tripům persistentní identifikátory. Ty se skládají ze tří komponent - linky, čísla spoje (generuje se sekvenčně od 1 výš) a data prvního
    /// výskytu. Používá textovou databázi, kam si ukládá přidělení IDček spojům. IDčka se přidělují podle průběhu spoje (projeté zastávky a časy odjezdů).
    /// Platí, že dva shodné tripy mají stejné číslo spoje. Mohou být dva spoje se stejným číslem, pak se liší datem prvního výskytu (např. v případě, že
    /// je pracovnědenní spoj stejný průběhem jako víkendový, ale jeden je nízkopodlažní a druhý ne).
    /// 
    /// Za stejné se považují tripy, které mají shodnou linku, čas výjezdu z první zastávky, čas příjezdu do cílové zastávky a seznam projetých uzlů.
    /// Může se stát, že bude ve feedu více spojů, které budou z tohoto hlediska považovány za shodné, pak se pro odlišení používá datum prvního výskytu
    /// spoje (v historii). Problém nastává pouze ve chvíli, kdy by tyto shodné spoje jely ve stejný den, nicméně to by nemělo nastat, vyjma následujících
    /// případů. (1) posila dvěma vozy (mají různé číslo pořadí - z hlediska cestujícího ale stále vlastně jeden spoj, čili druhý mohu smazat). (2) chyba
    /// v datech (duplicitní spoj), zde je na místě smazání duplicity.
    /// 
    /// K rozpoznávání "stejného spoje jako včera" se využívá také kalendář a skutečnost, že spoje linek ve feedu jsou řazené podle toho, kdy nejdříve jedou.
    /// Pokud se tedy jeden spoj do dalšího dne rozpadne na dva, tak stejné ID si zachová ten z nich, který jede dřív. Může se tedy stát, že spoj změní ID,
    /// ale nemělo by se to stát v den, kdy spoj jede (protože takový spoj se zpracuje jako první a ID si zachová).
    /// </summary>
    class TripIdPersistentDb
    {
        #region Helper classes

        // Popis tripu, pomocí kterého se porovnávají.
        private class TripDescription
        {
            public int LineNumber { get; private set; }

            public Time FirstStopDepartureTime { get; private set; }

            public Time LastStopArrivalTime { get; private set; }

            public int[] PassedNodes { get; private set; }

            public override int GetHashCode()
            {
                int hash = 19;
                foreach (var item in PassedNodes)
                    hash = hash * 31 + item.GetHashCode();

                return (LineNumber.GetHashCode() << 16) ^ FirstStopDepartureTime.GetHashCode() ^ LastStopArrivalTime.GetHashCode() ^ hash;
            }

            protected TripDescription()
            {
            }

            public TripDescription(MergedTripGroup trip)
            {
                LineNumber = trip.Route.LineNumber;
                FirstStopDepartureTime = trip.PublicStopTimes.First().DepartureTime;
                LastStopArrivalTime = trip.PublicStopTimes.Last().ArrivalTime;
                PassedNodes = trip.PublicStopTimes.Select(st => st.Stop.NodeId).ToArray();
            }

            public override bool Equals(object obj)
            {
                var other = obj as TripDescription;
                if (other == null)
                    return false;

                if (LineNumber != other.LineNumber || !FirstStopDepartureTime.Equals(other.FirstStopDepartureTime) || !LastStopArrivalTime.Equals(other.LastStopArrivalTime))
                    return false;

                if (PassedNodes.Length != other.PassedNodes.Length)
                    return false;

                for (int i = 0; i < PassedNodes.Length; i++)
                {
                    if (PassedNodes[i] != other.PassedNodes[i])
                        return false;
                }

                return true;
            }

            public override string ToString()
            {
                var stopList = string.Join("-", PassedNodes);
                return $"{LineNumber},{FirstStopDepartureTime},{LastStopArrivalTime},{stopList}";
            }

            public static TripDescription FromString(string str)
            {
                var columns = str.Split(new[] { ',' });
                var nodes = columns[3].Split(new[] { '-' });
                var result = new TripDescription()
                {
                    LineNumber = int.Parse(columns[0]),
                    FirstStopDepartureTime = Time.Parse(columns[1]),
                    LastStopArrivalTime = Time.Parse(columns[2]),
                    PassedNodes = new int[nodes.Length]
                };

                for (int i = 0; i < nodes.Length; i++)
                    result.PassedNodes[i] = int.Parse(nodes[i]);

                return result;
            }
        }

        // Reprezentuje sadu všech shodných spojů, které jsou odlišené datem prvního výskytu.
        private class TripData
        {
            // ID spoje v rámci linky (v trip ID 'LINKA_ID_DATUM' je to ta prostřední část)
            public int Id { get; private set; }
            
            // Shodné spoje s tímto ID Spoje rozdělené podle prvního dne platnosti (tím jsou unikátní)
            //  - pozn. toto datum tvoří třetí část trip ID
            public VersionedItemByDate<TripDataDateRecord> DateRecords { get; private set; }
            
            public TripData(int id)
            {
                Id = id;
                DateRecords = new VersionedItemByDate<TripDataDateRecord>();
            }

            // přidá nový záznam k zadanému datu prvního výskytu a vrátí ho
            // pokud selže, zaloguje a vrátí null
            public TripDataDateRecord AddRecord(DateTime firstOccurenceDate, MergedTripGroup trip, ICommonLogger log)
            {
                var dateRecord = new TripDataDateRecord(firstOccurenceDate, trip, this);
                if (!DateRecords.AlreadyContainsVersion(dateRecord.FirstOccurenceDate))
                {
                    DateRecords.AddVersion(dateRecord, dateRecord.FirstOccurenceDate);
                    return dateRecord;
                }
                else
                {
                    // tohle by již mělo být ošetřené
                    var otherTrip = DateRecords.GetVersion(dateRecord.FirstOccurenceDate).CurrentTrip;
                    log.Log(LogMessageType.ERROR_TRIP_DUPLICATE_UNIQUE_TRIP_ID, $"Shodné spoje, které se chtějí alokovat na stejné ID+datum {Id}_{firstOccurenceDate:yyMMdd}: {trip} a {otherTrip}. Druhý spoj dostane dočasné ID.");
                    return null;
                }
            }
        }

        // Reprezentuje jeden slot (spoj, datum) načtený z databáze, na který se snažíme namatchovat nějaký trip z feedu. To se nutně nemusí
        // povést; co se ale nesmí stát, že by byly dva tripy přiřazené jednomu tomuto záznamu (resp. že by dva tripy odkazovaly na stejnou
        // dvojici id + first occurence date). Výjimkou je situace, kdy jsou tyto dva spoje na různých obězích, pak mohou zde být spolu,
        // vždy je ale jeden "hlavní" (CurrentTrip) a všechny jsou pak sdruženy v AllTripsOnDifferentCircles.
        private class TripDataDateRecord
        {
            // skupina spojů se stejným číslem (prostřední část ID spoje)
            public TripData OwnerTripData { get; private set; }

            // datum prvního výskytu spoje (třetí část ID spoje)
            public DateTime FirstOccurenceDate { get; private set; }

            // dny, kdy spoj jede, nemusí být vyplněno až do konce, pokud nenamapujeme na nějaký spoj z feedu, bude nám každý den z bitmapy jeden bit mizet
            public ServiceDaysBitmap ServiceBitmap { get; private set; }

            // trip přiřazený této dvojici (id, firstOccurenceDate)
            // pokud žádný takový ve feedu není, je null
            public MergedTripGroup CurrentTrip { get; private set; }

            // CurrentTrip + všechny tripy "navíc", které jsme nechtěli zahodit, protože mají jiné pořadí a podmnožinový kalendář,
            // ale jinak jsou stejné; indexováno číslem pořadí
            public Dictionary<int, MergedTripGroup> AllTripsOnDifferentCircles { get; private set; }

            protected TripDataDateRecord(TripData ownerTripData)
            {
                OwnerTripData = ownerTripData;
                AllTripsOnDifferentCircles = new Dictionary<int, MergedTripGroup>();
            }

            // založí záznam a rovnou přiřadí trip
            public TripDataDateRecord(DateTime firstOccurenceDate, MergedTripGroup trip, TripData ownerTripData)
            {
                OwnerTripData = ownerTripData;
                FirstOccurenceDate = firstOccurenceDate;
                CurrentTrip = trip;
                ServiceBitmap = trip.ServiceAsBits;
                AllTripsOnDifferentCircles = new Dictionary<int, MergedTripGroup>() { { trip.ReferenceTrip.CurrentRunNumber, trip } };
            }

            // Pokud je slot volný, nastaví trip a vrátí true. Pokud není volný, vrátí false.
            // Pokud je force = true, pak nastaví vždy
            public bool SetTrip(MergedTripGroup trip)
            {
                if (CurrentTrip == null)
                {
                    // volný slot, umístíme sem
                    ForceSetTrip(trip);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // Nastaví trip. Pokud je slot volný, stejně jako SetTrip. Pokud je slot zabraný, tak v něm
            // tento trip nahradí ten stávající. Zároveň je vždy přidán do skupiny AllTripsOnDifferentCircles.
            // Pokud již ve skupině existoval trip se stejným číslem pořadí, který jsme tímto
            // zahodili úplně, tak ho vrátí (abychom ho zrušili), jinak vrátí null.
            public MergedTripGroup ForceSetTrip(MergedTripGroup trip)
            {
                MergedTripGroup result;
                AllTripsOnDifferentCircles.TryGetValue(trip.ReferenceTrip.CurrentRunNumber, out result);

                AllTripsOnDifferentCircles[trip.ReferenceTrip.CurrentRunNumber] = trip;
                CurrentTrip = trip;
                ServiceBitmap = trip.ServiceAsBits;

                return result;
            }

            // pokud to jde, přidá do AllTripsOnDifferentCircles
            public bool AddExtraTrip(MergedTripGroup trip)
            {
                if (AllTripsOnDifferentCircles.ContainsKey(trip.ReferenceTrip.CurrentRunNumber))
                {
                    // škoda, stejný spoj se stejným pořadím už tu máme, asi bude chyba v datech
                    return false;
                }

                AllTripsOnDifferentCircles.Add(trip.ReferenceTrip.CurrentRunNumber, trip);
                return true;
            }

            public void ShiftBitmapToLeft(int dayShift)
            {
                ServiceBitmap = ServiceBitmap.ShiftLeft(dayShift);
            }

            public override string ToString()
            {
                var tripIds = string.Join("+", AllTripsOnDifferentCircles.Values);
                return $"{FirstOccurenceDate:yyMMdd},{ServiceBitmap.ToString()},{tripIds}";
            }

            public static TripDataDateRecord FromString(string str, TripData ownerTripData)
            {
                var cols = str.Split(new[] { ',' });
                return new TripDataDateRecord(ownerTripData)
                {
                    FirstOccurenceDate = DateTime.ParseExact(cols[0], "yyMMdd", CultureInfo.InvariantCulture),
                    ServiceBitmap = ServiceDaysBitmap.FromBitmapString(cols[1]),
                };
            }
        }

        // Databáze IDček tripů - každý nový trip dostane ID, které se ukládá a při dalším exportu znovu načítá,
        // aby stejný trip dostal stejné ID
        private class TripPersistentDatabase
        {
            // seznam tripů indexovaný 'prostřední částí ID' vedoucí na všechny záznamy podle prvního výskytu
            public Dictionary<TripDescription, TripData> Trips { get; private set; }

            // pro přidělování nových idček
            private Dictionary<int, int> maxIdForLine;

            public TripPersistentDatabase()
            {
                Trips = new Dictionary<TripDescription, TripData>();
                maxIdForLine = new Dictionary<int, int>();
            }

            private int GenerateNewId(int lineNumber)
            {
                int result;
                if (!maxIdForLine.TryGetValue(lineNumber, out result))
                {
                    result = 0;
                    maxIdForLine.Add(lineNumber, 0);
                }

                result++;
                maxIdForLine[lineNumber] = result;
                return result;
            }

            public TripData FindTrip(MergedTripGroup trip)
            {
                var tripDescription = new TripDescription(trip);
                TripData tripData;
                if (!Trips.TryGetValue(tripDescription, out tripData))
                {
                    tripData = new TripData(GenerateNewId(trip.Route.LineNumber));
                    Trips.Add(tripDescription, tripData);
                }

                return tripData;
            }

            // odstraní redundantní záznamy
            public void Filter(DateTime current)
            {
                foreach (var trip in Trips.Values)
                {
                    var versions = trip.DateRecords.AllVersions().ToArray();
                    foreach (var version in versions)
                    {
                        if (version.FirstOccurenceDate > current && version.CurrentTrip == null)
                        {
                            // budoucí a nemá trip => smazat, aby se na to v budoucnu něco nenalepilo
                            trip.DateRecords.RemoveVersion(version.FirstOccurenceDate);
                        }
                        else if (version.ServiceBitmap.IsEmpty)
                        {
                            // nikdy už nepojede
                            trip.DateRecords.RemoveVersion(version.FirstOccurenceDate);
                        }
                    }
                }
            }

            public void SaveTo(StreamWriter writer)
            {
                foreach (var trip in Trips.OrderBy(t => t.Value.Id).OrderBy(t => t.Key.LineNumber))
                {
                    writer.WriteLine($"{trip.Value.Id}|{trip.Key}");
                    foreach (var dateRecord in trip.Value.DateRecords.AllVersions())
                    {
                        writer.WriteLine($"-{dateRecord}");
                    }
                }
            }

            public static TripPersistentDatabase LoadFrom(StreamReader reader, int dayShift)
            {
                var result = new TripPersistentDatabase();

                TripData current = null;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!line.StartsWith("-"))
                    {
                        var lineSplit = line.Split(new[] { '|' });
                        var tripDescription = TripDescription.FromString(lineSplit[1]);
                        current = new TripData(int.Parse(lineSplit[0]));
                        result.Trips.Add(tripDescription, current);

                        // využíváme toho, že je to setříděné
                        result.maxIdForLine[tripDescription.LineNumber] = current.Id;
                    }
                    else
                    {
                        var dateRecord = TripDataDateRecord.FromString(line.Substring(1), current);
                        dateRecord.ShiftBitmapToLeft(dayShift);
                        current.DateRecords.AddVersion(dateRecord, dateRecord.FirstOccurenceDate);
                    }
                }

                return result;
            }
        }

        #endregion

        private ICommonLogger log = Loggers.CommonLoggerInstance;
        private string tripDbFolder;
        private DateTime globalStartDate;

        private TripPersistentDatabase tripDb;
        private IDictionary<MergedTripGroup, TripDataDateRecord> tripsToIdMapping;

        /// <summary>
        /// Pro kontroly IDček spojů kontrolujeme jen spoje v následujících dnech.
        /// Ty jsou totiž zásadní a zároveň už by měly být poměrně stabilní
        /// (ty ve vzdálenější budoucnosti se víc mohou měnit a není to relevantní).
        /// Tato hodnota udává kolik dní dopředu se maximálně díváme; spoje, které
        /// jedou později v budoucnosti, neřešíme a při verifikacích je ignorujeme
        /// </summary>
        public const int DaysAheadToVerify = 5;

        /// <summary>
        /// Počet spojů, kterým bylo přiděleno ID z databáze (logujeme jen spoje v následujících <see cref="DaysAheadToVerify"/> dnech viz <see cref="LogTripWithReusedId(MergedTripGroup)"/>)
        /// </summary>
        public int TripsWithReusedId { get; set; }

        /// <summary>
        /// Počet spojů, kterým bylo vygenerováno nové ID (logujeme jen spoje v následujících <see cref="DaysAheadToVerify"/> dnech viz <see cref="LogTripWithNewId(MergedTripGroup)"/>)
        /// </summary>
        public int TripsWithNewId { get; set; }

        public TripIdPersistentDb(string tripDbFolder)
        {
            this.tripDbFolder = tripDbFolder;
        }

        /// <summary>
        /// Načte data o ID tripů ze souboru a na základě toho přiřadí každému tripu v seznamu <paramref name="trips"/> jeho ID.
        /// </summary>
        /// <param name="globalStartDate">Počátek feedu</param>
        /// <param name="trips">Seznam tripů, kterým chceme přiřadit ID</param>
        public void Init(DateTime globalStartDate, IEnumerable<MergedTripGroup> trips)
        {
            this.globalStartDate = globalStartDate;
            tripDb = LoadTripDatabase(globalStartDate);
            tripsToIdMapping = new Dictionary<MergedTripGroup, TripDataDateRecord>();
            
            foreach (var trip in trips.OrderBy(t => t.ServiceAsBits.GetFirstDayOfService()))
            {
                var tripDataDateRecord = AssignIdAndDateSlotForTrip(trip);
                tripsToIdMapping.Add(trip, tripDataDateRecord);
            }
        }

        /// <summary>
        /// Vrátí ID pro zadaný trip. Musí již být načtena databáze a přiřazena IDčka pomocí <see cref="Init(DateTime, IEnumerable{MergedTripGroup})"/>.
        /// </summary>
        /// <param name="trip">Spoj, kterému chceme přiřadit ID</param>
        /// <returns>ID pro daný spoj (unikátní)</returns>
        public string GetGtfsIdForTrip(MergedTripGroup trip)
        {
            var dateRecord = tripsToIdMapping.GetValueOrDefault(trip);
            if (dateRecord == null)
            {
                LogTripWithNewId(trip);
                return $"{trip.Route.LineNumber}_X{trip.TripIds.First()}";
            }

            bool includeSequenceNumberInGtfsId = dateRecord.CurrentTrip != trip; // pokud není current, pak je mezi ostatními, které jsou shodné a liší se číslem oběhu a musí obsahovat toto číslo oběhu v ID
            if (includeSequenceNumberInGtfsId)
                return $"{trip.Route.LineNumber}_{dateRecord.OwnerTripData.Id}_{dateRecord.FirstOccurenceDate:yyMMdd}_POS{trip.ReferenceTrip.CurrentRunNumber}";
            else
                return $"{trip.Route.LineNumber}_{dateRecord.OwnerTripData.Id}_{dateRecord.FirstOccurenceDate:yyMMdd}";
        }

        /// <summary>
        /// Uloží databázi spojů
        /// </summary>
        public void SaveTripDatabase()
        {
            var fileName = ConstructFileName(globalStartDate);
            using (var stream = new StreamWriter(fileName))
            {
                tripDb.Filter(globalStartDate);
                tripDb.SaveTo(stream);
            }
        }

        private TripDataDateRecord AssignIdAndDateSlotForTrip(MergedTripGroup trip)
        {
            var tripData = tripDb.FindTrip(trip);
            var firstOccurenceDate = trip.ServiceAsBits.GetFirstDayOfService().AsDateTime(globalStartDate);

            foreach (var dateRecord in tripData.DateRecords.AllVersions().Reverse()) // od nejnovějšího, snažíme se přiřadit nejnovějšímu, který začíná nejpozději v první den platnosti spoje
            {
                if (dateRecord.FirstOccurenceDate > firstOccurenceDate)
                    continue; // nesmíme přiřadit budoucímu

                if (dateRecord.ServiceBitmap.Intersect(trip.ServiceAsBits).IsEmpty && dateRecord.FirstOccurenceDate != firstOccurenceDate)
                    continue; // jiný kalendář 
                        // druhá podmínka je pojistka - spoj by si totiž nemohl vytvořit vlastní slot, protože jeho first occurence date už je zabrané

                // ve chvíli kdy najdeme vhodného kandidáta (nejbližší prvním výskytem "zespoda" a zároveň s alespoň jedním bitem ve společném kalendáři,
                // už jde jen o to, jestli spoj do slotu přiřadíme, anebo vytvoříme slot nový

                if (dateRecord.SetTrip(trip))
                {
                    // volný slot, umístěno sem
                    LogTripWithReusedId(trip);
                    return dateRecord;
                }
                else if (trip.ServiceAsBits.IsSubsetOf(dateRecord.CurrentTrip.ServiceAsBits))
                {
                    // slot už je obsazen, ale tento spoj má podmnožinový kalendář, čili je posilový;
                    // dostane proto stejné ID s přívětkem čísla oběhu
                    LogTripWithNewId(trip);
                    if (dateRecord.AddExtraTrip(trip))
                    {                        
                        return dateRecord;
                    }
                    else if(!dateRecord.AllTripsOnDifferentCircles[trip.ReferenceTrip.CurrentRunNumber].ServiceAsBits.Intersect(trip.ServiceAsBits).IsEmpty)
                    {
                        // slot je obsazen spojem se stejným číslem oběhu, který navíc jede některý shodný den => chyba
                        log.Log(LogMessageType.ERROR_TRIP_CONFLICTED_UNIQUE_TRIP_ID, $"Nelze přidat trip {trip} jako posilový k {dateRecord.CurrentTrip} (oba {dateRecord.OwnerTripData.Id}_{dateRecord.FirstOccurenceDate:yyMMdd}). Pravděpodobně shodné tripy (včetně čísla oběhu). Spoj dostane dočasné ID.");
                        return null;
                    }
                    else
                    {
                        // slot je obsazen spojem se stejným číslem oběhu, ale disjunktním kalendářem, takže to může být přidán ještě extra jako nový datumový záznam
                        return tripData.AddRecord(firstOccurenceDate, trip, log);
                    }
                }
                else if (dateRecord.AllTripsOnDifferentCircles.Values.All(
                    otherTrip => otherTrip.ServiceAsBits.IsSubsetOf(trip.ServiceAsBits)))
                {
                    // slož už je obsazen, ale tento spoj má nadmnožinový kalendář (ke všem spojům,
                    // které patří k tomuto IDčku), čili tyto spoje jsou posilové; stane se novým
                    // current tripem a ostatní tripy budou anotovány číslem oběhu
                    LogTripWithNewId(trip);
                    var originalTrip = dateRecord.ForceSetTrip(trip);
                    if (originalTrip != null)
                    {
                        log.Log(LogMessageType.ERROR_TRIP_CONFLICTED_UNIQUE_TRIP_ID, $"Při přidávání tripu {trip} k {dateRecord.OwnerTripData.Id}_{dateRecord.FirstOccurenceDate:yyMMdd} byl přemazán trip {originalTrip}, který je mu posilový. Pravděpodobně shodné tripy (včetně čísla oběhu). Původní trip dostane dočasné ID.");
                        return null;
                    }

                    return dateRecord;
                }
                else
                {
                    // nemá ani podmnožinový ani nadmnožinový kalendář k existujícím spojům, přidáme nový datumový záznam
                    LogTripWithNewId(trip);
                    return tripData.AddRecord(firstOccurenceDate, trip, log);
                }
            }

            // nenašla se žádná vhodná verze, tak ji založíme
            LogTripWithNewId(trip);
            return tripData.AddRecord(firstOccurenceDate, trip, log);
        }

        private TripPersistentDatabase LoadTripDatabase(DateTime current)
        {
            for (int i = 0; i < 14; i++)
            {
                var fileName = ConstructFileName(current.AddDays(-i));
                if (File.Exists(fileName))
                {
                    using (var stream = new StreamReader(fileName))
                    {
                        return TripPersistentDatabase.LoadFrom(stream, i);
                    }
                }
            }

            return new TripPersistentDatabase();
        }

        private string ConstructFileName(DateTime date)
        {
            return Path.Combine(tripDbFolder, $"trips_db_{date:yyMMdd}.txt");
        }

        private void LogTripWithNewId(MergedTripGroup trip)
        {
            if (trip.ServiceAsBits.GetFirstDayOfService() < DaysAheadToVerify)
            {
                TripsWithNewId++;
            }
        }

        private void LogTripWithReusedId(MergedTripGroup trip)
        {
            if (trip.ServiceAsBits.GetFirstDayOfService() < DaysAheadToVerify)
            {
                TripsWithReusedId++;
            }
        }
    }
}
