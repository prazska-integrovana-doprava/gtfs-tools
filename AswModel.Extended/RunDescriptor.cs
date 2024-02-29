using CommonLibrary;
using System.Collections.Generic;

namespace AswModel.Extended
{
    /// <summary>
    /// Reprezentuje oběh jednoho vozidla (pořadí linky)
    /// </summary>
    public class RunDescriptor
    {
        /// <summary>
        /// Čas, kdy už nezačínají noční, ale začínají denní oběhy. Pokud by nějaký denní oběh začínal před třetí, je to divné, stejně jako kdyby nějaký noční oběh začínal po třetí
        /// </summary>
        public static readonly Time StartOfRunFromPreviousDayUntil = new Time(3, 0, 0);
                                                                                            
        /// <summary>
        /// Kmenová linka
        /// </summary>
        public int RootLineNumber { get; set; }

        /// <summary>
        /// Číslo pořadí (unikátní v rámci kmenové linky)
        /// </summary>
        public int RunNumber { get; set; }

        /// <summary>
        /// Kdy oběh platí
        /// </summary>
        public ServiceDaysBitmap ServiceAsBits { get; set; }
        
        /// <summary>
        /// Spoje seřazené dle posloupnosti v rámci pořadí (resp. jejich IDčka)
        /// </summary>
        internal List<int> TripIds { get; set; }

        /// <summary>
        /// Seznam přejezdů v rámci oběhu (každý přejezd je sekvence spojů bez přestupu)
        /// </summary>
        internal List<List<int>> ConnectedTrips { get; set; }

        /// <summary>
        /// Spoje seřazené dle posloupnosti v rámci pořadí (reference varianta pole <see cref="TripIds"/>)
        /// </summary>
        public IList<Trip> Trips { get; private set; }

        /// <summary>
        /// Odkazy na spoje v oběhu. Nemusí být záznam pro každé trip ID (některé spoje jsou neveřejné, ty se nenačítají).
        /// </summary>
        /// <param name="tripIdResolver">Pro překlad z trip ID na reálný trip</param>
        internal void ResolveTrips(IDictionary<int, Trip> tripIdResolver)
        {
            Trips = new List<Trip>();
            foreach (var tripId in TripIds)
            {
                Trip trip;
                if (tripIdResolver.TryGetValue(tripId, out trip))
                {
                    Trips.Add(trip);
                }
                // opačný případ nevadí, při načítání mohl být spoj z důvodu chyby ignorován
            }
        }

        public override string ToString()
        {
            return $"{RootLineNumber}/{RunNumber}";
        }
    }
}
