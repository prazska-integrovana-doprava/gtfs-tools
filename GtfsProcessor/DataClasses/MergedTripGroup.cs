using AswModel.Extended;
using CommonLibrary;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor.DataClasses
{
    /// <summary>
    /// Skupinka spojů (1 až N), které byly sloučeny do jednoho (jsou tedy v zásadě identické a liší se jen kalendáři).
    /// Tyto multitripy vzniknou v <see cref="TripMergeOperation"/> a používají se v různých operacích, až se z nich
    /// nakonec vyrobí GTFS spoj <see cref="GtfsModel.Extended.Trip"/> v <see cref="TripsTransformation"/>.
    /// </summary>
    class MergedTripGroup
    {
        /// <summary>
        /// Všechny (identické) spoje, vždy alespoň jeden v množině je.
        /// </summary>
        public IList<Trip> AllTrips { get; private set; }

        /// <summary>
        /// Jeden spoj je zvolen jako "hlavní". Vždy existuje.
        /// </summary>
        public Trip ReferenceTrip { get { return AllTrips.First(); } }
        
        /// <summary>
        /// ID spoje z exportu, který měl tento kalendář. Jsou unikátní pouze v rámci svých souborů (viz <see cref="Trip.TripId"/>)
        /// </summary>
        public int[] TripIds { get { return AllTrips.Select(t => t.TripId).ToArray(); } }

        /// <summary>
        /// Kalendář jízd z ASW (jedničky/nuly pro každý den exportu)
        /// </summary>
        public ServiceDaysBitmap ServiceAsBits { get { return ServiceDaysBitmap.Union(AllTrips.Select(t => t.ServiceAsBits)); } }

        /// <summary>
        /// Linka
        /// </summary>
        public Route Route
        {
            get
            {
                return ReferenceTrip.Route;
            }
            set
            {
                foreach (var trip in AllTrips)
                    trip.Route = value;
            }
        }

        /// <summary>
        /// Směr (odvozeno z ASW JŘ)
        /// </summary>
        public int DirectionId
        {
            get
            {
                return ReferenceTrip.DirectionId;
            }
            set
            {
                foreach (var trip in AllTrips)
                    trip.DirectionId = value;
            }
        }

        /// <summary>
        /// Dopravce na lince + licenční číslo
        /// </summary>
        public AswAgency Agency
        {
            get
            {
                return ReferenceTrip.Agency;
            }
            set
            {
                foreach (var trip in AllTrips)
                    trip.Agency = value;
            }
        }
                
        /// <summary>
        /// Odkaz na spoj, ze kterého tento spoj přímo přejíždí nebo null, pokud tento spoj není přejezdem.
        /// </summary>
        public MergedTripGroup PreviousPublicTripInBlock { get; set; }

        /// <summary>
        /// Odkaz na spoj, na který tento spoj přímo přejíždí nebo null, pokud nepřejíždí.
        /// </summary>
        public MergedTripGroup NextPublicTripInBlock { get; set; }

        /// <summary>
        /// První spoj v blocku. Nemůže být null, může však jít o tento spoj (this).
        /// </summary>
        public MergedTripGroup FirstTripInBlock
        {
            get
            {
                var iterator = this;
                while (iterator.PreviousPublicTripInBlock != null)
                    iterator = iterator.PreviousPublicTripInBlock;
                return iterator;
            }
        }
        
        /// <summary>
        /// Typ výkonu dle ASW JŘ (tady by se nemělo stát, že se to ve skupině liší a kdyby, tak se toho tolik nestane, vezmem něco prostě)
        /// </summary>
        public TripOperationType TripType { get { return AllTrips.Min(t => t.TripType); } set { foreach (var t in AllTrips) t.TripType = value; } }

        /// <summary>
        /// Zda jde o nestandardní trip (výjezd / zátah / přejezd), který nemá být uveden v seznamu linek v zastávce
        /// </summary>
        public bool IsExceptional { get; set; }

        /// <summary>
        /// Bezbariérová přístupnost spoje.
        /// </summary>
        public bool IsWheelchairAccessible { get { return ReferenceTrip.IsWheelchairAccessible; } }
        
        /// <summary>
        /// Druh dopravy dle ASW JŘ přeložený do číselníku GTFS.
        /// Ano, druh dopravy je v ASW JŘ uveden u každého spoje, po načtení spojů se to musí agregovat přes všechny spoje k lince.
        /// </summary>
        public AswTrafficType TrafficType { get { return ReferenceTrip.TrafficType; } }

        /// <summary>
        /// Typ linky pro daný spoj (vychází z typu linky, může být upraven podle charakteru spoje)
        /// </summary>
        public AswLineType LineType { get; set; }
        
        /// <summary>
        /// Číslo závodu.
        /// </summary>
        public int CompanyId { get { return ReferenceTrip.CompanyId; } }

        /// <summary>
        /// Všechna zastavení včetně neveřejných, ale očištěná o neveřejné + smyčky na začátku/konci spoje.
        /// </summary>
        public StopTime[] StopTimes { get; set; }

        /// <summary>
        /// Všechna zastavení ve veřejných zastávkách
        /// </summary>
        public IEnumerable<StopTime> PublicStopTimes 
        {
            get
            {
                return StopTimes.Where(st => st.IsPublic); 
            } 
        }
        

        public MergedTripGroup(Trip referenceTrip)
        {
            AllTrips = new List<Trip>() { referenceTrip };
            LineType = ReferenceTrip.LineType;
            StopTimes = ReferenceTrip.StopTimesPublicPart;
            IsExceptional = referenceTrip.TripType != TripOperationType.Regular;
        }
                
        public override string ToString()
        {
            return $"{Route}|{string.Join(";", TripIds)}";
        }

        public string ToStringWithCalendar()
        {
            return $"{ToString()} cal {ServiceAsBits}";
        }
    }
}
