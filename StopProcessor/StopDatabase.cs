using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StopProcessor
{
    internal class StopDatabase : IEnumerable<StopCollectionForName>
    {
        /// <summary>
        /// Všechny zastávky indexované číslem uzlu a číslem zastávky
        /// </summary>
        public Dictionary<int, Dictionary<int, Stop>> StopsById { get; private set; }

        /// <summary>
        /// Mapa GTFS IDček na zastávky (nemusí být zastoupeny všechny zastávky, jen ty, které jsou využity v GTFS)
        /// </summary>
        public Dictionary<string, Stop> StopsByGtfsId { get; private set; }

        /// <summary>
        /// Všechny zastávky v prostém seznamu
        /// </summary>
        public IEnumerable<Stop> AllStops { get { return StopsById.Values.SelectMany(n => n.Values); } }

        /// <summary>
        /// Všechny zastávky roztříděné podle unikátních názvů (vyplněno až po volání <see cref="InitStopsByName"/>).
        /// </summary>
        private Dictionary<StopCollectionId, StopCollectionForName> stopsByName;

        public StopDatabase()
        {
            StopsById = new Dictionary<int, Dictionary<int, Stop>>();
            StopsByGtfsId = new Dictionary<string, Stop>();
        }

        /// <summary>
        /// Přidá zastávku, pokud v databázi ještě není
        /// </summary>
        /// <param name="stop">Zastávka</param>
        /// <returns>True, pokud byla přidána, false, pokud již v databázi je</returns>
        public bool AddStop(Stop stop)
        {
            if (!StopsById.ContainsKey(stop.NodeId))
            {
                StopsById.Add(stop.NodeId, new Dictionary<int, Stop>());
            }

            var nodeStops = StopsById[stop.NodeId];
            if (!nodeStops.ContainsKey(stop.StopId))
            {
                nodeStops.Add(stop.StopId, stop);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Vrátí zastávku nebo null, pokud v databázi není.
        /// </summary>
        /// <param name="nodeId">Č. uzlu</param>
        /// <param name="stopId">Č. zastávky</param>
        /// <returns>Zastávka nebo null</returns>
        public Stop FindStop(int nodeId, int stopId)
        {
            Dictionary<int, Stop> nodeStops;
            if (!StopsById.TryGetValue(nodeId, out nodeStops))
            {
                return null;
            }

            Stop stop;
            if (!nodeStops.TryGetValue(stopId, out stop))
            {
                return null;
            }

            return stop;
        }

        /// <summary>
        /// Roztřídí zastávky do <see cref="stopsByName"/>. Prozatím nerozděluje skupiny, jako UniqueName nastaví společný název. Trhání distinguishery probíhá později zvlášť.
        /// </summary>
        public void InitStopsByName()
        {
            stopsByName = new Dictionary<StopCollectionId, StopCollectionForName>();

            foreach (var stop in AllStops)
            {
                if (!stop.IsUsed)
                    continue;

                var stopCollectionId = StopCollectionId.FromStop(stop);
                if (!stopsByName.ContainsKey(stopCollectionId))
                {
                    stopsByName.Add(stopCollectionId, new StopCollectionForName(stopCollectionId));
                }

                stopsByName[stopCollectionId].Add(stop);
                
                // TODO původně tam byl ještě distinct, nebude chybět?
            }
        }

        public IEnumerator<StopCollectionForName> GetEnumerator()
        {
            return stopsByName.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
