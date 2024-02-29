using AswModel.Extended;
using GtfsProcessor.DataClasses;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Převádí ASW reprezentaci oběhů <see cref="RunDescriptor"/> do mé GTFS interpretace <see cref="GtfsModel.RunTrip"/>.
    /// </summary>
    class RunsTransformation
    {
        // mapování ASW spojů na sloučené spoje
        private Dictionary<Trip, MergedTripGroup> tripsToGroups;

        public List<MergedRun> Runs { get; private set; }

        /// <summary>
        /// Vytvoří instanci, v rámci čehož si vygeneruje kolekci <see cref="MergedRun"/> z ASW spojů a oběhů
        /// </summary>
        /// <param name="mergedTripGroups">Mapování ASW spojů na sloučené spoje</param>
        public RunsTransformation(IEnumerable<MergedTripGroup> mergedTripGroups)
        {
            tripsToGroups = new Dictionary<Trip, MergedTripGroup>();
            foreach (var tripGroup in mergedTripGroups)
            {
                foreach (var trip in tripGroup.AllTrips)
                {
                    tripsToGroups.Add(trip, tripGroup);
                }
            }

            // merge oběhů (když je shodná kmenová linka, oběh a posloupnost tripů, viz MergedRun.IsEqual)
            var runsNotMerged = tripsToGroups.Keys.SelectMany(t => t.OwnerRun).Distinct().ToArray();
            Runs = new List<MergedRun>();
            foreach (var run in runsNotMerged)
            {
                bool wasMerged = false;
                foreach (var presentRun in Runs)
                {
                    if (presentRun.IsEqual(run, tripsToGroups))
                    {
                        presentRun.Merge(run);
                        wasMerged = true;
                        break;
                    }
                }

                if (!wasMerged)
                {
                    Runs.Add(MergedRun.Construct(run, tripsToGroups));
                }
            }
        }

        /// <summary>
        /// Vrátí oběhy v mé GTFS reprezentaci
        /// </summary>
        /// <param name="tripsMapping">Mapování spojů na GTFS záznamy</param>
        /// <param name="calendarToRunAssignment">Mapování kalendářů pro oběhy</param>
        public IEnumerable<GtfsModel.RunTrip> GetTransformedRuns(IDictionary<MergedTripGroup, GtfsModel.Extended.Trip> tripsMapping,
            IDictionary<MergedRun, GtfsModel.Extended.CalendarRecord> calendarToRunAssignment)
        {
            foreach (var run in Runs)
            {
                foreach (var tripAndNumber in run.TripsAndNumbers)
                {
                    yield return new GtfsModel.RunTrip()
                    {
                        RouteId = $"L{run.RootLineNumber}",
                        RunNumber = run.RunNumber,
                        ServiceId = calendarToRunAssignment[run].GtfsId,
                        TripId = tripsMapping[tripAndNumber.TripData].GtfsId,
                        VehicleType = tripAndNumber.TripData.ReferenceTrip.VehicleType,
                        RouteLicenceNumber = tripAndNumber.RouteLicenceNumber,
                        TripNumber = tripAndNumber.TripData.TrafficType != AswTrafficType.Metro 
                            ? tripAndNumber.TripNumber
                            : tripAndNumber.TripNumber % 1000, //u metra chceme jen číslo vlaku (bez čísla grafikonu)
                    };
                }
            }
        }
    }
}
