using System.Collections.Generic;

namespace GtfsModel
{
    /// <summary>
    /// Reprezentuje jeden celý GTFS feed (odpovídá složce s TXT soubory na disku).
    /// </summary>
    public class GtfsFeed
    {
        public List<GtfsAgency> Agency { get; set; }

        public List<GtfsStop> Stops { get; set; }

        public List<GtfsRoute> Routes { get; set; }

        public List<GtfsTrip> Trips { get; set; }

        public List<GtfsStopTime> StopTimes { get; set; }

        public List<GtfsCalendarRecord> Calendar { get; set; }

        public List<GtfsCalendarDate> CalendarDates { get; set; }

        public List<GtfsFareRule> FareRules { get; set; }

        public List<GtfsShapePoint> Shapes { get; set; }

        public List<GtfsTransfer> Transfers { get; set; }

        public List<GtfsFeedInfo> FeedInfo { get; set; }
        
        // rozšíření...

        public List<RouteSubAgency> RouteSubAgencies { get; set; }

        public List<RunTrip> TripRuns { get; set; }

        public List<RouteStop> RouteStops { get; set; }
    }
}
