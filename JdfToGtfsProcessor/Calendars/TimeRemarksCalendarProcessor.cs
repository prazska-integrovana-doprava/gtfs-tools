using CommonLibrary;
using GtfsLogging;
using JdfModel;

namespace JdfToGtfsProcessor.Calendars
{
    /// <summary>
    /// Zpracovává časové poznámky JDF spojů (jede, nejede, jede také atd.)
    /// </summary>
    internal class TimeRemarksCalendarProcessor
    {
        private ISimpleLogger log;

        private Trip trip;

        private List<TimeRemark> tripTimeRemarks;

        public TimeRemarksCalendarProcessor(Trip trip, IEnumerable<TimeRemark> tripTimeRemarks, ISimpleLogger log)
        {
            this.trip = trip;
            this.tripTimeRemarks = tripTimeRemarks.ToList();
            this.log = log;
        }

        /// <summary>
        /// Vrací true, pokud spoj obsahuje poznámku zadaného typu.
        /// </summary>
        /// <param name="typeCode">Zadaný typ (lze použít výčet <see cref="TimeRemarkTypes"/>)</param>
        public bool HasRemarksOfType(char typeCode)
        {
            return tripTimeRemarks.Any(r => r.TimeRemarkType == typeCode);
        }

        /// <summary>
        /// Srovná časové poznámky, aby byly v rámci platnosti záznamu linky. Pokud některá poznámka je celá mimo rozsah, je smazána.
        /// </summary>
        /// <param name="routeValidFrom">Platnost záznamu linky od</param>
        /// <param name="routeValidTo">Platnost záznamu linky do</param>
        public void CorrectTimeRemarks(DateTime routeValidFrom, DateTime routeValidTo)
        {
            foreach (var timeRemark in tripTimeRemarks.ToArray()) // to array je tam, abychom udělali kopii, protože z původní kolekce budeme mazat
            {
                if (timeRemark.DateFrom.HasValue && timeRemark.DateTo.HasValue)
                {
                    if (timeRemark.DateFrom < routeValidFrom)
                    {
                        timeRemark.DateFrom = routeValidFrom;
                    }

                    if (timeRemark.DateTo > routeValidTo)
                    {
                        timeRemark.DateTo = routeValidTo;
                    }

                    if (timeRemark.DateFrom > timeRemark.DateTo)
                    {
                        tripTimeRemarks.Remove(timeRemark);
                    }
                }
                else if (timeRemark.DateFrom.HasValue && !timeRemark.DateTo.HasValue)
                {
                    if (timeRemark.DateFrom < routeValidFrom || timeRemark.DateFrom > routeValidTo)
                    {
                        tripTimeRemarks.Remove(timeRemark);
                    }
                }
            }
        }

        public GtfsModel.Extended.BaseCalendarRecord ProcessOperatesOnlyRemarks()
        {
            var operatesOnlyRemarks = tripTimeRemarks.Where(tr => tr.TimeRemarkType == TimeRemarkTypes.OperatesOnly);
            var calendar = new GtfsModel.Extended.BaseCalendarRecord();

            foreach (var remark in operatesOnlyRemarks)
            {
                if (remark.DateFrom.HasValue)
                {
                    calendar.AddException(remark.DateFrom.Value, GtfsModel.Enumerations.CalendarExceptionType.Add);

                    if (remark.DateTo.HasValue)
                    {
                        log.Log($"Spoj {trip} má poznámku {remark} \"jede také\" s vyplněným druhým datumem, což je v rozporu se specifikací JDF. Používám pouze první datum.");
                    }
                }
                else
                {
                    log.Log($"Spoj {trip} má poznámku {remark} \"jede také\" bez vyplněného data. Ignoruji.");
                }
            }

            return calendar;
        }

        public GtfsModel.Extended.BaseCalendarRecord ProcessOperatesOnRemarks(GtfsModel.Extended.CalendarRecord calendarRecord)
        {
            var operatesOnRemarks = tripTimeRemarks.Where(tr => tr.TimeRemarkType == TimeRemarkTypes.OperatesOn);
            if (operatesOnRemarks.Any2())
            {
                // pokud máme více poznámek "jede", nezbývá nám, než všechny dny vyznačit jednotlivě
                var calendar2 = new GtfsModel.Extended.BaseCalendarRecord();

                foreach (var operatesOnRemark in operatesOnRemarks)
                {
                    foreach (var date in calendarRecord.ListDates())
                    {
                        if (date >= operatesOnRemark.DateFrom && date <= (operatesOnRemark.DateTo ?? operatesOnRemark.DateFrom.Value))
                        {
                            calendar2.AddException(date, GtfsModel.Enumerations.CalendarExceptionType.Add);
                        }
                    }
                }

                return calendar2;
            }
            else if (operatesOnRemarks.Any())
            {
                // pokud máme jen jednu poznámku "jede", můžeme jen omezit datum od/do
                var operatesOnRemark = operatesOnRemarks.First();
                if (operatesOnRemark.DateFrom.HasValue)
                {
                    calendarRecord.StartDate = operatesOnRemark.DateFrom.Value;
                    calendarRecord.EndDate = operatesOnRemark.DateTo ?? operatesOnRemark.DateFrom.Value;
                }
                else
                {
                    log.Log($"Spoj {trip} má poznámku {operatesOnRemark} \"jede\" bez vyplněného data. Ignoruji.");
                }

                return calendarRecord;
            }
            else
            {
                // pokud nemáme žádnou poznámku jede, vrátíme kalendář jak byl
                return calendarRecord;
            }
        }
    }
}
