using CommonLibrary;
using GtfsModel.Extended;
using StopTimetableGen.StopTimetableModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopTimetableGen.Transformations
{
    /// <summary>
    /// Zajišťuje přidělení poznámek spojům podle variant tras
    /// </summary>
    class TripVariantsSort
    {
        // jedna skupina spojů, která má shodnou množinu zastávek (tudíž budou mít všechny stejnou poznámku)
        private class DepartureGroup
        {
            public DepartureTrip Representative { get; set; }
            public List<DepartureTrip> AllDepartures { get; set; }

            public DepartureGroup(DepartureTrip departure)
            {
                Representative = departure;
                AllDepartures = new List<DepartureTrip>() { departure };
            }
        }

        /// <summary>
        /// Vrací ke každému odjezdu poznámku (algoritmus je postaven tak, že každý trip má nejvýš jednu).
        /// </summary>
        /// <param name="departures">Všechny průjezdy zastávkou (každý má nad sebou celý trip).</param>
        /// <param name="stops">Zastávky na lince již seřazené a agregované přes všechny spoje.</param>
        /// <returns>Přiřazení poznámek tripům (resp. jejich odjezdům)</returns>
        public IDictionary<DepartureTrip, TripVariantRemark> GetTripRemarkAssignment(IEnumerable<DepartureTrip> departures, IEnumerable<StopOnLine> stops)
        {
            // nejdříve si připravíme skupiny sdružující vždy tripy jedoucí stejnou trasou do 'groups'
            var groups = new List<DepartureGroup>();
            var stopsArray = stops.Select(s => s.Stop).ToArray();
            foreach (var departure in departures)
            {
                bool assigned = false;
                foreach (var group in groups)
                {
                    if (AreTripsEquivalent(group.Representative, departure, stopsArray))
                    {
                        group.AllDepartures.Add(departure);
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    groups.Add(new DepartureGroup(departure));
                }
            }

            // ke každé skupině vytvoříme remark a přidělíme ho jednotlivým tripům
            var remarkLetters = new List<char>() { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            var result = new Dictionary<DepartureTrip, TripVariantRemark>();
            var futureStopsArray = stops.Where(s => s.StopClassification == StopOnLine.StopClass.FutureStop).ToArray();
            foreach (var group in groups)
            {
                var remark = CreateRemark(group.Representative, futureStopsArray, stops.FirstOrDefault(s => s.StopClassification == StopOnLine.StopClass.CurrentStop));
                if (remark != null)
                {
                    var chosenLetter = remark.SetLetter(remarkLetters);
                    remarkLetters.Remove(chosenLetter);
                    foreach (var departure in group.AllDepartures)
                    {
                        result.Add(departure, remark);
                    }
                }
            }

            return result;
        }

        // porovnává, jestli oba odjezdy reprezentují spoje jedoucí stejnou trasou
        private bool AreTripsEquivalent(DepartureTrip firstTripDeparture, DepartureTrip secondTripDeparture, Stop[] stopsOnRoute)
        {
            // porovnáváme seznamy zastávek, které jsou na trase budoucí
            var firstFollowingStops = ListFollowingStops(firstTripDeparture).ToArray();
            var secondFollowingStops = ListFollowingStops(secondTripDeparture).ToArray();

            // musí se shodovat posloupnost zastávek
            // zároveň se musí shodovat headsign - NEBO stačí, když alespoň v rámci trasy dojedou oba spoje na konečnou - pak už je nám další cesta jedno
            return (firstTripDeparture.Trip.Headsign == secondTripDeparture.Trip.Headsign || firstFollowingStops.Contains(stopsOnRoute.Last()) && secondFollowingStops.Contains(stopsOnRoute.Last()))
                && Enumerable.SequenceEqual(firstFollowingStops, secondFollowingStops);
        }

        // vrátí všechna zastavení, která následují po zadaném zastavení
        private IEnumerable<Stop> ListFollowingStops(DepartureTrip departure)
        {
            return departure.StopTime.ListFollowingPublicStopTimes().Select(st => st.Stop);
        }

        private class StopAndPrev
        {
            public Stop Stop { get; set; }
            public StopOnLine LastRegular { get; set; }
            public StopOnLine NextRegular { get; set; }
        }

        // vytvoří poznámku pro spoj (který ale zastupuje celou množinu všech stejných spojů)
        // pokud jde o regulérní spoj, který zastavuje všude, vrátí null (žádná poznámka)
        private TripVariantRemark CreateRemark(DepartureTrip tripDeparture, StopOnLine[] futureStopsInTimetable, StopOnLine currentStop)
        {
            // following stops jsou zastávky následné na trase (předepsané)
            // future stops jsou zastávky spoje, kudy reálně jede
            var followingStops = ListFollowingStops(tripDeparture).ToList();
            
            // vyznačíme si zastávky, které jsou v JŘ, ale spoj je neobsluhuje => stopsNotPassed
            StopOnLine lastRegular = currentStop;
            var stopsNotPassed = new List<StopAndPrev>();
            foreach (var futureStop in futureStopsInTimetable)
            {
                if (!followingStops.Contains(futureStop.Stop))
                {
                    // spoj zastávku neobsluhuje
                    stopsNotPassed.Add(new StopAndPrev() { Stop = futureStop.Stop, LastRegular = lastRegular });
                }
                else
                {
                    lastRegular = futureStop;
                    if (stopsNotPassed.Any())
                        stopsNotPassed.Last().NextRegular = futureStop;
                }
            }

            // vyznačíme si zastávky, které nejsou v JŘ, ale spoj je obsluhuje navíc
            lastRegular = currentStop;
            var stopsPassedExtra = new List<StopAndPrev>();
            foreach (var followingStop in followingStops)
            {
                var futureStop = futureStopsInTimetable.FirstOrDefault(s => s.Stop == followingStop);
                if (futureStop == null)
                {
                    // spoj obsluhuje zastávku, která není na seznamu
                    stopsPassedExtra.Add(new StopAndPrev() { Stop = followingStop, LastRegular = lastRegular });
                }
                else
                {
                    lastRegular = futureStop;
                    if (stopsPassedExtra.Any())
                        stopsPassedExtra.Last().NextRegular = futureStop;
                }
            }

            if (!stopsNotPassed.Any() && !stopsPassedExtra.Any())
            {
                // zastavuje všude, regulérní spoj
                return null;
            }

            // potřebujeme rozlišit projeté zastávky od zastávek, kam spoj vůbec nedojede (skončí dřív)
            // děláme to ale jen v případě, že spoj jede do té správné konečné
            Stop lastStop = null;
            bool isLastStopOnRoute = futureStopsInTimetable.Any(s => s.Stop == followingStops.Last());
            if (isLastStopOnRoute)
            {
                // začneme od konce a postupně budeme posouvat konečnou a odebírat neprojeté zastávky a uvidíme, kam se dostaneme

                foreach (var stop in futureStopsInTimetable.Reverse())
                {
                    var stopNotPassedRecord = stopsNotPassed.FirstOrDefault(s => s.Stop == stop.Stop);
                    if (stopNotPassedRecord == null)
                    {
                        lastStop = stop.Stop;
                        break;
                    }
                    else
                    {
                        stopsNotPassed.Remove(stopNotPassedRecord);
                    }
                }
            }

            // výpis úseků nad rámec trasy
            // zároveň pokud v úseku spoj nějaké zastávky vynechává, tak už je nebudeme vypisovat znovu
            var remarkText = new StringBuilder();
            var stopsInvolved = new List<Stop>(stopsNotPassed.Select(s => s.Stop));
            var extraStopGroups = stopsPassedExtra.GroupBy(s => s.LastRegular);
            foreach (var extraStopGroupByLast in extraStopGroups)
            {
                if (remarkText.Length > 0)
                    remarkText.Append(" ");
                if (extraStopGroupByLast.Key != null)
                {
                    foreach (var extraStopGroupByNext in extraStopGroupByLast.GroupBy(s => s.NextRegular))
                    {
                        if (extraStopGroupByNext.Key != null)
                        {
                            stopsInvolved.Add(extraStopGroupByLast.Key.Stop);
                            if (extraStopGroupByNext.Any2())
                            {
                                var stopListAsText = StringHelper.JoinWithCommaAndAnd(extraStopGroupByNext.Select(s => s.Stop.Name));
                                remarkText.Append($"Ze stanice {extraStopGroupByLast.Key.Name} jede přes stanice {stopListAsText} do stanice {extraStopGroupByNext.Key.Name}");
                            }
                            else
                            {
                                remarkText.Append($"Ze stanice {extraStopGroupByLast.Key.Name} jede přes stanici {extraStopGroupByNext.Single().Stop.Name} do stanice {extraStopGroupByNext.Key.Name}");
                            }
                        }
                        else
                        {
                            // už se nevrací na trasu
                            stopsInvolved.Add(extraStopGroupByNext.Last().Stop);
                            remarkText.Append($"Ze stanice {extraStopGroupByLast.Key.Name} jede do stanice {extraStopGroupByNext.Last().Stop.Name}");
                        }
                    }
                }

                // jakmile máme v nějakém úseku zastávky navíc, pak už je nebudeme vypisovat mezi neobslouženými
                //stopsNotPassed.RemoveAll(s => s.LastRegular == extraStopGroupByLast.Key);
            }

            // zastávky co zbyly ve stopsNotPassed jsou uprostřed trasy, tedy vynechané
            if (stopsNotPassed.Count == 1)
            {
                if (remarkText.Length > 0)
                    remarkText.Append(" ");
                remarkText.Append($"Nezastavuje ve stanici {stopsNotPassed.Single().Stop.Name}.");
            }
            else if (stopsNotPassed.Count > 1)
            {
                if (remarkText.Length > 0)
                    remarkText.Append(" ");
                remarkText.Append($"Nezastavuje ve stanicích {StringHelper.JoinWithCommaAndAnd(stopsNotPassed.Select(s => s.Stop.Name))}.");
            }

            // lastStop nemůže být null, protože spoj nemůže ignorovat všechny budoucí zastávky na trase, jinak
            // by vůbec neměl být zahrnut
            if (lastStop != null && lastStop != futureStopsInTimetable.Last().Stop)
            {
                // spoj končí dříve
                if (remarkText.Length > 0)
                    remarkText.Append(" ");
                if (tripDeparture.Trip.NextTripInBlock == null)
                {
                    remarkText.Append($"Končí ve stanici {lastStop.Name}.");
                }
                else
                {
                    var nextTrip = tripDeparture.Trip.NextTripInBlock;
                    remarkText.Append($"Ze stanice {lastStop.Name} pokračuje jako linka {nextTrip.Route.ShortName} ve směru {nextTrip.Headsign}.");
                }

                stopsInvolved.Add(lastStop);
            }
            //else if (tripDeparture.Trip.NextTripInBlock != null)
            //{
            //    if (remarkText.Length > 0)
            //        remarkText.Append(" ");
            //    var nextTrip = tripDeparture.Trip.NextTripInBlock;
            //    remarkText.Append($"Dále pokračuje jako linka {nextTrip.Route.ShortName} ve směru {nextTrip.Headsign}.");
            //}

            return new TripVariantRemark() { Text = remarkText.Replace("..", ".").ToString(), StopsInvolved = stopsInvolved };
        }
    }
}
