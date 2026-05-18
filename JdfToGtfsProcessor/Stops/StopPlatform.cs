using GtfsModel.Extended;
using System.Globalization;
using System.Text;

namespace JdfToGtfsProcessor.Stops
{
    /// <summary>
    /// Reprezentuje jedno nástupiště (CIS číslo + nástupiště). Může obsahovat více GTFS zastávek, pokud různé linky mají různé zóny
    /// </summary>
    internal class StopPlatform
    {
        public GtfsStopFactory stopFactory { get; private set; }

        /// <summary>
        /// Všechny GTFS zastávky pro nástupiště (musí se lišit zónou).
        /// Je prázdné, dokud zastávka není použita nějakým zastavením.
        /// Pokud obsahuje jeden prvek, skládá se GTFS ID z CIS čísla a nástupiště.
        /// Pokud obsahuje více než jeden prvek, skládá se GTFS ID z CIS čísla, nástupiště a zóny
        /// </summary>
        public List<Stop> StopsAndZones { get; private set; }

        public StopPlatform(GtfsStopFactory stopFactory) 
        {
            this.stopFactory = stopFactory;
            StopsAndZones = new List<Stop>();
        }

        /// <summary>
        /// Zajistí verzi zastávky pro danou zónu (použije existující záznam nebo vytvoří nový)
        /// </summary>
        /// <param name="zone">Zóna</param>
        public Stop GetStopForZone(string zone)
        {
            foreach (var gtfsStop in StopsAndZones)
            {
                if (gtfsStop.ZoneId == zone)
                {
                    return gtfsStop;
                }
            }

            var newStop = stopFactory.CreateStop();
            newStop.ZoneId = zone;
            if (StopsAndZones.Count > 0)
            {
                AdjustGtfsIdToZone(newStop);
            }

            if (StopsAndZones.Count == 1)
            {
                AdjustGtfsIdToZone(StopsAndZones[0]);
            }

            StopsAndZones.Add(newStop);
            return newStop;
        }

        /// <summary>
        /// Doplní do GTFS ID název zóny
        /// </summary>
        /// <param name="stop">GTFS zastávka</param>
        private void AdjustGtfsIdToZone(Stop stop)
        {
            stop.GtfsId += "_Z" + stop.ZoneId.Replace(',', '+').Replace(' ', '-');
        }
    }
}
