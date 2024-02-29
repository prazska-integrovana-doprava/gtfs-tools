using AswModel.Extended;
using GtfsLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GtfsProcessor.Logging
{
    class TrajectoryConnectorLogger : BaseTextLogger, ITrajectoryConnectorLogger
    {
        protected class DistanceAndListOfLines
        {
            public double DistanceMeters { get; private set; }
            public List<Trip> ReferingTrips { get; private set; }

            public DistanceAndListOfLines(double distance)
            {
                DistanceMeters = distance;
                ReferingTrips = new List<Trip>();
            }
        }

        // varianty tras, kde se nepodařilo propojit fragmenty
        protected Dictionary<ShapeFragmentDescriptor, DistanceAndListOfLines> UnconnectedFragments { get; private set; }

        public TrajectoryConnectorLogger(TextWriter writer)
            : base(writer)
        {
            UnconnectedFragments = new Dictionary<ShapeFragmentDescriptor, DistanceAndListOfLines>();
        }

        public void LogUnconnected(ShapeFragmentDescriptor fragment, double distanceMeters, Trip referingTrip)
        {
            if (!UnconnectedFragments.ContainsKey(fragment))
            {
                UnconnectedFragments.Add(fragment, new DistanceAndListOfLines(distanceMeters));
            }

            UnconnectedFragments[fragment].ReferingTrips.Add(referingTrip);
        }

        /// <summary>
        /// Uloží duplicitní fragmetny seřazené podle četnosti výskytu.
        /// Uloží chybějící fragmenty seřazené podle počtu tras.
        /// </summary>
        public override void Close()
        {
            foreach (var unconnected in UnconnectedFragments.OrderByDescending(frag => frag.Value.DistanceMeters))
            {
                var distance = Math.Round(unconnected.Value.DistanceMeters);
                Writer.WriteLine($"{unconnected.Key} ({distance} metrů, {AswModel.Extended.Logging.TrajectoryDbLogger.GetAffectedLinesAndTrips(unconnected.Value.ReferingTrips)})");
            }

            base.Close();
        }
    }

}
