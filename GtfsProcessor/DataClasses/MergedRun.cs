using AswModel.Extended;
using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor.DataClasses
{
    /// <summary>
    /// Data o oběhu, vzniklá z <see cref="RunDescriptor"/> (případně sloučením více shodných).
    /// </summary>
    class MergedRun
    {
        /// <summary>
        /// Informace o spoji v oběhu (jeden <see cref="MergedTripGroup"/> a přidružené info).
        /// Přidružené informace nemůžou být součástí <see cref="MergedTripGroup"/>, protože skupina mohla vzniknout sloučením spojů různých čísel spojů i dopravců
        /// </summary>
        public class TripIdentification
        {
            /// <summary>
            /// Data spoje (vzniklá sloučením potenciálně více shodných spojů)
            /// </summary>
            public MergedTripGroup TripData { get; set; }

            /// <summary>
            /// Licenční číslo linky v tomto oběhu
            /// </summary>
            public int RouteLicenceNumber { get; set; }

            /// <summary>
            /// Číslo spoje v tomto oběhu
            /// </summary>
            public int TripNumber { get; set; }

            public override bool Equals(object obj)
            {
                var other = obj as TripIdentification;
                if (other == null)
                {
                    return false;
                }

                return TripData.Equals(other.TripData) && RouteLicenceNumber.Equals(other.RouteLicenceNumber) && TripNumber.Equals(other.TripNumber);
            }

            public override int GetHashCode()
            {
                return TripData.GetHashCode() + RouteLicenceNumber.GetHashCode() * 171 + TripNumber.GetHashCode() * 86921;
            }
        }

        /// <summary>
        /// Kmenová linka
        /// </summary>
        public int RootLineNumber { get; private set; }

        /// <summary>
        /// Číslo pořadí
        /// </summary>
        public int RunNumber { get; private set; }

        /// <summary>
        /// Kalendář pro oběh, kdy platí
        /// </summary>
        public ServiceDaysBitmap ServiceBitmap { get; private set; }

        /// <summary>
        /// Spoje na oběhu a jejich čísla ROPID (pokud byly očíslovány, jinak 0)
        /// </summary>
        public TripIdentification[] TripsAndNumbers { get; private set; }

        public static MergedRun Construct(RunDescriptor run, IDictionary<Trip, MergedTripGroup> tripsToGroups)
        {
            return new MergedRun()
            {
                RootLineNumber = run.RootLineNumber,
                RunNumber = run.RunNumber,
                ServiceBitmap = run.ServiceAsBits,
                TripsAndNumbers = TransformTrips(run.Trips, tripsToGroups).ToArray(),
            };
        }

        /// <summary>
        /// Vrací true, pokud je <paramref name="run"/> obsahově shodný s touto instancí (tzn. jde o stejný oběh ze stejných spojů a liší se jen kalendáři).
        /// </summary>
        /// <param name="run">Druhý oběh</param>
        /// <param name="tripsToGroups">Mapování ASW spojů na sloučené spoje</param>
        public bool IsEqual(RunDescriptor run, IDictionary<Trip, MergedTripGroup> tripsToGroups)
        {
            return RootLineNumber == run.RootLineNumber && RunNumber == run.RunNumber && Enumerable.SequenceEqual(TripsAndNumbers, TransformTrips(run.Trips, tripsToGroups));
        }

        /// <summary>
        /// Sloučí daný oběh s tímto. Předpokládá se, že jde o totožný oběh a liší se jen kalendáři.
        /// </summary>
        /// <param name="run">Druhý oběh</param>
        public void Merge(RunDescriptor run)
        {
            ServiceBitmap = ServiceBitmap.Union(run.ServiceAsBits);
        }

        /// <summary>
        /// Převede ASW spoje (<see cref="Trip"/>) na sloučené spoje (<see cref="MergedTripGroup"/>).
        /// Spoje, které nemají odraz v kolekci sloučených spojů jsou přeskočeny (typicky půjde o neveřejné spoje)
        /// </summary>
        /// <param name="trips">Spoje</param>
        /// <param name="tripsToGroups">Mapování ASW spojů na sloučené spoje</param>
        /// <returns>Kolekce odpovídající zadaným <paramref name="trips"/>. Může být shodně velká, ale i kratší, pokud oběh obsahuje spoje, které nejdou do výstupu (např. neveřejné)</returns>
        public static IEnumerable<TripIdentification> TransformTrips(IEnumerable<Trip> trips, IDictionary<Trip, MergedTripGroup> tripsToGroups)
        {
            foreach (var trip in trips)
            {
                var tripGroup = tripsToGroups.GetValueOrDefault(trip);

                // tripGroup bude null, pokud spoj nebyl nalezen v mapě tripsToGroups, což znamená, že nebyl mezi zpracovanými spoji při mergi (typickým důvodem bude, že jde o neveřejný spoj);
                // v takovém případě tedy spoj ignorujeme
                if (tripGroup != null)
                {
                    yield return new TripIdentification()
                    {
                        TripData = tripGroup,
                        RouteLicenceNumber = trip.RouteLicenceNumber,
                        TripNumber = trip.TripNumber % 1000 != 0 ? trip.TripNumber : 0,
                    };
                }
            }
        }

        public override string ToString()
        {
            return $"{RootLineNumber}/{RunNumber}";
        }
    }
}
