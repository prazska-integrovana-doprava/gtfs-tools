using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Jeden spoj
    /// </summary>
    public class Trip
    {
        /// <summary>
        /// Charakter výkonu dle číselníku ASW JŘ
        /// </summary>
        public enum Character
        {
            /// <summary>
            /// Nedefinováno (vyskytuje se třeba u metra)
            /// </summary>
            Undefined = 0,

            /// <summary>
            /// Standardní spoj PID
            /// </summary>
            PID = 1,

            /// <summary>
            /// Smluvní spoj
            /// </summary>
            Contractual = 2,

            /// <summary>
            /// Náhradní doprava za metro
            /// </summary>
            SubstituteForMetro = 3,

            /// <summary>
            /// Náhradní doprava za TRAM
            /// </summary>
            SubstituteForTram = 4,

            /// <summary>
            /// Náhradní doprava za vlak
            /// </summary>
            SubstituteForTrain = 5,

            /// <summary>
            /// Náhradní doprava ostatní
            /// </summary>
            SubstituteOthers = 6,

            /// <summary>
            /// Mimo PID vnitřní
            /// </summary>
            OutsidePIDInner = 7,

            /// <summary>
            /// Mimo PID vnější
            /// </summary>
            OutsidePIDOuter = 8,
        }

        /// <summary>
        /// ID spoje z exportu, při zpracování více souborů není unikátní, protože v každém souboru jsou spoje číslovány od 1.
        /// Pokud bychom to chtěli umět rozlišit, potřebovali bychom jako ID volit dvojici (string filename, int id)
        /// </summary>
        public int TripId { get; set; }

        /// <summary>
        /// Kalendář jízd z ASW (jedničky/nuly pro každý den exportu)
        /// </summary>
        public ServiceDaysBitmap ServiceAsBits { get; set; }

        /// <summary>
        /// Linka
        /// </summary>
        public Route Route { get; set; }

        /// <summary>
        /// Dopravce, který provádí spoj
        /// </summary>
        public AswAgency Agency { get; set; }

        /// <summary>
        /// Licenční číslo linky
        /// </summary>
        public int RouteLicenceNumber { get; set; }

        /// <summary>
        /// Směr (odvozeno z ASW JŘ)
        /// </summary>
        public int DirectionId { get; set; }

        /// <summary>
        /// Poznámky spoje
        /// </summary>
        public Remark[] Remarks { get; set; }

        /// <summary>
        /// Odkaz na spoj, ze kterého tento spoj přímo přejíždí nebo null, pokud tento spoj není přejezdem.
        /// </summary>
        public Trip PreviousTripInBlock { get; set; }
        
        /// <summary>
        /// Odkaz na spoj, na který tento spoj přímo přejíždí nebo null, pokud nepřejíždí.
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
        /// Odkaz na nejbzližší veřejný spoj v sekvenci přejezdů, ze kterého tento spoj přejíždí, nebo null, pokud nepřejíždí z veřejného spoje.
        /// </summary>
        public Trip PreviousPublicTripInBlock
        {
            get
            {
                var iterator = PreviousTripInBlock;
                while (iterator != null && !iterator.IsPublic)
                    iterator = iterator.PreviousTripInBlock;
                return iterator;
            }
        }

        /// <summary>
        /// Odkaz na nejbližší veřejný spoj v sekvenci přejezdů, na který tento spoj přejíždí, nebo null, pokud již nepřejíždí na žádný veřejný spoj
        /// </summary>
        public Trip NextPublicTripInBlock
        {
            get
            {
                var iterator = NextTripInBlock;
                while (iterator != null && !iterator.IsPublic)
                    iterator = iterator.NextTripInBlock;
                return iterator;
            }
        }

        /// <summary>
        /// Vrací první veřejný spoj v celé sekvenci (může být teoreticky před i za tímto spojem), nebo null, pokud je celá sekvence přejezdů neveřejná
        /// </summary>
        public Trip FirstPublicTripInBlock
        {
            get
            {
                var iterator = FirstTripInBlock;
                while (iterator != null && !iterator.IsPublic)
                    iterator = iterator.NextTripInBlock;
                return iterator;
            }
        }

        /// <summary>
        /// Výlukový spoj (bere se z ASW)
        /// </summary>
        public bool IsDiverted { get; set; }

        /// <summary>
        /// Pořadí, do kterého je spoj zapojen
        /// </summary>
        public List<RunDescriptor> OwnerRun { get; set; }

        /// <summary>
        /// Číslo kmenové linky oběhu, na kterém je spoj zařazen. Mělo by platit, že všechny varianty oběhů, na kterých je spoj zařazen, jsou stejné.
        /// </summary>
        public int RootLineNumber { get { return OwnerRun.First().RootLineNumber; } }

        /// <summary>
        /// Číslo oběhu na kmenové lince, na kterém je spoj zařazen. Mělo by platit, že všechny varianty oběhů, na kterých je spoj zařazen, jsou stejné.
        /// </summary>
        public int RootRunNumber { get { return OwnerRun.FirstOrDefault()?.RunNumber ?? 0; } }

        /// <summary>
        /// Číslo oběhu na aktuální lince.
        /// </summary>
        public int CurrentRunNumber { get; set; }

        /// <summary>
        /// Grafikon
        /// </summary>
        public Graph Graph { get; set; }

        /// <summary>
        /// Typ vozu dle číselníku ASW JŘ (ukládá se do oběhu pro mobilní tabla)
        /// </summary>
        public int VehicleType { get; set; }

        /// <summary>
        /// Bezbariérová přístupnost spoje
        /// </summary>
        public bool IsWheelchairAccessible { get; set; }
        
        /// <summary>
        /// Druh dopravy dle ASW JŘ.
        /// Ano, druh dopravy je v ASW JŘ uveden u každého spoje, po načtení spojů se to musí agregovat přes všechny spoje k lince.
        /// </summary>
        public AswTrafficType TrafficType { get; set; }

        /// <summary>
        /// Jde o veřejný spoj pro cestující
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Číslo závodu dle ASW JŘ
        /// </summary>
        public int CompanyId { get { return Graph.Id.CompanyId; } }

        /// <summary>
        /// Charakter výkonu dle ASW JŘ
        /// </summary>
        public Character TripCharacter { get; set; }

        /// <summary>
        /// Druh dopravy (městská / příměstská / ...)
        /// Vychází z <see cref="Route.LineType"/>, ale může ho ještě upravit podle charakteru tripu.
        /// 
        /// Jeli <see cref="TripCharacter"/> = <see cref="Character.Contractual"/>, pak je LineType vždy <see cref="AswLineType.SpecialTransport"/>.
        /// </summary>
        public AswLineType LineType
        {
            get
            {
                if (TripCharacter != Character.Contractual)
                {
                    return Route.LineType;
                }
                else
                {
                    return AswLineType.SpecialTransport;
                }
            }
        }

        /// <summary>
        /// Typ výkonu dle ASW JŘ
        /// </summary>
        public TripOperationType TripType { get; set; } 

        /// <summary>
        /// Číslo spoje ROPID
        /// </summary>
        public int TripNumber { get; set; }

        /// <summary>
        /// Manipulační spoj (reálně to znamená leccos)
        /// </summary>
        public bool HasManipulationFlag { get; set; }

        /// <summary>
        /// Úplně všechna načtená zastavení včetně úvodních neveřejných a smyček.
        /// </summary>
        public StopTime[] AllStopTimes { get; private set; }

        /// <summary>
        /// Všechna zastavení počínaje první veřejnou zastávkou končeje poslední veřejnou zastávkou, navíc očištěné
        /// o smyčky na konci a začátku spoje. Může však stále obsahovat neveřejné zastávky uprostřed trasy.
        /// Je potřeba kvůli určení trasy (chronometráže mohou vést přes neveřejné zastávky).
        /// 
        /// Pro načtení úplně všech zastavení včetně úvodních neveřejných a smyček použijte <see cref="AllStopTimes"/>
        /// Pro načtení pouze veřejných zastavení (tedy odfiltrování neveřejných uvnitř trasy) použijte <see cref="PublicStopTimes"/>
        /// </summary>
        public StopTime[] StopTimesPublicPart { get; private set; }

        /// <summary>
        /// Všechna zastavení ve veřejných zastávkách
        /// </summary>
        public IEnumerable<StopTime> PublicStopTimes
        {
            get
            {
                return StopTimesPublicPart.Where(st => st.IsPublic);
            }
        }
        

        /// <summary>
        /// Přidá zastávku na konec trasy.
        /// </summary>
        /// <param name="stopTime">Záznam o zastavení v zastávce.</param>
        public void SetStopTimes(IList<StopTime> stopTimes)
        {
            AllStopTimes = stopTimes.ToArray();
            StopTimesPublicPart = GetPublicPart(stopTimes).ToArray();
        }
        
        private IEnumerable<StopTime> GetPublicPart(IList<StopTime> stopTimes)
        {
            int firstIndex = 0;
            int lastIndex = stopTimes.Count - 1;

            while (firstIndex < stopTimes.Count && !stopTimes[firstIndex].IsPublic)
            {
                firstIndex++;
            }

            while (lastIndex >= 0 && !stopTimes[lastIndex].IsPublic)
            {
                lastIndex--;
            }
                        
            if (lastIndex >= firstIndex)
                return stopTimes.Skip(firstIndex).Take(lastIndex - firstIndex + 1);
            else
                return Enumerable.Empty<StopTime>();
        }

        /// <summary>
        /// Rozdělí spoj na dva podle masky. Vrácený spoj bude mít tu část bitmapy, která se překrývá s
        /// <paramref name="bitmapMaskOfResult"/>, tomuto spoji naopak budou tyto bity smazány.
        /// Pokud by výsledný trip vycházel s nulovým kalendářem, nebude vytvořen a metoda vrátí null.
        /// Pokud by výsledný trip zcela přemazal ten současný (současnému by nezbyly aktivní dny v kalendáři),
        /// není vytvořeno nic a je vrácen současný trip.
        /// </summary>
        /// <param name="bitmapMaskOfResult">Maska, pro níž chceme odštěpit spoj.</param>
        /// <returns>Odštěpený spoj</returns>
        public Trip SplitByCalendarMask(ServiceDaysBitmap bitmapMaskOfResult)
        {
            var targetBitmap = ServiceAsBits.Intersect(bitmapMaskOfResult);
            if (targetBitmap.IsEmpty)
            {
                return null;
            }
            else if (ServiceAsBits.Subtract(targetBitmap).IsEmpty)
            {
                return this;
            }

            // výchozí stav - odtržený trip bude mít stejného předchůdce, jak tento...
            var previousSplit = PreviousTripInBlock;
            if (PreviousTripInBlock != null)
            {
                // ...ale možné od něho bude také potřeba něco odtrhnout
                previousSplit = PreviousTripInBlock.SplitByCalendarMask(bitmapMaskOfResult);
            }

            var result = new Trip
            {
                Agency = Agency,
                DirectionId = DirectionId,
                Graph = Graph,
                HasManipulationFlag = HasManipulationFlag,
                IsDiverted = IsDiverted,
                IsPublic = IsPublic,
                IsWheelchairAccessible = IsWheelchairAccessible,
                NextTripInBlock = NextTripInBlock,
                OwnerRun = OwnerRun.Where(r => !r.ServiceAsBits.Intersect(bitmapMaskOfResult).IsEmpty).ToList(),
                PreviousTripInBlock = previousSplit,
                Remarks = Remarks,
                Route = Route,
                RouteLicenceNumber = RouteLicenceNumber,
                ServiceAsBits = targetBitmap,
                TrafficType = TrafficType,
                TripCharacter = TripCharacter,
                TripId = TripId + 10000000,
                TripNumber = TripNumber,
                TripType = TripType,
                VehicleType = VehicleType,
            };

            result.SetStopTimes(AllStopTimes);
            Route.Trips.Add(result);
            foreach (var ownerRun in result.OwnerRun)
            {
                ownerRun.Trips.Replace(this, result);
                this.OwnerRun.Remove(ownerRun);
            }

            if (previousSplit != null)
            {
                previousSplit.NextTripInBlock = result;
            }

            this.ServiceAsBits = ServiceAsBits.Subtract(targetBitmap);
            return result;
        }

        public override string ToString()
        {
            return $"{Route}|{TripId}";
        }

        public string ToStringWithCalendar()
        {
            return $"{ToString()} cal {ServiceAsBits} graph {Graph}";
        }
    }
}
