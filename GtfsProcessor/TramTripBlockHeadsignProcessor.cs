using AswModel.Extended;
using CommonLibrary;
using GtfsLogging;
using GtfsProcessor.DataClasses;
using GtfsProcessor.Logging;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Pouze kosmetické dorovnání headsignů u výjezdových a zátahových tramvajových spojů. Pracuje nad <see cref="MergedTripGroup"/>,
    /// který ještě Headsign nemá (ten se bude konstruovat až později), takže pouze poznamenává <see cref="MergedTripGroup.CopyHeadsignFromNextTrip"/>.
    /// 
    /// Pozor, může spojům měnit také linku (<see cref="MergedTripGroup.Route"/>) a direction - na přejezdech, které jsou prodloužením pravidelného spoje
    /// (např. 15ka do Řep je v datech původně jako 98, takže se to zde přehodí na 15)
    /// </summary>
    class TramTripBlockHeadsignProcessor
    {
        private ICommonLogger log = Loggers.CommonLoggerInstance;

        public TramTripBlockHeadsignProcessor(ICommonLogger log)
        {
            this.log = log;
        }

        /// <summary>
        /// Vyplní všem tripům položku <see cref="MergedTripGroup.CopyHeadsignFromNextTrip"/>,
        /// v případě potřeby též upravuje <see cref="MergedTripGroup.Route"/> a <see cref="MergedTripGroup.DirectionId"/>
        /// </summary>
        /// <param name="trips"></param>
        public List<MergedTripGroup> ProcessTripsBlocks(IEnumerable<MergedTripGroup> trips)
        {
            var resultTrips = new List<MergedTripGroup>();
            foreach (var trip in trips.Where(t => t.PreviousPublicTripInBlock == null))
            {
                var processedTrips = ProcessTripBlock(GetAllTripsInBlock(trip).ToList());
                resultTrips.AddRange(processedTrips);
            }

            return resultTrips;
        }

        private IEnumerable<MergedTripGroup> GetAllTripsInBlock(MergedTripGroup firstTripInBlock)
        {
            var iterator = firstTripInBlock;
            while (iterator != null)
            {
                yield return iterator;
                iterator = iterator.NextPublicTripInBlock;
            }
        }

        // zkoriguje headsigny v rámci bloku - výjezdové spoje dostanou headsign následujícího spoje v bloku, naopak spoje před zátahem dostanou headsign vozovny,
        // podobně u přejezdů se zkopíruje headsign z následného spoje
        //
        // zároveň přerovná direction idčka u výjezdů a zátahů
        private List<MergedTripGroup> ProcessTripBlock(List<MergedTripGroup> tripBlock)
        {
            // TODO zbývající problémy
            // - těžko zaručit, že ta predikce je vždy správná - co když někdy zátahový / přejezdový spoj má nějakou jinou orientaci?
            //    - napadá mě třeba 6 do Vozovny Hloubětín, která má orientaci Palmovka, protože tam jede
            //    - co za orientaci má 13ka na Čechovo náměstí, která dále pokračuje směr Nádr. Hostivař (jako 99? nebo jako 13?)
            // - evidentně ani číslo nemusí být vždy správně, viz dvojka do Modřan, dá se nějak vypozorovat správné pravidlo?

            if (tripBlock.All(trip => trip.TripType == TripOperationType.Regular))
                return tripBlock; // celý blok se skládá jen z obyčejných spojů, netřeba nic řešit

            if (tripBlock.First().TrafficType != AswTrafficType.Tram)
            {
                log.Log(LogMessageType.WARNING_TRIP_IRREGULAR_NOT_TRAM, $"Neregulérní spoj (výjezdy / zátahy / přejezdy) na jiné síti než v tramvajích: {string.Join(" - ", tripBlock)}");
            }

            // 1. zátahové spoje a spoje přejíždějící na jinou linku se spojí s předchozím spojem
            // newtriplist bude jako zásobník, budeme si na něj klást spoje odzadu (asi bychom si ho ani sestavovat nemuseli, beztak ho zahodíme)
            var newTripList = new List<MergedTripGroup>() { tripBlock.Last() };
            while (newTripList.First().PreviousPublicTripInBlock != null)
            {
                var trip = newTripList.First();
                if (trip.TripType == TripOperationType.ToDepo || trip.TripType == TripOperationType.TransferBetweenLines)
                {
                    // zátahový a přejezdový spoj kopíruje linku a směr předchozího a dohromady mají orientaci druhého spoje
                    newTripList[0] = ConcatTrips(trip.PreviousPublicTripInBlock, trip, trip.PreviousPublicTripInBlock.Route, trip.PreviousPublicTripInBlock.DirectionId, trip.PreviousPublicTripInBlock.TripType);
                }
                else
                {
                    newTripList.Insert(0, trip.PreviousPublicTripInBlock);
                }
            }

            // 2. výjezdové spoje se sloučí s následujícím spojem
            newTripList = new List<MergedTripGroup>() { newTripList.First() };
            while (newTripList.Last().NextPublicTripInBlock != null)
            {
                var trip = newTripList.Last();
                if (trip.TripType == TripOperationType.FromDepo || trip.TripType == TripOperationType.TransferBetweenLines)
                {
                    newTripList[newTripList.Count - 1] = ConcatTrips(trip, trip.NextPublicTripInBlock, trip.NextPublicTripInBlock.Route, trip.NextPublicTripInBlock.DirectionId, trip.NextPublicTripInBlock.TripType);
                }
                else
                {
                    newTripList.Add(trip.NextPublicTripInBlock);
                }
            }

            // 3. spoje přejíždějící v rámci linky se spojí s předchozím i následným spojem
            newTripList = new List<MergedTripGroup>() { newTripList.First() };
            while (true)
            {
                var trip = newTripList.Last();
                if (trip.TripType == TripOperationType.TransferWithinLine)
                {
                    if (trip.PreviousPublicTripInBlock != null)
                    {
                        newTripList.Remove(trip.PreviousPublicTripInBlock);
                        trip = ConcatTrips(trip.PreviousPublicTripInBlock, trip, trip.PreviousPublicTripInBlock.Route, trip.PreviousPublicTripInBlock.DirectionId, trip.PreviousPublicTripInBlock.TripType);
                        newTripList[newTripList.Count - 1] = trip;
                    }

                    if (trip.NextPublicTripInBlock != null)
                    {
                        newTripList[newTripList.Count - 1] = ConcatTrips(trip, trip.NextPublicTripInBlock, trip.NextPublicTripInBlock.Route, trip.NextPublicTripInBlock.DirectionId, trip.NextPublicTripInBlock.TripType);
                    }
                    else
                    {
                        break;
                    }
                }
                else if (trip.NextPublicTripInBlock != null)
                {
                    newTripList.Add(trip.NextPublicTripInBlock);
                }
                else
                {
                    break;
                }
            }

            return newTripList;
        }

        private MergedTripGroup ConcatTrips(MergedTripGroup first, MergedTripGroup second, Route route, int directionId, TripOperationType tripType)
        {
            if (!first.ServiceAsBits.Equals(second.ServiceAsBits) || first.LineType != second.LineType)
            {
                log.Log(LogMessageType.ERROR_TRIP_BLOCK_INCOMPATIBLE, $"Spojování dvou tripů {first} a {second} do jednoho není možné, protože mají odlišné kalendáře. Druhý bude smazán.");
                return first;
            }

            first.DirectionId = directionId;
            first.Route = route;
            first.TripType = tripType;
            first.IsExceptional |= second.IsExceptional;
            first.NextPublicTripInBlock = second.NextPublicTripInBlock;
            if (second.NextPublicTripInBlock != null)
                second.NextPublicTripInBlock.PreviousPublicTripInBlock = first;

            var concatStopTimes = first.StopTimes.ToList();

            // poslední stoptime prvního spoje a první stoptime druhého spoje odkazují na stejnou zastávku, musí být sloučeny;
            // použijeme poslední stoptime prvního tripu a dosadíme mu některé vlastnosti (třeba departure time)
            var commonStopTime = concatStopTimes.Last();
            if (commonStopTime.Stop == second.StopTimes.First().Stop)
            {
                commonStopTime.DepartureTime = second.StopTimes.First().DepartureTime;
                commonStopTime.IsRequestStop = second.StopTimes.First().IsRequestStop;
                commonStopTime.Remarks = commonStopTime.Remarks.Concat(second.StopTimes.First().Remarks).ToArray();
                commonStopTime.TripOperationType = second.StopTimes.First().TripOperationType;
            }
            else
            {
                log.Log(LogMessageType.WARNING_TRIP_BLOCK_STOPS_DO_NOT_MATCH, $"Spojování dvou tripů {first} a {second} do jednoho, navzdory očekávání však poslední zastavení prvního neodpovídá prvnímu zastavení druhého.");
                concatStopTimes.Add(second.StopTimes.First());
            }

            // přeřadíme stoptimes druhého spoje prvnímu (první nás nezajímá, ten je shodný s posledním z prvního tripu)
            foreach (var stopTime in second.StopTimes.Skip(1))
            {
                concatStopTimes.Add(stopTime);
            }

            first.StopTimes = concatStopTimes.ToArray();
            return first;
        }
    }
}
