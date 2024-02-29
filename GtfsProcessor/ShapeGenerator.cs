using CommonLibrary;
using CsvSerializer;
using GtfsLogging;
using GtfsProcessor.DataClasses;
using GtfsProcessor.Logging;
using System.Collections.Generic;

namespace GtfsProcessor
{
    /// <summary>
    /// Vytváří GTFS trasy (shapes) na základě sledu zastávek spojů (<see cref="MergedTripGroup"/>). Trasy se sestavují z mezizastávkových tras,
    /// na které odkazují přímo samotné spoje. Spoje, které mají shodný průběh (sled zastávek včetně mezilehlých neveřejných i mezizastávkové trasy),
    /// budou sdílet stejnou trasu (shape). ID shape se odvozuje z čísla linky, pokud však dva spoje na různých linkách mají stejnou trasu,
    /// nemusí číslo linky v shape odpovídat číslu linky spoje.
    /// 
    /// Samotné generování shapes spočívající v propojování mezizastávkových tras provádí třída <see cref="ShapeFragmentConnector"/>
    /// </summary>
    class ShapeGenerator
    {
        /// <summary>
        /// Porovnává spoje podle trasy - stejné jsou spoje se stejným sledem public části zastávek (ignorují se neveřejné zastávky na začátku
        /// a na konci trasy, ale neveřejné zastávky uprostřed trasy se počítají). 
        /// 
        /// U metra stačí shodný sled zastávek, u ostatních spojů shodné musí být též i mezizastávkové trasy.
        /// </summary>
        private class TripStopSequenceComparer : IEqualityComparer<MergedTripGroup>
        {
            public bool Equals(MergedTripGroup x, MergedTripGroup y)
            {
                if (x == y)
                    return true;
                else if (x == null || y == null)
                    return false;

                if (x.StopTimes.Length != y.StopTimes.Length)
                    return false;

                for (int i = 0; i < x.StopTimes.Length; i++)
                {
                    if (!x.StopTimes[i].Stop.Equals(y.StopTimes[i].Stop))
                        return false;

                    if ((x.TrafficType != AswModel.Extended.AswTrafficType.Metro || y.TrafficType != AswModel.Extended.AswTrafficType.Metro)
                        && !Equals(x.StopTimes[i].TrackToThisStop, y.StopTimes[i].TrackToThisStop))
                        return false;
                }

                return true;
            }

            public int GetHashCode(MergedTripGroup obj)
            {
                int result = 0;
                foreach (var stopTime in obj.StopTimes)
                {
                    if (stopTime.TrackToThisStop != null && obj.TrafficType != AswModel.Extended.AswTrafficType.Metro)
                    {
                        result ^= stopTime.Stop.GetHashCode() ^ stopTime.TrackToThisStop.GetHashCode();
                    }
                    else
                    {
                        result ^= stopTime.Stop.GetHashCode();
                    }
                }

                return result;
            }
        }

        
        // pro číslování tras, pro každou linku extra číselná řada
        private Dictionary<int, int> lastShapeId;

        public ShapeGenerator()
        {
            lastShapeId = new Dictionary<int, int>();
        }

        /// <summary>
        /// Vytvoří všem tripům GTFS trasu na základě trasy a úseků. Tripy se shodnou trasou budou sdílet shodný shape.
        /// </summary>
        /// <param name="trips">Tripy (sloučené po duplicitách)</param>
        /// <param name="shapeToTripAssignment">Parametr, do kterého se ukládá mapování tras na spoje</param>
        /// <returns>Všechny vygenerované trasy</returns>
        public IEnumerable<GtfsModel.Extended.Shape> GenerateAndAssignShapes(IEnumerable<MergedTripGroup> trips, out Dictionary<MergedTripGroup, ShapeEx> shapeToTripAssignment,
            ShapeConstructor shapeConstructor, IEnumerable<AswModel.Extended.Stop> metroStations, ICommonLogger log)
        {
            var shapeFragmentConnector = new ShapeFragmentConnector();
            var metroShapeConstructor = new MetroShapeConstructor(shapeConstructor, metroStations, log);
            shapeToTripAssignment = new Dictionary<MergedTripGroup, ShapeEx>(new TripStopSequenceComparer());
            foreach (var trip in trips)
            {
                // pokud už existuje trip se shodnou trasu (všimněte si zadaného compareru), vrátíme existující trasu
                var shape = shapeToTripAssignment.GetValueOrDefault(trip);
                if (shape == null)
                {
                    // nenalezena správná verze, zkusíme si nechat vyrobit novou
                    if (trip.TrafficType != AswModel.Extended.AswTrafficType.Metro)
                    {
                        shape = shapeFragmentConnector.CreateShapeForTrip(trip);
                    }
                    else
                    {
                        shape = metroShapeConstructor.CreateShapeForTrip(trip);
                    }

                    shapeToTripAssignment.Add(trip, shape);
                    SetShapeId(trip.Route.LineNumber, shape);
                }
            }

            return shapeToTripAssignment.Values;
        }
        
        private void SetShapeId(int lineNumber, ShapeEx shape)
        {
            if (!lastShapeId.ContainsKey(lineNumber))
            {
                lastShapeId.Add(lineNumber, 0);
            }

            int variantIndex = ++lastShapeId[lineNumber];
            shape.Id = $"L{lineNumber}V{variantIndex}";
        }
    }
}
