using GtfsLogging;
using System;
using System.Collections.Generic;
using System.IO;

namespace GtfsProcessor.Logging
{
    internal class MergedTripsLogger : BaseTextLogger, IMergedTripsLogger
    {
        private List<Tuple<LoggedTrip, LoggedTrip, LoggedTrip>> merged;
        private Dictionary<TripEqualityResult, HashSet<Tuple<LoggedTrip, LoggedTrip>>> comments;

        public MergedTripsLogger(TextWriter writer) 
            : base(writer)
        {
            merged = new List<Tuple<LoggedTrip, LoggedTrip, LoggedTrip>>();
            comments = new Dictionary<TripEqualityResult, HashSet<Tuple<LoggedTrip, LoggedTrip>>>();
        }

        public void LogMerged(LoggedTrip result, LoggedTrip first, LoggedTrip second)
        {
            merged.Add(new Tuple<LoggedTrip, LoggedTrip, LoggedTrip>(first, second, result));
        }

        public void LogComment(LoggedTrip first, LoggedTrip second, TripEqualityResult equalityResult)
        {
            if (!comments.ContainsKey(equalityResult))
            {
                comments.Add(equalityResult, new HashSet<Tuple<LoggedTrip, LoggedTrip>>());
            }

            comments[equalityResult].Add(new Tuple<LoggedTrip, LoggedTrip>(first, second));
        }

        public override void Close()
        {
            foreach (var equalityPairs in comments)
            {
                Writer.WriteLine(equalityPairs.Key.ToString());

                foreach (var tripPair in equalityPairs.Value)
                {
                    Writer.WriteLine($"{tripPair.Item1} & {tripPair.Item2}");
                }

                Writer.WriteLine("");
            }

            Writer.WriteLine("Merged:");
            foreach (var mergedTriple in merged)
            {
                Writer.WriteLine($"{mergedTriple.Item1} & {mergedTriple.Item2} -> {mergedTriple.Item3}");
            }

            base.Close();
        }
    }
}
