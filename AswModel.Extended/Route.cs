using CommonLibrary;
using GtfsLogging;
using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Laura a její tygři.
    /// Tedy, linka a její spoje.
    /// 
    /// Respektive jedna její verze platnosti (viz <see cref="VersionedItemByBitmap{T}"/>)
    /// </summary>
    public class Route : IVersionableByBitmap
    {
        public const int LineANumber = 991;
        public const int LineBNumber = 992;
        public const int LineCNumber = 993;

        /// <summary>
        /// Číslo linky dle ASW JŘ.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Platnost záznamu linky
        /// </summary>
        public ServiceDaysBitmap ServiceAsBits { get; set; }

        /// <summary>
        /// Začátek platnosti záznamu
        /// </summary>
        public RelativeDate ValidityStartDate { get { return ServiceAsBits.GetFirstDayOfService(); } }
        
        /// <summary>
        /// Alias linky (většinou shodné s číslem)
        /// </summary>
        public string LineName { get; set; }

        /// <summary>
        /// Trasa linky
        /// </summary>
        public string RouteDescription { get; set; }

        /// <summary>
        /// Druh dopravy dle číselníku ASW.
        /// </summary>
        public AswTrafficType TrafficType { get; private set; }

        /// <summary>
        /// Kategorie linky pro IDOS
        /// </summary>
        public IdosRouteCategory IdosRouteCategory { get; set; }

        /// <summary>
        /// Dopravci operující na lince
        /// </summary>
        public List<RouteAgency> RouteAgencies { get; private set; }

        /// <summary>
        /// Spoje linky
        /// </summary>
        public List<Trip> Trips { get; private set; }

        /// <summary>
        /// Pouze veřejné spoje linky
        /// </summary>
        public IEnumerable<Trip> PublicTrips { get { return Trips.Where(t => t.IsPublic); } }

        /// <summary>
        /// Vrátí všechny tripy, které jsou první v bloku - hodí se při zpracovávání tripů po blocích
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Trip> GetAllPublicTripsFirstInBlock()
        {
            return PublicTrips.Where(trip => trip.PreviousPublicTripInBlock == null);
        }

        /// <summary>
        /// Indikátor, zda jde o noční linku
        /// </summary>
        public bool IsNight { get; set; }

        /// <summary>
        /// Typ linky - městská / příměstská atd. Pokud možno, používat <see cref="Trip.LineType"/>, který může ještě podle charakteru spoje tuto hodnotu pro konkrétní spoj upravit.
        /// </summary>
        public AswLineType LineType { get; set; }
        

        public Route()
        {
            TrafficType = AswTrafficType.Undefined;
            RouteAgencies = new List<RouteAgency>();
            Trips = new List<Trip>();
        }

        public void ExtendServiceDays(ServiceDaysBitmap bitmapToAdd)
        {
            ServiceAsBits = ServiceAsBits.Union(bitmapToAdd);
        }

        /// <summary>
        /// Nastaví traffic type podle spojů. V modelu ASW totiž nejde o vlastnost linky, ale spojů. Předpokládá se však, že všechny spoje jedné linky jsou ze stejné trakce.
        /// Nnedělá se v LineProcessoru, protože v té době ještě nejsou načtené tripy; musí se dělat až úplně nakonec.
        /// </summary>
        /// <param name="log">Log, kam se mají hlásit chyby v případě, že linka obsahuje spoje různého druhu.</param>
        public void InferTrafficTypeFromTrips(ICommonLogger log)
        {
            if (PublicTrips.Any())
            {
                TrafficType = PublicTrips.First().TrafficType;

                foreach (var trip in PublicTrips)
                {
                    if (TrafficType != trip.TrafficType)
                    {
                        log.Log(LogMessageType.WARNING_ROUTE_CONFLICTED_TRIP_TYPES, $"TrafficTypeSetterOperation: Spoj {trip} má jiný typ ({trip.TrafficType}) než jeho mateřská linka {this} ({TrafficType}).");
                    }
                }
            }
        }

        public override string ToString()
        {            
            return $"{LineName}";
        }
    }
}
