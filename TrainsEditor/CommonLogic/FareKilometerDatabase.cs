using CsvSerializer;
using CsvSerializer.Attributes;
using CzpttModel;
using GtfsLogging;
using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrainsEditor.ExportModel;

namespace TrainsEditor.CommonLogic
{
    internal class FareKilometerDatabase
    {
        private class FareKmRowCsv
        {
            [CsvField("cisjr_from", 1)]
            public int FromStopCisId { get; set; }

            [CsvField("cisjr_to", 2)]
            public int ToStopCisId { get; set; }

            [CsvField("dist_meters", 3)]
            public int FareDistanceMeters { get; set; }
        }

        // pozor, nemusí být zadán (může být null)
        ICommonLogger log;

        // indexováno dvojicí stanic vrací kilometry
        private Dictionary<(Stop, Stop), int> fareKmDb;

        protected FareKilometerDatabase(ICommonLogger log)
        {
            this.log = log;
            fareKmDb = new Dictionary<(Stop, Stop), int>();
        }

        /// <summary>
        /// Vytvoří instanci databáze kilometrů
        /// </summary>
        /// <param name="fileName">Soubor s kilometrážemi</param>
        /// <param name="stationDb">Databáze stanic</param>
        /// <param name="log">Log pro zapisování chyb při určování km (může být null, pokud nechceme logovat)</param>
        public static FareKilometerDatabase Create(string fileName, StationDatabase stationDb, ICommonLogger log)
        {
            var fileData = CsvFileSerializer.DeserializeFile<FareKmRowCsv>(fileName);
            var result = new FareKilometerDatabase(log);

            foreach (var row in fileData)
            {
                if (row.FromStopCisId / 100000 != 54 || row.ToStopCisId / 100000 != 54)
                    continue; // není česká železniční stanice

                var locationCodeFrom = LocationIdent.CountryCodeCZ + row.FromStopCisId % 100000;
                var locationCodeTo = LocationIdent.CountryCodeCZ + row.ToStopCisId % 100000;

                if (stationDb.AllStops.TryGetValue(locationCodeFrom, out var fromStation) && stationDb.AllStops.TryGetValue(locationCodeTo, out var toStation))
                {
                    result.fareKmDb.Add((fromStation, toStation), row.FareDistanceMeters / 1000);
                }
            }

            return result;
        }

        /// <summary>
        /// Vyplní u zastavení všech spojů hodnoty do <see cref="StopTime.FareKilometerDistance"/> - pouze u stanic, které mají definovanou tarifní kilometráž
        /// (neveřejné body jako výhybny apod. přeskočí).
        /// 
        /// Zároveň píše do logu veřejná zastavení bez kilometráže.
        /// </summary>
        public void ProcessTrips(IEnumerable<Trip> trips)
        {
            foreach (var trip in trips)
            {
                AssignFareKilometersToTrip(trip);
            }
        }

        public void AssignFareKilometersToTrip(Trip trip)
        {
            var lastWithKm = trip.StopTimes.First();
            trip.StopTimes.First().FareKilometerDistance = (trip.PreviousTripInBlock?.StopTimes?.LastOrDefault()?.FareKilometerDistance).GetValueOrDefault();
            foreach (var st in trip.StopTimes.Skip(1))
            {
                if (fareKmDb.TryGetValue((lastWithKm.Stop, st.Stop), out var km))
                {
                    st.FareKilometerDistance = lastWithKm.FareKilometerDistance.Value + km;
                    lastWithKm = st;
                }
            }

            foreach (var st in trip.PublicStopTimes)
            {
                if (!st.FareKilometerDistance.HasValue)
                {
                    log?.Log(LogMessageType.WARNING_TRAIN_FARE_KM_UNDEFINED, $"Spoj {trip} nemá definovaný tarifní km pro {st}.");
                }
            }
        }
    }
}
