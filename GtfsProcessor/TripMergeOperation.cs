using System.Collections.Generic;
using System.Linq;
using AswModel.Extended;
using GtfsProcessor.Logging;
using System;
using GtfsProcessor.DataClasses;

namespace GtfsProcessor
{
    /// <summary>
    /// Sloučí spoje, které jsou vlastně úplně stejné, ale jedou v různé provozní dny (podle různých grafikonů).
    /// Například pozdě večer nebo o víkendu je všednodenní spoj shodný s víkendovým a je škoda mít tam
    /// dva záznamy.
    /// 
    /// Vstupem je množina ASW spojů <see cref="Trip"/> (po linkách). Výstupem je množina sloučených skupin <see cref="MergedTripGroup"/>.
    /// </summary>
    class TripMergeOperation
    {
        /// <summary>
        /// Porovnává spoje, jestli jsou stejné (až na provozní dny, které naopak musí být disjunktní)
        /// </summary>
        private class TripComparer
        {
            /// <summary>
            /// Dva spoje se rovnají, pokud
            /// - jsou na stejné lince ve stejném směru
            /// - mají stejnou bezbariérovou přístupnost
            /// - pokud přejíždí, tak se rovnají i spoje, na které přejíždí
            /// - nemají žádný společný provozní den
            /// - mají stejné zastávky a příjezdy a odjezdy v nich
            /// </summary>
            /// <param name="x">První spoj</param>
            /// <param name="y">Druhý spoj</param>
            /// <returns>Výsledek porovnání viz <see cref="TripEqualityResult"/></returns>
            public TripEqualityResult Equals(MergedTripGroup x, Trip y)
            {
                if (x.Route != y.Route || x.Agency != y.Agency)
                {
                    return TripEqualityResult.AreDifferent;
                }

                if (x.DirectionId != y.DirectionId || x.IsWheelchairAccessible != y.IsWheelchairAccessible)
                {
                    return TripEqualityResult.AreDifferent;
                }

                if ((x.NextPublicTripInBlock == null) != (y.NextPublicTripInBlock == null))
                {
                    // jeden spoj pokračuje, druhý ne
                    return TripEqualityResult.AreDifferent;
                }

                if (!CompareStopTimeCollection(x.PublicStopTimes, y.PublicStopTimes))
                {
                    // rozdíly v zastávkách / odjezdech / příjezdech
                    return TripEqualityResult.AreDifferent;
                }

                if (x.NextPublicTripInBlock != null)
                {
                    var nextTripResult = Equals(x.NextPublicTripInBlock, y.NextPublicTripInBlock);
                    if (nextTripResult != TripEqualityResult.AreEqualAndDisjunct && nextTripResult != TripEqualityResult.AreTotallySame)
                    {
                        return nextTripResult;
                    }
                }

                if (x.ServiceAsBits.Equals(y.ServiceAsBits))
                {
                    if (x.AllTrips.Any(t => t.RootLineNumber == y.RootLineNumber && t.RootRunNumber == y.RootRunNumber))
                        // stejný kalendář a dokonce i stejný oběh
                        return TripEqualityResult.AreTotallySame;
                    else
                        // stejný kalendář, ale různý oběh (asi posila)
                        return TripEqualityResult.AreSameWithDifferentCircleNumber;
                }

                var calendarIntersect = x.ServiceAsBits.Intersect(y.ServiceAsBits);
                if (!calendarIntersect.IsEmpty)
                {
                    // spoje by neměly jet oba ve stejný den
                    return TripEqualityResult.AreEqualButNotDisjunct;
                }

                return TripEqualityResult.AreEqualAndDisjunct;

            }

            private bool CompareStopTimeCollection(IEnumerable<StopTime> x, IEnumerable<StopTime> y)
            {
                var enuFirst = x.GetEnumerator();
                var enuSecond = y.GetEnumerator();

                // cyklus končí jakmile v jednom ze seznamů dojdou zastávky
                bool isNextFirst, isNextSecond;
                bool first = true;
                while ((isNextFirst = enuFirst.MoveNext()) & (isNextSecond = enuSecond.MoveNext()))
                { 
                    if (!CompareStopTimes(enuFirst.Current, enuSecond.Current, first, false))
                    {
                        // enumerator neumí říct, jestli prvek je poslední a nechceme volat MoveNext, abychom si ho nezničili, takže porovnáváme bez znalosti toho,
                        // jestli jde o poslední stoptime a až když nám vyjde, že jsou stoptimy různé, zkusíme jako poslední záchranu, jestli náhodou nejde
                        // o poslední stoptime a pokud ano, jestli porovnání s tímto příznakem ještě nevrátí true
                        // - pokud nejde o poslední stoptime, nebo se liší i tak, vrátíme tedy false
                        var e1 = enuFirst.Current;
                        var e2 = enuSecond.Current;
                        var last = !enuFirst.MoveNext() && !enuSecond.MoveNext();
                        return last && CompareStopTimes(e1, e2, false, true);
                    }

                    first = false;
                }

                // pokud skončily oba seznamy zastávek najednou, pak je to v pořádku
                return !isNextFirst && !isNextSecond;
            }

            private bool CompareStopTimes(StopTime x, StopTime y, bool isFirst, bool isLast)
            {
                return x.Stop == y.Stop && (isFirst || x.ArrivalTime == y.ArrivalTime) && (isLast || x.DepartureTime == y.DepartureTime) && x.TrackToThisStop == y.TrackToThisStop
                    && x.IsRequestStop == y.IsRequestStop && Enumerable.SequenceEqual(x.TimedTransferRemarks, y.TimedTransferRemarks);
            }
        }


        private IEnumerable<Route> allRouteVersions;
        private IMergedTripsLogger mergeLog = Loggers.MergedTripsLoggerInstance;

        public TripMergeOperation(IEnumerable<Route> allRouteVersions)
        {
            this.allRouteVersions = allRouteVersions;
        }

        /// <summary>
        /// Porovná spoje v rámci linek a vrátí je sloučené po skupinách <see cref="MergedTripGroup"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MergedTripGroup> Perform()
        {
            foreach (var route in allRouteVersions)
            {
                var mergedTrips = MergeTrips(route.GetAllPublicTripsFirstInBlock().ToList());
                foreach (var trip in mergedTrips)
                {
                    // merge funguje nad prvními spoji v blocku a také vrací jen první spoje v blocku (merguje celý blok), musíme vrátit i ty následné
                    var iterator = trip;
                    while (iterator != null)
                    {
                        yield return iterator;
                        iterator = iterator.NextPublicTripInBlock;
                    }
                }
            }
        }

        // vrací sloučené spoje, porovnává každý s každým a když najde dva stejné, tak první upraví
        // a druhý zruší
        private IEnumerable<MergedTripGroup> MergeTrips(List<Trip> trips)
        {
            var comparer = new TripComparer();
            var removedTrips = new HashSet<Trip>();

            for (int i = 0; i < trips.Count; i++)
            {
                if (removedTrips.Contains(trips[i]))
                    continue; // již je zpracovaný v nějaké skupině - díky tomu, že jde o ekvivalence víme, že ve skupině jsou již všechny ekvivalentní tripy

                var mergedTrip = CreateTripGroupRecursive(trips[i]);

                for (int j = i + 1; j < trips.Count; j++)
                {
                    if (removedTrips.Contains(trips[j]))
                        continue; // již je zpracovaný v nějaké skupině - díky tomu, že jde o ekvivalence víme, že ve skupině jsou již všechny ekvivalentní tripy

                    var compareResult = comparer.Equals(mergedTrip, trips[j]);
                    if (compareResult == TripEqualityResult.AreEqualAndDisjunct)
                    {
                        Merge(mergedTrip, trips[j]);
                        removedTrips.Add(trips[j]);
                    }
                    else if (compareResult != TripEqualityResult.AreDifferent)
                    {
                        mergeLog.LogComment(ToLoggedTrip(mergedTrip), ToLoggedTrip(trips[j]), compareResult); // zalogovat nestandardní stav
                        if (compareResult == TripEqualityResult.AreTotallySame)
                        {
                            // pokud jsou úplně stejné, tak ten druhý "zrušíme" též zamergováním
                            Merge(mergedTrip, trips[j]);
                            removedTrips.Add(trips[j]);
                        }
                    }
                }

                yield return mergedTrip;
            }
        }

        // převede celý block tripů na MergedTripGroupy
        private MergedTripGroup CreateTripGroupRecursive(Trip trip, MergedTripGroup prevInBlock = null)
        {
            if (trip.PreviousPublicTripInBlock != null && prevInBlock == null)
            {
                throw new ArgumentException("Prev trip in block is not null, however merged trip in block is null.");
            }

            var result = new MergedTripGroup(trip);
            result.PreviousPublicTripInBlock = prevInBlock;
            if (trip.NextPublicTripInBlock != null)
            {
                result.NextPublicTripInBlock = CreateTripGroupRecursive(trip.NextPublicTripInBlock, result);
            }

            return result;
        }

        // označí trip second k zamergování do first
        private void Merge(MergedTripGroup first, Trip second)
        {
            // uložíme si verzi před mergem pro logování
            var firstLogged = ToLoggedTrip(first);

            first.AllTrips.Add(second);
            mergeLog.LogMerged(ToLoggedTrip(first), firstLogged, ToLoggedTrip(second)); // musí být před tím, než druhému tripu zclearujeme ServiceRecords, pak už nejde zjistit původní service bitmap druhého spoje

            // pokud spoje pokračují v blocku (už máme ověřeno, že celý blok je shodný), mergujeme dál
            if (first.NextPublicTripInBlock != null)
            {
                Merge(first.NextPublicTripInBlock, second.NextPublicTripInBlock);
            }
        }

        private static LoggedTrip ToLoggedTrip(MergedTripGroup trip)
        {
            return new LoggedTrip()
            {
                Calendar = trip.ServiceAsBits.ToString(),
                RouteId = trip.Route.LineNumber,
                TripIds = trip.TripIds.ToList(),
            };
        }

        private static LoggedTrip ToLoggedTrip(Trip trip)
        {
            return new LoggedTrip()
            {
                Calendar = trip.ServiceAsBits.ToString(),
                RouteId = trip.Route.LineNumber,
                TripIds = new List<int>() { trip.TripId },
            };
        }
    }
}
