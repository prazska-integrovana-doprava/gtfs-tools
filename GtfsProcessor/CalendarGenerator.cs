using CommonLibrary;
using GtfsLogging;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using GtfsModel.Functions;
using GtfsProcessor.DataClasses;
using GtfsProcessor.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Generuje tripům (<see cref="MergedTripGroup"/>) kalendáře podle service bitmap.
    /// 
    /// Kalendáře se tvoří na základě platnosti a dnech grafikonu a kalendáře spoje. Funguje tak, že spoji vyrobí kalendář a pokud vyšel
    /// identicky jako již některý dřívější kalendář, použije se ten. Novým kalendářům také přiděluje ID (odvozené od provozních dní)
    /// </summary>
    ///
    /// Detailní postup tvorby kalendáře je uveden v metodě <see cref="PrepareCalendar(MergedTripGroup)"/>.
    class CalendarGenerator
    {
        // porovnává GTFS kalendáře podle obsahové identity
        private class CalendarComparer : IEqualityComparer<CalendarRecord>
        {
            public int GetHashCode(CalendarRecord record)
            {
                var result = record.ServiceAsFlags.GetHashCode() ^ record.StartDate.GetHashCode() ^ record.EndDate.GetHashCode();
                foreach (var exception in record.Exceptions)
                {
                    result ^= exception.GetHashCode();
                }

                return result;
            }

            public bool Equals(CalendarRecord first, CalendarRecord second)
            {
                if (first == null && second == null)
                    return true;
                else if (first == null || second == null)
                    return false;

                if (first.StartDate != second.StartDate || first.EndDate != second.EndDate)
                    return false;

                for (int i = 0; i < first.InService.Length; i++)
                {
                    if (first.InService[i] != second.InService[i])
                        return false;
                }

                return Enumerable.SequenceEqual(first.Exceptions, second.Exceptions);
            }
        }



        /// <summary>
        /// Datum počátku feedu
        /// </summary>
        public DateTime GlobalStartDate { get; set; }

        // pro přidělování identifikátorů kalendářům
        private CalendarIdManager idManager;

        private ICommonLogger log = Loggers.CommonLoggerInstance;
        private ISimpleLogger calendarLog = Loggers.CalendarLoggerInstance;

        // indexuju sám sebe, abych se mohl vyzvednout
        private Dictionary<CalendarRecord, CalendarRecord> records;

        // uložení přiřazení kalendářů tripům (je potřeba při konstrukci kalendářů oběhů)
        private Dictionary<MergedTripGroup, CalendarRecord> calendarToTripAssignment;

        public CalendarGenerator(DateTime globalStartDate)
        {
            GlobalStartDate = globalStartDate;
            idManager = new CalendarIdManager();
            records = new Dictionary<CalendarRecord, CalendarRecord>(new CalendarComparer());
        }

        /// <summary>
        /// Vygeneruje všechny kalendáře spojům a zároveň vytvoří přiřazení kalendářů jednotlivým tripům (resp. <see cref="MergedTripGroup"/>).
        /// </summary>
        /// <param name="tripGroups">Spoje</param>
        /// <param name="calendarToTripAssignment">Výsledné přiřazení</param>
        /// <returns>Vytvořené kalendáře</returns>
        public IEnumerable<CalendarRecord> GenerateCalendarsForTrips(IEnumerable<MergedTripGroup> tripGroups, 
            out Dictionary<MergedTripGroup, CalendarRecord> calendarToTripAssignment)
        {
            calendarToTripAssignment = new Dictionary<MergedTripGroup, CalendarRecord>();

            foreach (var trip in tripGroups)
            {
                if (trip.ServiceAsBits.IsEmpty)
                {
                    log.Log(LogMessageType.ERROR_TRIP_CALENDAR_NULL, $"{trip} má nulový kalendář. Měl být ignorován při načítání.");
                }
                
                calendarLog.Log($"Vytvářím kalendář pro spoj {trip.ToStringWithCalendar()}");

                var calendar = CreateCalendar(trip.ServiceAsBits, trip.AllTrips);
                calendarToTripAssignment.Add(trip, calendar);

                LogAndCheckCalendar(calendar, trip.ServiceAsBits, trip);
            }

            this.calendarToTripAssignment = calendarToTripAssignment;
            return calendarToTripAssignment.Values.Distinct();
        }

        /// <summary>
        /// Vygeneruje kalendáře oběhům a zároveň vytvoří přiřazení kalendářů jednotlivým oběhům. Při tom využívá kalendáře již vytvořené
        /// při volání <see cref="CalendarGenerator.CreateCalendarsForTrips"/>.
        /// 
        /// Podmínkou volání této metody je, že již proběhla tvorba kalendářů spojů (je tedy nejdříve nutné zavolat <see cref="CalendarGenerator.CreateCalendarsForTrips"/>).
        /// </summary>
        /// <param name="runs">Oběhy</param>
        /// <param name="calendarToRunAssignment">Výsledné přiřazení</param>
        /// <returns>Vytvořené kalendáře (jen ty, které už nebyly</returns>
        public IEnumerable<CalendarRecord> GenerateCalendarsForRuns(IEnumerable<MergedRun> runs, out Dictionary<MergedRun, CalendarRecord> calendarToRunAssignment)
        {
            calendarToRunAssignment = new Dictionary<MergedRun, CalendarRecord>();

            foreach (var run in runs)
            {
                // zkusíme najít spoj se stejným kalendářem a použít ho
                foreach (var tripAndNumber in run.TripsAndNumbers)
                {
                    if (run.ServiceBitmap.Equals(tripAndNumber.TripData.ServiceAsBits))
                    {
                        calendarLog.Log($"Pro oběh {run} v {run.ServiceBitmap} používám kalendář spoje {tripAndNumber.TripData}.");
                        calendarToRunAssignment.Add(run, calendarToTripAssignment[tripAndNumber.TripData]);
                        break;
                    }
                }

                // pokud jsme takový spoj nenašli, odvodíme si kalendář sami
                if (!calendarToRunAssignment.ContainsKey(run))
                {
                    calendarLog.Log($"Vytvářím kalendář pro oběh {run} v {run.ServiceBitmap}");

                    // vybíráme spoje s podmnožinovým kalendářem, ale stejně to moc dobře nefunguje, protože může být oběh složený z více grafikonů a všechny spoje mají stejný kalendář,
                    // ale jejich grafikony ne.. a tak může být oběh s jednodenním kalendářem, který začíná spoji z celodobého grafikonu
                    // - no a protože dál se pak bere union grafikonů, tak se použije to.. muselo by se to nějak asi rozdělit, ta filosofie tvorby kalendářů spojů z grafikonů
                    // na ty oběhy tolik nesedí
                    //
                    // ... nicméně to moc neva, protože drtivou většinu oběhů obsloužíme výše tím, že jim flákneme kalendář z některého z jeho spojů
                    var tripsWithRepresentativeBitmap = run.TripsAndNumbers.SelectMany(gr => gr.TripData.AllTrips.Where(t => t.ServiceAsBits.IsSubsetOf(run.ServiceBitmap)));
                    if (!tripsWithRepresentativeBitmap.Any())
                    {
                        // pokud není žádný spoj s podmnožinovým kalendářem, musíme holt použít všechny
                        tripsWithRepresentativeBitmap = run.TripsAndNumbers.SelectMany(gr => gr.TripData.AllTrips);
                    }

                    var calendar = CreateCalendar(run.ServiceBitmap, tripsWithRepresentativeBitmap);
                    calendarToRunAssignment.Add(run, calendar);

                    LogAndCheckCalendar(calendar, run.ServiceBitmap, run);
                }
            }

            var onlyNewCalendars = calendarToRunAssignment.Values.Distinct().Except(calendarToTripAssignment.Values);
            return onlyNewCalendars;
        }

        /// <summary>
        /// Vrátí kalendář pro spoj nebo oběh. Buď použije již existující, pokud by byl shodný, nebo vytvoří nový.
        /// Rovnou ho přidá do <see cref="records"/>.
        /// </summary>
        /// <param name="serviceBitmap">Bitmapový kalendář spoje/oběhu</param>
        /// <param name="allTrips">Všechny spoje, ze kterých je spoj zmergován, nebo které tvoří oběh. Použijí se pro určení grafikonů, které se použijí pro určení provozních dnů.</param>
        /// (v případě oběhů to ale funguje pofidérně, viz výše)
        private CalendarRecord CreateCalendar(ServiceDaysBitmap serviceBitmap, IEnumerable<AswModel.Extended.Trip> allTrips)
        {
            var calendarRecord = PrepareCalendar(serviceBitmap, allTrips);
            if (records.ContainsKey(calendarRecord))
            {
                return records[calendarRecord];
            }

            calendarRecord.GtfsId = idManager.CreateCalendarId(calendarRecord)
                // TODO zrušit, až odladíme
                .Replace('_', '-');

            records.Add(calendarRecord, calendarRecord);

            return calendarRecord;
        }

        // Vyrobí na základě service bitmapy spoje (spojů) a grafikonu (grafikonů)
        //
        // Ze zmergovaného spoje se vezmou všechny grafikony, ze kterých byl spoj slepen. Použijeme ty grafikony, které platí až do konce feedu
        // a z určíme provozní dny jako sjednocení provozních dnů těchto grafikonů. To kvůli tomu, aby když někdo protáhne konec platnosti feedu,
        // měl co nejlepší predikci a neovlivňovaly mu to dočasné spoje, o kterých víme, že končí dřív, než vůbec končí náš feed.
        // Příklad: spoj jede v pracovní dny a k tomu právě jednu sobotu. Aby ta sobota nemátla, kdyby někdo predikoval kalendář do budoucna,
        // tak ji do dnů v týdnu nedáme a použijeme pouze +výjimku.
        //
        // Speciální případ, pokud všechny grafikony platí až do konce feedu - prosté sjednocení jejich provozních dnů
        // Speciální případ, pokud žádný grafikon neplatí až do konce feedu - také prosté sjednocení jejich provozních dnů.
        //
        // Nakonec se ke zvoleným dnům v týdnu dotvoří výjimky tak, aby kalendář odpovídal původní bitmapě
        private CalendarRecord PrepareCalendar(ServiceDaysBitmap serviceBitmap, IEnumerable<AswModel.Extended.Trip> allTrips)
        {
            // spoj může být sloučený z více grafikonů, zjistíme si, od kdy do kdy platí (sjednocení). Zatím neřešíme provozní dny, jen od-do.
            var graphs = allTrips.Select(sr => sr.Graph).Distinct().ToArray();
            var graphServiceBits = allTrips.Select(sr => sr.Graph.ValidityRange.Union(sr.ServiceAsBits)).ToArray(); // union na konci zajistí, že vždy když jede spoj, platí i grafikon
            var graphServiceUnion = ServiceDaysBitmap.Union(graphServiceBits);

            var resultCalendar = new CalendarRecord()
            {
                StartDate = graphServiceUnion.GetFirstDayOfService().AsDateTime(GlobalStartDate), //začátek bereme dle skutečnosti, protože minulost nás nezajímá
                EndDate = graphServiceUnion.GetLastDayOfService().AsDateTime(GlobalStartDate), // konec bereme dle grafů, protože chceme predikovat, že spoj v budoucnu ještě pojede
            };

            if (graphServiceUnion.Last())
            {
                // kalendář pravděpodobně pokračuje za hranici výhledu (usuzujeme z toho, že končí jedničkou)
                // shromáždíme si tedy všechny grafikony, které platí až do konce a vezmeme jejich dny v týdnu
                foreach (var graph in graphs)
                {
                    if (graph.ValidityRange.Union(serviceBitmap).Last()) // union zajistí, že grafikon platí vždy když platí spoj
                    {
                        for (int i = 0; i < 7; i++)
                            resultCalendar.InService[i] |= graph.DaysInWeek[i];
                    }
                }
            }
            else
            {
                // žádný grafikon neplatí až do konce, sjednotíme je tedy všechny a vykašleme se na predikci
                resultCalendar.EndDate = serviceBitmap.GetLastDayOfService().AsDateTime(GlobalStartDate);
                foreach (var graph in graphs)
                {
                    for (int i = 0; i < 7; i++)
                        resultCalendar.InService[i] |= graph.DaysInWeek[i];
                }
            }

            // nakonec zbývá zapracovat výjimky napredikovaného kalendáře oproti realitě
            var bitmapBetweenStartDateEndDate = new ServiceDaysBitmap(
                serviceBitmap.Skip((resultCalendar.StartDate - GlobalStartDate).Days).Take(resultCalendar.DaysLength).ToArray()
                );
            resultCalendar.IncorporateExceptions(bitmapBetweenStartDateEndDate);

            return resultCalendar;
        }

        // vypíše kalendář a zároveň zkontroluje, že odpovídá původní bitmapě
        private void LogAndCheckCalendar(CalendarRecord calendar, ServiceDaysBitmap serviceBitmap, object originalObj)
        {
            calendarLog.Log($"    * Rekapitulace: ID={calendar.GtfsId}, Od={calendar.StartDate:d.M.yyyy}, Do={calendar.EndDate:d.M.yyyy}, Dny={calendar.ServiceAsBinaryString:0000000}, Výjimky: [{calendar.Exceptions.Count()}]");
            foreach (var ex in calendar.Exceptions.Values)
            {
                if (ex.ExceptionType == CalendarExceptionType.Add)
                    calendarLog.Log($"       +{ex.Date:d.M.yyyy}");
                else
                    calendarLog.Log($"       -{ex.Date:d.M.yyyy}");
            }

            var serviceBitsCheck = CalendarRecordAsServiceBitmap(calendar, GlobalStartDate, serviceBitmap.Length);
            log.Assert(serviceBitsCheck.Equals(serviceBitmap), LogMessageType.ERROR_TRIP_CALENDAR_WRONG, $"Sestavený kalendář {calendar} neodpovídá původní bitové mapě: {serviceBitsCheck} != {serviceBitmap}.", originalObj);

        }

        // převede kalendář zpátky na bitmapu
        private ServiceDaysBitmap CalendarRecordAsServiceBitmap(CalendarRecord calendar, DateTime globalStartDate, int bitmapLength)
        {
            var result = new bool[bitmapLength];
            for (int i = 0; i < bitmapLength; i++)
            {
                var date = globalStartDate.AddDays(i);
                result[i] = calendar.OperatesAt(date);
            }

            return new ServiceDaysBitmap(result);
        }

    }
}
