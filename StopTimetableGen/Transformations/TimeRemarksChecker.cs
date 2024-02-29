using CommonLibrary;
using GtfsModel.Extended;
using StopTimetableGen.StopTimetableModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.Transformations
{
    /// <summary>
    /// Kontroluje časová omezení a časové poznámky spojům
    /// </summary>
    class TimeRemarksChecker
    {
        private DateTime startDate;
        private DateTime endDate;

        private List<TimeRemark> pastRemarks = new List<TimeRemark>();
        private List<char> lastTimeRemarkSymbols = new List<char>() { '!', '#', '$', '*', '&', '%', '°', '§', '×', '=', '+', '@', '^', '>', '<', '?' };

        private readonly Dictionary<WeekdaySubset, DayOfWeek[]> weekdaySubsetDays = new Dictionary<WeekdaySubset, DayOfWeek[]>
        {
            { WeekdaySubset.AllDays, new [] {DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday}},
            { WeekdaySubset.Saturdays, new [] {DayOfWeek.Saturday } },
            { WeekdaySubset.Sundays, new [] {DayOfWeek.Sunday } },
            { WeekdaySubset.Weekends, new [] {DayOfWeek.Saturday, DayOfWeek.Sunday} },
            { WeekdaySubset.Workdays, new [] {DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } },
        };

        public TimeRemarksChecker(DateTime startDate, DateTime endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;
        }

        /// <summary>
        /// Vrací true, pokud spoj podle svého kalendáře jede v dané dny.
        /// </summary>
        /// <param name="calendar">Kalendář spoje</param>
        public bool SatisfiesWeekdays(CalendarRecord calendar, WeekdaySubset weekdaySubset, bool benevolent)
        {
            bool workdays, weekends;

            if (!benevolent)
            {
                workdays = calendar.Monday || calendar.Tuesday || calendar.Wednesday || calendar.Thursday || calendar.Friday;
                weekends = calendar.Saturday || calendar.Sunday;
            }
            else
            {
                workdays = calendar.ListDates().Any(d => d.DayOfWeek == DayOfWeek.Monday || d.DayOfWeek == DayOfWeek.Tuesday || d.DayOfWeek == DayOfWeek.Wednesday || d.DayOfWeek == DayOfWeek.Thursday || d.DayOfWeek == DayOfWeek.Friday);
                weekends = calendar.ListDates().Any(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday);
            }

            switch (weekdaySubset)
            {
                case WeekdaySubset.AllDays:
                    return workdays || weekends;
                case WeekdaySubset.Workdays:
                    return workdays;
                case WeekdaySubset.Weekends:
                    return weekends;
                case WeekdaySubset.Saturdays:
                    return calendar.Saturday;
                case WeekdaySubset.Sundays:
                    return calendar.Sunday;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Vrací časovou poznámku spoje, pokud některé ze dnů nejede
        /// </summary>
        /// <param name="departure">Spoj reprezentovaný odjezdem</param>
        /// <param name="weekdaySubset">Dny v týdnu, do které to dáváme</param>
        public TimeRemark GetTimeRemark(DepartureTrip departure, WeekdaySubset weekdaySubset)
        {
            var remark = new TimeRemark()
            {
                IsNight = departure.StopTime.DepartureTime >= new Time(24, 0, 0),
                IsEarlyMorning = departure.StopTime.DepartureTime <= new Time(3, 0, 0),
            };

            // zjistíme dny, kdy spoj nejede (oproti dnům v týdnu, které reprezentuje chlívek, do kterého budeme spoj vpisovat)
            foreach (var dayOfWeek in weekdaySubsetDays[weekdaySubset])
            {
                if (departure.Calendar.OperatesOn(dayOfWeek))
                {
                    remark.DaysOfWeekWithService.Add(dayOfWeek);
                }
                else if (departure.Calendar.IsDefinedOn(dayOfWeek, DaysOfWeekCalendars.TrainsInstance))
                {
                    remark.DaysOfWeekNoService.Add(dayOfWeek);
                }
            }
            
            // zjistíme dny, kdy spoj mimořádně nejede
            TimeRemark.AdjustableDateInterval currentNoServiceInterval = null;
            TimeRemark.DateInterval currentWithServiceInterval = null;
            DateTime firstDayWithoutService = DateTime.MinValue;
            for (var day = startDate; day <= endDate; day = day.AddDays(1))
            {
                var dayOfWeek = DaysOfWeekCalendars.TrainsInstance.GetDayOfWeekFor(day);
                if (remark.DaysOfWeekWithService.Contains(dayOfWeek))
                {
                    if (!departure.Calendar.OperatesAt(day))
                    {
                        // nejede
                        if (currentNoServiceInterval == null)
                        {
                            // první den kdy nejede po sérii dnů kdy jede, založíme novou sérii
                            currentNoServiceInterval = new TimeRemark.AdjustableDateInterval(remark.IsNight, remark.IsEarlyMorning)
                            {
                                MinimumFrom = firstDayWithoutService,
                                From = day,
                                To = day,
                                MaximumTo = DateTime.MaxValue,
                            };

                            remark.DaysNoService.Add(currentNoServiceInterval);
                        }
                        else
                        {
                            // další den v řadě, kdy nejede => prodloužíme sérii
                            currentNoServiceInterval.To = day;
                        }

                        if (currentWithServiceInterval != null)
                        {
                            // den, kdy nejede, po sérii dní, kdy jel, tímto ukončíme sérii
                            currentWithServiceInterval = null;
                        }
                    }
                    else
                    {
                        // jede
                        if (currentNoServiceInterval != null)
                        {
                            // den, kdy jede, po sérii dní, kdy nejel, tímto ukončíme sérii
                            currentNoServiceInterval.MaximumTo = day.AddDays(-1);
                            currentNoServiceInterval = null;
                        }

                        firstDayWithoutService = day.AddDays(1);

                        if (currentWithServiceInterval == null)
                        {
                            // první den, kdy jede, po sérii dní, kdy nejel
                            currentWithServiceInterval = new TimeRemark.DateInterval(remark.IsNight, remark.IsEarlyMorning)
                            {
                                From = day,
                                To = day,
                            };

                            remark.DaysWithService.Add(currentWithServiceInterval);
                        }
                        else
                        {
                            // další den v řadě, kdy jede => prodloužíme sérii
                            currentWithServiceInterval.To = day;
                        }

                        if (DaysOfWeekCalendars.TrainsInstance.NonredundantDayExceptions.ContainsKey(day) && remark.DaysOfWeekNoService.Any())
                        {
                            // jde o výjimečné přiřazení do tohoto provozního dne, je dobré explicitně zmínit
                            remark.DaysWithExtraService.Add(day);
                        }
                    }
                }
                else if (departure.Calendar.OperatesAt(day) && weekdaySubsetDays[weekdaySubset].Contains(dayOfWeek))
                {
                    // spoj sice v tento den v týdnu běžně nejede, avšak výjimečně tento konkrétní den ano;
                    // zároveň je o den, který spadá do zvolené množiny dní v týdnu, takže je vhodné zmínit, že vlastně jede
                    remark.DaysWithExtraService.Add(day);
                }
            }

            if (currentNoServiceInterval != null)
            {
                // poslední interval nebyl uzavřen
                remark.DaysNoService.Last().MaximumTo = DateTime.MaxValue;
            }

            if (remark.IsEmpty)
            {
                return null;
            }

            var identicalPastRemark = FindIdenticalRemark(remark);
            if (identicalPastRemark != null)
            {
                identicalPastRemark.Include(remark);
                return identicalPastRemark; // použijeme již jednou vytvořenou poznámku, ať jsou stejné
            }
            else
            {
                remark.Symbol = lastTimeRemarkSymbols.First().ToString();
                lastTimeRemarkSymbols.RemoveAt(0);
                pastRemarks.Add(remark);
                return remark;
            }
        }

        private TimeRemark FindIdenticalRemark(TimeRemark timeRemark)
        {
            foreach (var remark in pastRemarks)
            {
                if (timeRemark.IsIdenticalTo(remark))
                {
                    return remark;
                }
            }

            return null;
        }
    }
}
