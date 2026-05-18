using CommonLibrary;
using GtfsLogging;
using JdfModel;

namespace JdfToGtfsProcessor.Calendars
{
    /// <summary>
    /// Vytváří kalendáře z pevných poznámek v JDF, tzn. nastavuje hlavně dny v týdnu a koriguje svátky.
    /// </summary>
    internal class FixedCodesCalendarProcessor
    {
        private ISimpleLogger log;

        PublicHolidaysCalendar publicHolidaysCalendar;

        Dictionary<string, FixedCode> fixedCodes;

        public FixedCodesCalendarProcessor(ISimpleLogger log, Dictionary<string, FixedCode> fixedCodes)
        {
            this.log = log;
            this.fixedCodes = fixedCodes;
            publicHolidaysCalendar = new PublicHolidaysCalendar(2020, 2060);
        }

        /// <summary>
        /// Vytvoří kalendář z pevných poznámek spoje JDF a z platnosti JŘ linky
        /// </summary>
        /// <param name="trip">JDF spoj</param>
        /// <param name="ownerRoute">Linka</param>
        /// <returns></returns>
        public GtfsModel.Extended.CalendarRecord CreateCalendarFromFixedCodes(Trip trip, Route ownerRoute)
        {
            var calendar = new GtfsModel.Extended.CalendarRecord()
            {
                StartDate = ownerRoute.ValidFrom,
                EndDate = ownerRoute.ValidTo,
            };

            var holidaysOnWorkdays = publicHolidaysCalendar.GetAllHolidaysBetween(calendar.StartDate, calendar.EndDate).Where(dt => PublicHolidaysCalendar.IsWorkday(dt.DayOfWeek)).ToArray();

            foreach (var fixedCode in trip.FixedCodes)
            {
                var fixedCodeChar = fixedCodes.GetValueOrDefault(fixedCode)?.CodeChar;
                if (fixedCodeChar != null)
                {
                    if (fixedCodeChar == FixedCodes.OperatesOnWorkdays)
                    {
                        calendar.Monday = calendar.Tuesday = calendar.Wednesday = calendar.Thursday = calendar.Friday = true;
                        foreach (var date in holidaysOnWorkdays)
                        {
                            calendar.AddException(date, GtfsModel.Enumerations.CalendarExceptionType.Remove);
                        }
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnSundaysAndHolidays)
                    {
                        calendar.Sunday = true;
                        foreach (var date in holidaysOnWorkdays)
                        {
                            calendar.AddException(date, GtfsModel.Enumerations.CalendarExceptionType.Add);
                        }
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnMonday)
                    {
                        calendar.Monday = true;
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnTuesday)
                    {
                        calendar.Tuesday = true;
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnWednesday)
                    {
                        calendar.Wednesday = true;
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnThursday)
                    {
                        calendar.Thursday = true;
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnFriday)
                    {
                        calendar.Friday = true;
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnSaturday)
                    {
                        calendar.Saturday = true;
                    }
                    else if (fixedCodeChar == FixedCodes.OperatesOnSunday)
                    {
                        calendar.Sunday = true;
                    }
                }
                else
                {
                    log.Log($"Pevný kód {fixedCode} spoje {trip} nebyl nalezen mezi pevnými kódy. Výsledný kalendář spoje může být špatně.");
                }
            }

            if (calendar.InService.All(s => !s))
            {
                calendar.Monday = calendar.Tuesday = calendar.Wednesday = calendar.Thursday = calendar.Friday = calendar.Saturday = calendar.Sunday = true;
            }

            return calendar;
        }
    }
}
