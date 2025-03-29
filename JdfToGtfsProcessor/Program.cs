using CommonLibrary;
using CsvSerializer;
using JdfModel;
using System;
using System.Linq;
using System.Text;

namespace JdfToGtfsProcessor
{
    class Program
    {
        private static readonly DateTime[] SlovakHolidays = new DateTime[]
        {
            new DateTime(2025, 7, 5),
            new DateTime(2025, 8, 29),
            new DateTime(2025, 9, 1),
            new DateTime(2025, 9, 15),
            new DateTime(2025, 10, 28),
            new DateTime(2025, 11, 1),
            new DateTime(2025, 11, 17),
            new DateTime(2025, 12, 24),
            new DateTime(2025, 12, 25),
            new DateTime(2025, 12, 26),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 6),
            new DateTime(2026, 4, 3),
            new DateTime(2026, 4, 6),
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 8),
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Načítám JDF");
            var stops = CsvFileSerializer.DeserializeFile<Stop>(@"c:\temp\jrspoje\jdf\zastavky.txt", ',', null, "ddMMyyyy", Encoding.Default, false);
            var stopData = CsvFileSerializer.DeserializeFile<StopData>(@"c:\temp\jrspoje\jdf\ZastavkyData.csv", ';', null, "ddMMyyyy", Encoding.Default);
            var stopDataDictionary = stopData.ToDictionary(sd => sd.StopId);
            var agencies = CsvFileSerializer.DeserializeFile<Agency>(@"c:\temp\jrspoje\jdf\dopravci.txt", ',', null, "ddMMyyyy", Encoding.Default, false);
            var routes = CsvFileSerializer.DeserializeFile<Route>(@"c:\temp\jrspoje\jdf\linky.txt", ',', null, "ddMMyyyy", Encoding.Default, false);
            var routesDictionary = routes.ToDictionary(r => r.RouteId);
            var trips = CsvFileSerializer.DeserializeFile<Trip>(@"c:\temp\jrspoje\jdf\spoje.txt", ',', null, "ddMMyyyy", Encoding.Default, false);
            var fixedCodes = CsvFileSerializer.DeserializeFile<FixedCode>(@"c:\temp\jrspoje\jdf\pevnykod.txt", ',', null, "ddMMyyyy", Encoding.Default, false);
            var fixedCodesDictionary = fixedCodes.ToDictionary(fc => fc.CodeId);
            var timeRemarks = CsvFileSerializer.DeserializeFile<TimeRemark>(@"c:\temp\jrspoje\jdf\caskody.txt", ',', null, "ddMMyyyy", Encoding.Default, false);
            var routeStops = CsvFileSerializer.DeserializeFile<RouteStop>(@"c:\temp\jrspoje\jdf\zaslinky.txt", ',', null, "ddMMyyyy", Encoding.Default, false);
            var stopTimes = CsvFileSerializer.DeserializeFile<StopTime>(@"c:\temp\jrspoje\jdf\zasspoje.txt", ',', null, "ddMMyyyy", Encoding.Default, false);

            Console.WriteLine("Transformuji");
            var resultFeed = new GtfsModel.Extended.Feed();

            foreach (var stop in stops)
            {
                var thisStopData = stopDataDictionary[stop.StopId];

                resultFeed.Stops.Add(stop.StopId.ToString(), new GtfsModel.Extended.Stop()
                {
                    GtfsId = stop.StopId.ToString(),
                    Name = stop.NamePart1 + "," + stop.NamePart2 + "," + stop.NamePart3,
                    Position = new GpsCoordinates()
                    {
                        GpsLatitude = thisStopData.LatitudeTimes10000 / 10000.0,
                        GpsLongitude = thisStopData.LongitudeTimes10000 / 10000.0,
                    }
                });
            }

            foreach (var agency in agencies)
            {
                resultFeed.Agency.Add(agency.Id, new GtfsModel.GtfsAgency()
                {
                    Id = agency.Id,
                    Name = agency.Name,
                    Phone = agency.PhoneToAddress,
                    Timezone = "Europe/Prague",
                    Url = "https://agency_web_page.sk",
                });
            }

            foreach (var route in routes)
            {
                resultFeed.Routes.Add(route.RouteId, new GtfsModel.Extended.Route()
                {
                    GtfsId = route.RouteId,
                    LongName = route.RouteName,
                    ShortName = route.RouteId,
                    Type = GtfsModel.Enumerations.TrafficType.Bus,
                    Color = System.Drawing.Color.White,
                });
            }

            foreach (var trip in trips)
            {
                var tripId = trip.RouteId + "_" + trip.TripNumber.ToString();

                var calendar = new GtfsModel.Extended.CalendarRecord()
                {
                    GtfsId = tripId + "c",
                    StartDate = routesDictionary[trip.RouteId].ValidFrom,
                    EndDate = routesDictionary[trip.RouteId].ValidTo,
                };

                foreach (var fixedCode in trip.FixedCodes)
                {
                    var fixedCodeChar = fixedCodesDictionary[fixedCode].CodeChar;
                    if (fixedCodeChar == 'X')
                    {
                        calendar.Monday = calendar.Tuesday = calendar.Wednesday = calendar.Thursday = calendar.Friday = true;
                        foreach (var date in SlovakHolidays.Where(dt => dt >= calendar.StartDate && dt <= calendar.EndDate))
                        {
                            calendar.AddException(date, GtfsModel.Enumerations.CalendarExceptionType.Remove);
                        }
                    }
                    else if (fixedCodeChar == '6')
                    {
                        calendar.Saturday = true;
                    }
                    else if (fixedCodeChar == '+')
                    {
                        calendar.Sunday = true;
                        foreach (var date in SlovakHolidays.Where(dt => dt >= calendar.StartDate && dt <= calendar.EndDate))
                        {
                            calendar.AddException(date, GtfsModel.Enumerations.CalendarExceptionType.Add);
                        }
                    }
                }

                var tripTimeRemarks = timeRemarks.Where(tr => tr.RouteId == trip.RouteId && tr.TripNumber == trip.TripNumber).ToArray();
                var operatesOnRemarks = tripTimeRemarks.Where(tr => tr.TimeRemarkType == TimeRemarkTypes.OperatesOn);
                if (operatesOnRemarks.Any())
                {
                    var calendar2 = new GtfsModel.Extended.CalendarRecord()
                    {
                        GtfsId = calendar.GtfsId,
                        StartDate = calendar.StartDate,
                        EndDate = calendar.EndDate,
                    };

                    foreach (var operatesOnRemark in operatesOnRemarks)
                    {
                        foreach (var date in calendar.ListDates())
                        {
                            if (date >= operatesOnRemark.DateFrom && date <= operatesOnRemark.DateTo)
                            {
                                calendar2.AddException(date, GtfsModel.Enumerations.CalendarExceptionType.Add);
                            }
                        }
                    }

                    calendar = calendar2;
                }

                var doesNotOperateOnRemarks = tripTimeRemarks.Where(tr => tr.TimeRemarkType == TimeRemarkTypes.DoesNotOperateOn).ToArray();
                foreach (var doesNotOperateOnRemark in doesNotOperateOnRemarks)
                {
                    foreach (var date in calendar.ListDates())
                    {
                        if (date >= doesNotOperateOnRemark.DateFrom && date <= doesNotOperateOnRemark.DateTo)
                        {
                            calendar.AddExceptionIgnoreOlder(date, GtfsModel.Enumerations.CalendarExceptionType.Remove);
                        }
                    }
                }

                resultFeed.Trips.Add(tripId, new GtfsModel.Extended.Trip()
                {
                    CalendarRecord = calendar,
                    GtfsId = tripId,
                    DirectionId = trip.TripNumber % 2 == 0 ? GtfsModel.Enumerations.Direction.Inbound : GtfsModel.Enumerations.Direction.Outbound,
                    Route = resultFeed.Routes[trip.RouteId],
                });

                resultFeed.Calendar.Add(calendar.GtfsId, calendar);
            }

            foreach (var stopTime in stopTimes)
            {
                var routeStop = routeStops.First(rs => rs.RouteId == stopTime.RouteId && rs.StopIndex == stopTime.StopIndex);
                var tripId = stopTime.RouteId + "_" + stopTime.TripNumber.ToString();
                var departureTime = ParseJdfTime(stopTime.DepartureTime);
                var arrivalTime = ParseJdfTime(stopTime.ArrivalTime);
                if (!departureTime.HasValue && !arrivalTime.HasValue)
                    continue;

                var fixedCodeChars = routeStop.FixedCodes.Union(stopTime.FixedCodes).Select(fc => fixedCodesDictionary[fc].CodeChar).ToArray();
                bool isRequestStop = fixedCodeChars.Contains('x');

                var stopTimesForTrip = resultFeed.Trips[tripId].StopTimes;
                var previousStopTime = stopTimesForTrip.LastOrDefault();
                stopTimesForTrip.Add(new GtfsModel.Extended.StopTime()
                {
                    ArrivalTime = arrivalTime ?? departureTime.Value,
                    DepartureTime = departureTime ?? arrivalTime.Value,
                    DropOffType = isRequestStop ? GtfsModel.Enumerations.DropOffType.DriverRequest : GtfsModel.Enumerations.DropOffType.Regular,
                    PickupType = isRequestStop ? GtfsModel.Enumerations.PickupType.DriverRequest : GtfsModel.Enumerations.PickupType.Regular,
                    SequenceNumber = (previousStopTime?.SequenceNumber ?? 0) + 1,
                    ShapeDistanceTraveledMeters = stopTime.DistanceFromStartKm * 1000 ?? previousStopTime?.ShapeDistanceTraveledMeters ?? 0,
                    Trip = resultFeed.Trips[tripId],
                    Stop = (GtfsModel.Extended.Stop) resultFeed.Stops[stopTime.StopId.ToString()],
                });
            }

            foreach (var trip in resultFeed.Trips.Values)
            {
                trip.Headsign = trip.StopTimes.Last().Stop.Name;
            }

            Console.WriteLine("Ukládám");
            var gtfsFeed = resultFeed.ToGtfsFeed();
            GtfsModel.Functions.GtfsFeedSerializer.SerializeFeed(@"c:\temp\jrspoje\jdf\gtfs", gtfsFeed);

            Console.WriteLine("Hotovo.");
        }

        private static Time? ParseJdfTime(string timeStr)
        {
            if (timeStr.Length >= 3 && int.TryParse(timeStr, out int timeVal))
            {
                var hrs = timeVal / 100;
                var mins = timeVal % 100;
                return new Time(hrs, mins, 0);
            }
            else
            {
                return null;
            }
        }
    }
}
