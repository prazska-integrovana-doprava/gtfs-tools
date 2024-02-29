using CommonLibrary;
using GtfsLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AswModel.Extended.Logging
{
    public class TrajectoryDbLogger : BaseTextLogger, ITrajectoryDbLogger
    {
        // počty výskytů duplicitních fragmentů
        protected Dictionary<ShapeFragmentDescriptor, List<ServiceDaysBitmap>> DuplicateFragments { get; private set; }

        // varianty tras, kterým chyběl fragment
        protected Dictionary<ShapeFragmentDescriptor, List<Trip>> MissingFragments { get; private set; }

        protected Dictionary<ShapeFragmentDescriptor, List<Trip>> PartiallyMissingFragments { get; private set; }
        
        public TrajectoryDbLogger(TextWriter writer)
            : base(writer)
        {
            DuplicateFragments = new Dictionary<ShapeFragmentDescriptor, List<ServiceDaysBitmap>>();
            MissingFragments = new Dictionary<ShapeFragmentDescriptor, List<Trip>>();
            PartiallyMissingFragments = new Dictionary<ShapeFragmentDescriptor, List<Trip>>();
        }

        public void LogDuplicate(ShapeFragmentDescriptor descriptor, ServiceDaysBitmap serviceAsBits)
        {
            if (!DuplicateFragments.ContainsKey(descriptor))
            {
                DuplicateFragments.Add(descriptor, new List<ServiceDaysBitmap>() { serviceAsBits });
            }

            DuplicateFragments[descriptor].Add(serviceAsBits);
        }

        public void LogMissing(ShapeFragmentDescriptor descriptor, Trip referingTrip)
        {
            if (!MissingFragments.ContainsKey(descriptor))
            {
                MissingFragments.Add(descriptor, new List<Trip>());
            }

            MissingFragments[descriptor].Add(referingTrip);
        }

        public void LogPartiallyMissing(ShapeFragmentDescriptor descriptor, Trip referingTrip)
        {
            if (!PartiallyMissingFragments.ContainsKey(descriptor))
            {
                PartiallyMissingFragments.Add(descriptor, new List<Trip>());
            }

            PartiallyMissingFragments[descriptor].Add(referingTrip);
        }
        
        /// <summary>
        /// Uloží duplicitní fragmetny seřazené podle četnosti výskytu.
        /// Uloží chybějící fragmenty seřazené podle počtu tras.
        /// </summary>
        public override void Close()
        {
            Writer.WriteLine("Duplicitní (je použit vždy první výskyt):");
            foreach (var duplicate in DuplicateFragments.OrderByDescending(frag => frag.Value.Count))
            {
                var duplicateServiceBits = string.Join(", ", duplicate.Value);
                Writer.WriteLine($"{duplicate.Key} (ignorováno: {duplicateServiceBits})");
            }

            Writer.WriteLine("");
            Writer.WriteLine("Chybějící trasy (je nahrazeno přímým propojením zastávek):");

            foreach (var missing in MissingFragments.OrderByDescending(frag => frag.Value.Count))
            {
                Writer.WriteLine($"{missing.Key} ({GetAffectedLinesAndTrips(missing.Value, true)})");
            }

            Writer.WriteLine("");
            Writer.WriteLine("Trasy, kde nebyla nalezena přesná verze (byla použita nejbližší vhodná verze):");

            foreach (var missing in PartiallyMissingFragments.OrderByDescending(frag => frag.Value.Count))
            {
                Writer.WriteLine($"{missing.Key} ({GetAffectedLinesAndTrips(missing.Value, true)})");
            }

            base.Close();
        }

        private class TripByLineEqualityComparer : IEqualityComparer<Trip>
        {
            public bool Equals(Trip x, Trip y)
            {
                return x.Route.Equals(y.Route);
            }

            public int GetHashCode(Trip obj)
            {
                return obj.Route.GetHashCode();
            }
        }

        public static string GetAffectedLinesAndTrips(List<Trip> referingTrips, bool withCal = false)
        {
            var referingLines = referingTrips.Select(t => t.Route).Distinct();
            var lines = string.Join(", ", referingLines.Select(line => line.LineName));
            if (withCal)
            {
                var trips = referingTrips.Distinct(new TripByLineEqualityComparer()).Select(t => $"{t} cal {t.ServiceAsBits}").ToArray();
                return $"využito linkami {lines}, spoje {string.Join(", ", trips)}";
            }
            else
            {
                var trips = referingTrips.Distinct(new TripByLineEqualityComparer()).Select(t => t.ToString()).ToArray();
                return $"využito trasami {lines}, spoje {string.Join(", ", trips)}";
            }
        }
    }
}
