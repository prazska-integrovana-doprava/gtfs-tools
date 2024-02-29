using CommonLibrary;
using GtfsLogging;
using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended
{
    public class TripDatabase
    {
        // popisy oběhů - oběhy si celou dobu pamatují pouze XML IDčka spojů, která se vyhodnotí až na konci
        // díky tomu není potřeba u oběhů hlídat, jestli byly nějaké spoje smazány, zmergovány, nebo tak
        public IList<RunDescriptor> TripRuns { get; private set; }
        
        // seznam všech spojů v databázi
        private IList<Trip> allTrips { get; set; }

        /// <summary>
        /// Počet spojů načtených v databázi
        /// </summary>
        public int TripCount { get { return allTrips.Count; } }
        
        public TripDatabase()
        {
            TripRuns = new List<RunDescriptor>();
            allTrips = new List<Trip>();
        }

        /// <summary>
        /// Přidá záznam o oběhu
        /// </summary>
        /// <param name="tripRun">Oběh</param>
        public void AddTripSequence(RunDescriptor tripRun)
        {
            TripRuns.Add(tripRun);
        }

        /// <summary>
        /// Přidá spoj do databáze spojů a přidá jej i k seznamu spojů linky <see cref="Route.Trips"/>.
        /// </summary>
        /// <param name="trip">Spoj</param>
        public void AddTrip(Trip trip)
        {
            allTrips.Add(trip);
            trip.Route.Trips.Add(trip);
        }

        /// <summary>
        /// Vrátí všechny spoje
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Trip> GetAllTrips()
        {
            return allTrips;
        }

        /// <summary>
        /// Vrátí všechny veřejné spoje
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Trip> GetAllPublicTrips()
        {
            return allTrips.Where(t => t.IsPublic);
        }

        /// <summary>
        /// Doplní tripům vlastnosti <see cref="Trip.NextTripInBlock"/> a <see cref="Trip.PreviousTripInBlock"/> podle hodnoty <see cref="Trip.NextTripId"/>
        /// a resolvuje pořadím seznamy spojů <see cref="RunDescriptor.Trips"/>.
        /// </summary>
        /// <param name="log">Pro logování chyb</param>
        public void ResolveBlocksAndRuns(ICommonLogger log)
        {
            var xmlIdToTripMap = allTrips.ToDictionary(t => t.TripId, t => t);

            // nejprve ještě doodvodit následné spoje v přejezdech tramvají
            Trip prevTrip = null;
            foreach (var trip in GetAllPublicTrips())
            {
                if (prevTrip != null)
                {
                    InferBlock(prevTrip, trip);
                }

                prevTrip = trip;
            }

            foreach (var run in TripRuns)
            {
                run.ResolveTrips(xmlIdToTripMap);

                foreach (var connectedTrips in run.ConnectedTrips)
                {
                    Trip trip = null;
                    foreach (var connectedTripId in connectedTrips)
                    {
                        var nextTrip = xmlIdToTripMap.GetValueOrDefault(connectedTripId);
                        if (nextTrip == null || !nextTrip.IsPublic)
                        {
                            continue;
                        }

                        nextTrip = nextTrip.SplitByCalendarMask(run.ServiceAsBits);

                        if (trip != null)
                        {
                            if (nextTrip.PreviousTripInBlock != null)
                            {
                                //if (nextTrip.PreviousTripInBlock == trip && trip.NextTripInBlock == nextTrip)
                                //{
                                //    // už byly propojeny pravděpodobně v rámci inference bloků
                                //    trip = nextTrip;
                                //    continue;
                                //}

                                log.Log(LogMessageType.ERROR_TRIP_CONFLICTED_NEXT_TRIP, $"Spoj {trip} přejíždí na spoj {nextTrip}, avšak na ten již přejíždí {nextTrip.PreviousTripInBlock}.");
                                continue;
                            }

                            if (nextTrip.PublicStopTimes.First().ArrivalTime < trip.PublicStopTimes.Last().DepartureTime)
                            {
                                // následný spoj má dřívější příjezd než předchozí spoj odjezd
                                if (nextTrip.PublicStopTimes.First().DepartureTime < trip.PublicStopTimes.Last().DepartureTime)
                                {
                                    // on má dřívější i odjezd, tak to už je vážně nesmysl, mažu vazbu
                                    log.Log(LogMessageType.ERROR_TRIP_NEXT_TRIP_TOO_SOON_DEPARTURE, $"Spoj {trip} odjíždí z poslední zastávky v {trip.PublicStopTimes.Last().DepartureTime}, zatímco následný spoj {nextTrip} odjíždí z první zastávky v {nextTrip.PublicStopTimes.First().DepartureTime}, ignoruji blokovou vazbu.");
                                    continue;
                                }
                                else
                                {
                                    // jen korekce ArrivalTime
                                    log.Log(LogMessageType.WARNING_TRIP_NEXT_TRIP_TOO_SOON_ARRIVAL, $"Spoj {trip} odjíždí z poslední zastávky v {trip.PublicStopTimes.Last().DepartureTime}, zatímco následný spoj {nextTrip} přijíždí na první zastávku v {nextTrip.PublicStopTimes.First().ArrivalTime}, provádím korekci příjezdu.");
                                    nextTrip.PublicStopTimes.First().ArrivalTime = trip.PublicStopTimes.Last().DepartureTime;
                                }
                            }

                            // zapojit do spojového seznamu (na začátek)
                            nextTrip.PreviousTripInBlock = trip;
                            trip.NextTripInBlock = nextTrip;
                        }

                        trip = nextTrip;
                    }

                }
            }

        }

        /// <summary>
        /// Pokusí se zjistit, jestli dané dva spoje na stejné lince jsou přejezd (je potřeba kvůli tramvajím, kde
        /// někdy nejsou explicitně zadány přejezdy - týká se hlavně nájezdů na linky (*3 -> 3 apod.)
        /// 
        /// K tomu, aby byly spoje považovány za stejný blok, musí být splněny následující podmínky
        /// - jde o spoje tramvají (u metra a busů to spojuje i věci, které by úplně spojeny být neměly)
        /// - spoje mají stejné číslo oběhu
        /// - spoje mají stejný kalendář jízd
        /// - poslední zastávka prvního spoje je shodná s první zastávkou druhého spoje (bez smyček)
        /// - rozdíl času odjezdu a času příjezdu je maximálně 5 minut
        /// </summary>
        /// <param name="previousTrip">První spoj</param>
        /// <param name="currentTrip">Druhý spoj</param>
        /// <returns>True, pokud jde o přejezd, jinak false</returns>
        private bool InferBlock(Trip previousTrip, Trip currentTrip)
        {
            // u ostatních trakcí než tramvají to dělá divné věci (spojuje spoje, které spojeny být nemají)
            if (currentTrip.TrafficType != AswTrafficType.Tram)
            {
                return false;
            }

            if (previousTrip.Route != currentTrip.Route)
            {
                return false;
            }

            if (previousTrip.RootLineNumber != currentTrip.RootLineNumber || previousTrip.RootRunNumber != currentTrip.RootRunNumber)
            {
                return false;
            }

            if (!previousTrip.ServiceAsBits.Equals(currentTrip.ServiceAsBits))
            {
                return false;
            }

            // Používáme StopTimesPublicPart, abychom odstranili smyčky. Jinak by podmínka níže byla splněna téměř vždy,
            // protože spoje obsahují po konečné zastávce i výchozí zastávku
            var lastStopOfPrevious = previousTrip.StopTimesPublicPart.Last();
            var firstStopOfCurrent = currentTrip.StopTimesPublicPart.First();

            if (lastStopOfPrevious.Stop != firstStopOfCurrent.Stop)
            {
                return false;
            }

            var timeDiff = firstStopOfCurrent.DepartureTime.TotalSeconds - lastStopOfPrevious.ArrivalTime.TotalSeconds;
            if (timeDiff < 0 || timeDiff >= 300)
            {
                return false;
            }

            //previousTrip.NextTripInBlock = currentTrip;
            //currentTrip.PreviousTripInBlock = previousTrip;
            var commonRuns = previousTrip.OwnerRun.Intersect(currentTrip.OwnerRun);
            if (!commonRuns.Any())
            {
                throw new System.Exception("No common runs");
            }

            foreach (var run in commonRuns)
            {
                if (run.ConnectedTrips.Any(ct => ct.Any2(t1 => t1 == previousTrip.TripId, t2 => t2 == currentTrip.TripId)))
                {
                    // vazba už je zapracována, nemusíme zanášet
                    continue;
                }

                var existingConnectedTripRecordEnds = run.ConnectedTrips.FirstOrDefault(ct => ct.Last() == previousTrip.TripId);
                var existingConnectedTripRecordBegins = run.ConnectedTrips.FirstOrDefault(ct => ct.First() == currentTrip.TripId);
                if (existingConnectedTripRecordEnds != null && existingConnectedTripRecordBegins != null)
                {
                    // propojení dvou již existujících bloků dohromady (přidáme na konec prvního a druhý zrušíme)
                    existingConnectedTripRecordEnds.AddRange(existingConnectedTripRecordBegins);
                    run.ConnectedTrips.Remove(existingConnectedTripRecordBegins);
                }
                else if (existingConnectedTripRecordEnds != null)
                {
                    // připojení na konec existujícího bloku
                    existingConnectedTripRecordEnds.Add(currentTrip.TripId);
                }
                else if (existingConnectedTripRecordBegins != null)
                {
                    // zařazení před začátek existujícího bloku
                    existingConnectedTripRecordBegins.Insert(0, previousTrip.TripId);
                }
                else
                {
                    // propojení dvou nezávislých spojů (vytvoření nového bloku)
                    run.ConnectedTrips.Add(new List<int>() { previousTrip.TripId, currentTrip.TripId });
                }
            }

            return true;
        }

    }
}
