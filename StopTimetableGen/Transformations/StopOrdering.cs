using CommonLibrary;
using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.Transformations
{
    /// <summary>
    /// Zajišťuje uspořádání zastávek linky podle jednotlivých spojů.
    /// Podmínkou je, že jsou trasy topologicky uspořádatelné, tj. neexistují cykly (např. není jeden spoj jedoucí A>B>C>D a jiný A>C>B>D).
    /// Další podmínkou je, že žádný spoj nestaví na trase dvakrát u stejného sloupku (např. 423 Škvorec, nám.)
    /// </summary>
    class StopOrdering
    {
        private Dictionary<Stop, HashSet<Stop>> edges;
        private List<Trip> trips;
        private HashSet<Stop> openedStops;
        private Dictionary<Stop, int> order;
        private int currentOrder = 1;
        private string[] ignoredStops;

        public StopOrdering(IEnumerable<Trip> tripList, string[] ignoredStops)
        {
            // příprava grafu pro topologické uspořádání:
            // uzly = zastávky
            // hrany (orientované) vždy když spoj jede z A->B, přidá se hrana B->A
            // (topologické uspořádání pracuje s obráceně orientovaným grafem)
            this.ignoredStops = ignoredStops;
            trips = tripList.ToList();
            edges = new Dictionary<Stop, HashSet<Stop>>();
            foreach (var trip in trips)
            {
                var stopTimes = trip.PublicStopTimes.Where(st => !ignoredStops.Contains(st.Stop.Name)).ToArray();
                for (int i = stopTimes.Length - 1; i > 0; i--)
                {
                    var stop = stopTimes[i].Stop;
                    var prevStop = stopTimes[i - 1].Stop;
                    if (!edges.ContainsKey(stop))
                    {
                        edges.Add(stop, new HashSet<Stop>());
                    }

                    edges[stop].Add(prevStop);
                }
            }

            openedStops = new HashSet<Stop>();
            order = new Dictionary<Stop, int>();
        }

        /// <summary>
        /// Vytvoří seznam zastávek společný pro všechny spoje
        /// </summary>
        public IEnumerable<Stop> ExtractStopsAndOrderTopologically()
        {
            // TODO ve skutečnosti to nemusí být roots. Například Praha-Libeň na S49 není root, protože na ni ukazuje Praha-Hostivař, ale existují spoje,
            // které tam končí, takže na seznamu bude - nicméně nemělo by to vadit, cílem je projít celý graf a nemělo by záležet na pořadí
            //
            // jinými slovy tato množina je nadmnožinou skutečných kořenů
            var roots = trips.Select(trip => trip.PublicStopTimes.Where(st => !ignoredStops.Contains(st.Stop.Name)).Last().Stop).Distinct();
            foreach (var stop in roots)
            {
                if (!order.ContainsKey(stop))
                {
                    DoTopologicalOrdering(stop);
                }
            }

            return order.OrderBy(s => s.Value).Select(s => s.Key);
        }

        // prohledávání do hloubky = topologické uspořádání
        private void DoTopologicalOrdering(Stop stop)
        {
            openedStops.Add(stop);
            foreach (var prevStop in edges.GetValueOrDefault(stop, new HashSet<Stop>()))
            {
                if (order.ContainsKey(prevStop))
                    continue;

                if (openedStops.Contains(prevStop))
                {
                    // bohužel úplně neumíme určit, který spoj za to "může", protože si nevedeme záznam, od kterého spoje pochází která hrana
                    throw new InvalidOperationException("V pořadí zastávek existuje cyklus. To znamená, že existují dva spoje, které některou dvojici zastávek projíždí v opačném pořadí. Příčinou může být, že vlak v sudém směru má liché číslo nebo naopak.");
                }

                DoTopologicalOrdering(prevStop);
            }

            order[stop] = currentOrder++;
        }
    }
}
