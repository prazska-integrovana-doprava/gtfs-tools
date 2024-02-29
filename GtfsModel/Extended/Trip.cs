using CommonLibrary;
using GtfsModel.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Jeden spoj - záznam v trips.txt (rozšíření <see cref="GtfsTrip"/>).
    /// </summary>
    public class Trip
    {
        /// <summary>
        /// ID spoje v GTFS.
        /// </summary>
        public string GtfsId { get; set; }

        /// <summary>
        /// Linka
        /// </summary>
        public Route Route { get; set; }

        /// <summary>
        /// Kalendář jízd (které dny je spoj vypraven).
        /// </summary>
        public CalendarRecord CalendarRecord { get; set; }

        /// <summary>
        /// Cílová zastávka.
        /// </summary>
        public string Headsign { get; set; }

        /// <summary>
        /// Vrací cílovou orientaci, jakou má spoj na začátku trasy. Pokud není na cestě žádná změna orientace, odpovídá
        /// položce <see cref="Headsign"/>, která je vždy "poslední cílovkou". Pokud je na cestě změna orientace,
        /// odpovídá názvu první zastávky se změnou orientace.
        /// </summary>
        public string HeadsignAtFirstStop
        {
            get
            {
                return StopTimes.First().StopHeadsign ?? Headsign;
            }
        }

        /// <summary>
        /// Směr (odvozeno z ASW JŘ)
        /// </summary>
        public Direction DirectionId { get; set; }

        /// <summary>
        /// Název/číslo spoje (pro vlaky)
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// ID bloku spojů (všechny spoje ve stejném bloku jsou odjety stejným vozidlem, je tedy možné mezi nimi
        /// garantovaně "přestupovat"). Aktuálně odpovídá vždy ID prvního spoje celého bloku.
        /// </summary>
        public string BlockId { get; set; }

        /// <summary>
        /// Pokud je členem nějakého bloku, obsahuje odkaz na spoj, který dané vozidlo jede před tímto spojem.
        /// V opačném případě je null.
        /// </summary>
        public Trip PreviousTripInBlock { get; set; }

        /// <summary>
        /// Pokud je členem nějakého bloku, obsahuje odkaz na spoj, který dané vozidlo jede před tímto spojem.
        /// V opačném případě je null.
        /// </summary>
        public Trip NextTripInBlock { get; set; }

        /// <summary>
        /// První spoj v blocku. Nemůže být null, může však jít o tento spoj (this).
        /// </summary>
        public Trip FirstTripInBlock
        {
            get
            {
                var iterator = this;
                while (iterator.PreviousTripInBlock != null)
                    iterator = iterator.PreviousTripInBlock;
                return iterator;
            }
        }
        
        /// <summary>
        /// Trasa spoje na mapě.
        /// </summary>
        public Shape Shape { get; set; }

        /// <summary>
        /// Bezbariérová přístupnost spoje dle číselníku GTFS
        /// </summary>
        public WheelchairAccessibility WheelchairAccessible { get; set; }

        /// <summary>
        /// Možnost převést ve spoji kolo
        /// </summary>
        public BikeAccessibility BikesAllowed { get; set; }

        /// <summary>
        /// Nestandardní spoj (používáme pro výjezdy/zátahy tramvají a obskurní vlakové linky typu Cyklohráček)
        /// </summary>
        public bool IsExceptional { get; set; }

        /// <summary>
        /// Dopravce, který spoj skutečně zajišťuje
        /// </summary>
        public RouteSubAgency SubAgency { get; set; }

        /// <summary>
        /// Všechna zastavení v zastávkách
        /// </summary>
        public List<StopTime> StopTimes { get; set; }

        /// <summary>
        /// Zastavení v zastávkách, která jsou veřejná (je umožněn alespoň nástup nebo výstup)
        /// </summary>
        public IEnumerable<StopTime> PublicStopTimes
        {
            get
            {
                return StopTimes.Where(st => st.IsPublic);
            }
        }


        public Trip()
        {
            StopTimes = new List<StopTime>();
        }

        /// <summary>
        /// Vytvoří data o spoji z GTFS záznamu
        /// </summary>
        /// <param name="gtfsTrip">GTFS záznam</param>
        public static Trip Construct(GtfsTrip gtfsTrip, IDictionary<string, CalendarRecord> calendars, IDictionary<string, Route> routes, IDictionary<string, Shape> shapes)
        {
            return new Trip()
            {
                BikesAllowed = gtfsTrip.BikesAllowed,
                BlockId = gtfsTrip.BlockId,
                CalendarRecord = calendars[gtfsTrip.ServiceId],
                IsExceptional = gtfsTrip.IsExceptional != 0,
                Headsign = gtfsTrip.Headsign,
                GtfsId = gtfsTrip.Id,
                DirectionId = gtfsTrip.DirectionId,
                Route = routes[gtfsTrip.RouteId],
                Shape = !string.IsNullOrEmpty(gtfsTrip.ShapeId) ? shapes[gtfsTrip.ShapeId] : null,
                ShortName = gtfsTrip.ShortName,
                WheelchairAccessible = gtfsTrip.WheelchairAccessible,
                SubAgency = routes[gtfsTrip.RouteId].SubAgencies.FirstOrDefault(a => a.SubAgencyId == gtfsTrip.SubAgencyId),
            };
        }

        /// <summary>
        /// Vytvoří GTFS záznam spoje
        /// </summary>
        /// <returns></returns>
        public GtfsTrip ToGtfsTrip()
        {
            return new GtfsTrip()
            {
                RouteId = Route.GtfsId,
                ServiceId = CalendarRecord.GtfsId,
                Id = GtfsId,
                Headsign = Headsign,
                ShortName = ShortName,
                DirectionId = DirectionId == 0 ? Direction.Outbound : Direction.Inbound,
                BlockId = BlockId,
                ShapeId = Shape?.Id,
                WheelchairAccessible = WheelchairAccessible,
                BikesAllowed = BikesAllowed,
                IsExceptional = IsExceptional ? 1 : 0,
                SubAgencyId = SubAgency?.SubAgencyId ?? 0,
            };
        }

        /// <summary>
        /// Vrátí všechna zastavení v dané zastávce (může být prázdné, pokud spoj v zastávce nestaví, může být víceprvkové, pokud u stejného sloupku staví vícekrát).
        /// </summary>
        /// <param name="stop">Zastávka</param>
        public IEnumerable<StopTime> GetStopTimesAt(Stop stop)
        {
            return StopTimes.Where(st => st.Stop == stop);
        }

        /// <summary>
        /// Vytvoří GTFS záznamy všech zastavení
        /// </summary>
        public IEnumerable<GtfsStopTime> GetGtfsStopTimes()
        {
            int i = 1;
            foreach (var stopTime in StopTimes)
            {
                stopTime.SequenceNumber = i++;
                yield return stopTime.ToGtfsStopTime();
            }
        }

        public override string ToString()
        {
            return $"{GtfsId} ({StopTimes.First().Stop.Name} {StopTimes.First().DepartureTime} - {StopTimes.Last().Stop.Name} {StopTimes.Last().ArrivalTime})";
        }
    }
}
