using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GtfsModel.Extended;

namespace GtfsModel.Functions
{
    public static class VerboseDescriptor
    {
        public static string DescribeTrip(Trip trip)
        {
            var shortName = "";
            if (!string.IsNullOrEmpty(trip.ShortName))
            {
                shortName = $" ({trip.ShortName})";
            }

            var result = new StringBuilder($"Trip {trip.GtfsId}{shortName} route {trip.Route.ShortName} ({trip.Route.GtfsId}), direction {trip.Headsign} [{trip.DirectionId}], accessibility {trip.WheelchairAccessible}:\n");
            result.AppendLine($"  - calendar {DescribeCalendarRecord(trip.CalendarRecord)}");
            result.AppendLine($"  - stoptimes:");
            if (trip.PreviousTripInBlock != null)
            {
                result.AppendLine($"      continues {trip.PreviousTripInBlock.GtfsId} from {trip.PreviousTripInBlock.StopTimes.First().Stop.Name}");
            }

            foreach (var stopTime in trip.PublicStopTimes)
            {
                result.AppendLine($"      {DescribeStopTime(stopTime)}");
            }

            if (trip.NextTripInBlock != null)
            {
                result.AppendLine($"      continues as {trip.NextTripInBlock.GtfsId} to {trip.NextTripInBlock.StopTimes.Last().Stop.Name}");
            }

            return result.ToString(); ;
        }

        public static string DescribeCalendarRecord(CalendarRecord calendarRecord)
        {
            var result = new StringBuilder($"{calendarRecord.StartDate:dd.MM.yyyy}-{calendarRecord.EndDate:dd.MM.yyyy}:");
            var currentMonth = new DateTime(DateTime.Now.AddDays(-3).Year, DateTime.Now.AddDays(-3).Month, 1);
            var serviceBitmap = calendarRecord.AsServiceBitmap(currentMonth);
            var serviceBitmapIndex = 0;
            while (currentMonth < calendarRecord.EndDate)
            {
                result.Append($" || {currentMonth:MMM} ");
                for (int i = 0; i < (currentMonth.AddMonths(1) - currentMonth).Days; i++)
                {
                    if (i > 0 && currentMonth.AddDays(i).DayOfWeek == DayOfWeek.Monday)
                    {
                        result.Append(" ");
                    }

                    if (serviceBitmapIndex < serviceBitmap.Length)
                    {
                        result.Append(serviceBitmap[serviceBitmapIndex] ? "1" : "0");
                    }
                    else
                    {
                        break;
                    }

                    serviceBitmapIndex++;
                }

                currentMonth = currentMonth.AddMonths(1);
            }

            return result.ToString();
        }

        public static string DescribeStopTime(StopTime stopTime)
        {
            var times = stopTime.DepartureTime.ToString();
            if (stopTime.ArrivalTime != stopTime.DepartureTime)
            {
                times = $"{stopTime.ArrivalTime}-{stopTime.DepartureTime}";
            }

            return $"{stopTime.Stop.Name} [{stopTime.Stop.ZoneId}] {times}";
        }
    }
}
