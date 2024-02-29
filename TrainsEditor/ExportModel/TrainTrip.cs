using CommonLibrary;
using GtfsLogging;
using GtfsModel.Enumerations;
using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Vlakový spoj (respektive jeho část, která je celá v rámci jedné linky). Skutečný vlak je tak složen z jednoho nebo více těchto úseků.
    /// </summary>
    class TrainTrip : GtfsModel.Extended.Trip
    {
        /// <summary>
        /// Odkaz na celý vlak
        /// </summary>
        public Train WholeTrain { get; set; }

        /// <summary>
        /// Kdy je vlak vypraven (od StartDate)
        /// </summary>
        public ServiceDaysBitmap ServiceBitmap { get { return WholeTrain.ServiceBitmap; } }
        
        /// <summary>
        /// První datum, kdy vlak jede
        /// </summary>
        public DateTime StartDate { get { return WholeTrain.StartDate; } }

        /// <summary>
        /// Číslo vlaku
        /// </summary>
        public int TrainNumber { get; set; }
        
        /// <summary>
        /// Soubor, ze kterého vlak pochází (pro debug účely)
        /// </summary>
        public string FileName { get { return WholeTrain.FileName; } }

        /// <summary>
        /// Otočí směr (DirectionId)
        /// </summary>
        public void RevertDirection()
        {
            if (DirectionId == Direction.Inbound)
                DirectionId = Direction.Outbound;
            else
                DirectionId = Direction.Inbound;
        }

        /// <summary>
        /// Vytvoří instanci vlakového spoje nebo null, pokud nejde o PIDový vlak (nemá alespoň dvě PIDová zastavení).
        /// </summary>
        /// <param name="wholeTrain">Nadvlak (celá trasa)</param>
        /// <param name="stationTimes">Zastavení spoje</param>
        /// <param name="lineDb">Databáze linek pro resolve linky</param>
        /// <param name="loaderLog">Logger načítání dat</param>
        /// <param name="processLog">Logger načtených vlaků</param>
        /// <param name="emptyLineHandler">Procedura, která se zavolá, když se narazí na vlak s nevyplněnou linkou</param>
        public static TrainTrip Construct(Train wholeTrain, List<StationTime> stationTimes, RouteDatabase lineDb, ICommonLogger loaderLog, ISimpleLogger processLog,
            Train.TrainWithEmptyLineHandler emptyLineHandler)
        {
            if (!stationTimes.Where(st => !st.IsSubstituteTransportOnDeparture).Any2())
                return null;

            var trainNumber = stationTimes.First().TrainNumberOnDeparture;
            var trainType = stationTimes.First().TrainTypeOnDeparture;
            var trainShortName = trainType != null ? $"{trainType} {trainNumber}" : trainNumber.ToString();
            var trainRoute = $"{stationTimes.First().Stop} {stationTimes.First().DepartureTime} - {stationTimes.Last().Stop} {stationTimes.Last().ArrivalTime}";

            var integratedStopTimesPart = TrimUnknownStops(stationTimes);
            if (!integratedStopTimesPart.Any2())
            {
                processLog.Log($"{wholeTrain.FileName}: {trainShortName} {trainRoute} - ignorován, mimo PID");
                return null;
            }

            if (!integratedStopTimesPart.Where(st => st.IsPublic).Any2())
            {
                processLog.Log($"{wholeTrain.FileName}: {trainShortName} {trainRoute} - ignorován - je v PID, ale nemá tam dostatek veřejných zastavení");
                return null;
            }

            var trainRouteInPID = $"{integratedStopTimesPart.First().Stop} {integratedStopTimesPart.First().DepartureTime} - {integratedStopTimesPart.Last().Stop} {integratedStopTimesPart.Last().ArrivalTime}";

            if (stationTimes.First().TrainLineOnDeparture.IsNonPidLine)
            {
                processLog.Log($"{wholeTrain.FileName}: {trainShortName} {trainRoute} v PID {trainRouteInPID}, avšak není označen PIDovou linkou - ignorován");
                emptyLineHandler?.Invoke(stationTimes);
                return null;
            }

            var line = lineDb.Lines.GetValueOrDefault(stationTimes.First().TrainLineOnDeparture.LineName);
            if (line == null)
            {
                processLog.Log($"{wholeTrain.FileName}: {trainShortName} {trainRoute} v PID {trainRouteInPID}, avšak linka {stationTimes.First().TrainLineOnDeparture.LineName} není v číselníku - ignorován");
                return null;
            }

            var prevTrainInBlock = wholeTrain.LineTrips.LastOrDefault();
            var isWheelchairAccessible = stationTimes.All(st => st.IsWheelchairAccessible);

            var result = new TrainTrip()
            {
                WholeTrain = wholeTrain,
                BikesAllowed = BikeAccessibility.Possible,
                DirectionId = (trainNumber % 2 == 1) ? Direction.Inbound : Direction.Outbound, // BÚNO
                IsExceptional = line.AswId >= 1100 && line.AswId < 1200,
                StopTimes = stationTimes.Select(st => st as StopTime).ToList(),
                ShortName = trainShortName,
                TrainNumber = trainNumber,
                Route = line,
                PreviousTripInBlock = (prevTrainInBlock != null && prevTrainInBlock.StopTimes.Last().Stop.AswNodeId == stationTimes.First().Stop.AswNodeId) ? prevTrainInBlock : null,
                WheelchairAccessible = isWheelchairAccessible ? WheelchairAccessibility.Possible : WheelchairAccessibility.NotPossible,
                SubAgency = line.SubAgencies.FirstOrDefault(), // v současnosti vycházíme z toho, že jednu vlakovou linku jezdí jen 1 dopravce
            };

            // pojistka, aby u spojů v blocku jsme se nevraceli v čase (protože se tam stoptime opakuje a bylo by tam dvakrát pod sebou třeba 15:22-15:24)
            if (result.PreviousTripInBlock != null)
            {
                result.PreviousTripInBlock.StopTimes.Last().DepartureTime = result.PreviousTripInBlock.StopTimes.Last().ArrivalTime;
                result.StopTimes.First().ArrivalTime = result.StopTimes.First().DepartureTime;
            }            

            foreach (var stationTime in stationTimes)
            {
                stationTime.Trip = result;
            }

            if (result.PreviousTripInBlock != null)
            {
                result.PreviousTripInBlock.NextTripInBlock = result;
            }

            processLog.Log($"{wholeTrain.FileName}: {trainShortName} {trainRoute} v PID {trainRouteInPID} linka {line} - zpracováno OK, kalendář od {result.StartDate:d.M.yyyy}: {result.ServiceBitmap}");

            return result;
        }

        private static List<StationTime> TrimUnknownStops(List<StationTime> stationTimes)
        {
            var startIndex = 0;
            while (startIndex < stationTimes.Count && !stationTimes[startIndex].IsInIntegratedArea)
            {
                startIndex++;
            }

            var endIndex = stationTimes.Count - 1;
            while (endIndex >= startIndex && !stationTimes[endIndex].IsInIntegratedArea)
            {
                endIndex--;
            }

            return stationTimes.GetRange(startIndex, endIndex - startIndex + 1);
        }

        // 
        // veřejné zastávky, které jsou uprostřed trasy a nebyly nalezeny v číselníku, dá do logů
        /// <summary>
        /// Vymaže neveřejná zastavení a zastavení mimo oblast na začátku spoje, pokud nepokračuje z jiného spoje.
        /// Vymaže neveřejná zastavení a zastavení mimo oblast na konci spoje, pokud nepřevléká na jiný spoj.
        /// </summary>
        /// <param name="loaderLog"></param>
        public void SetPublicPartOfStationTimes(ICommonLogger loaderLog)
        {
            // osekání konečků
            if (PreviousTripInBlock == null)
            {
                while (StopTimes.Any() &&
                    (!StopTimes.First().IsPublic || !((StationTime)StopTimes.First()).IsInIntegratedArea))
                {
                    StopTimes.RemoveAt(0);
                }

                // příjezd do nástupní nás (zatím) nezajímá, ale v budoucnu by mohl
                StopTimes.First().ArrivalTime = StopTimes.First().DepartureTime;
            }

            if (NextTripInBlock == null)
            {
                while (!StopTimes.Last().IsPublic || !((StationTime)StopTimes.Last()).IsInIntegratedArea)
                {
                    StopTimes.RemoveAt(StopTimes.Count - 1);
                }

                // odjezd z poslední nás nezajímá, akorát by vytvářel divné nesmysly
                StopTimes.Last().DepartureTime = StopTimes.Last().ArrivalTime;
            }

            // kontrola, jestli nejsou uprostřed nějaké zastávky, které nebyly nalezeny v číselníku
            foreach (var stationTime in StopTimes.Cast<StationTime>())
            {
                if (stationTime.IsPublic && !((TrainStop)stationTime.Stop).IsFromAsw)
                {
                    loaderLog.Log(LogMessageType.WARNING_TRAIN_MISSING_STOP_IN_ASW, $"Zastávka {stationTime.StationCode} {stationTime.StationName} není v číselníku ASW, přitom se nachází uvnitř trasy vlaku {ShortName}. Pro jistotu označuji jako neveřejnou (mohlo by jít o omylné označení neveřejné stanice).");
                    stationTime.PickupType = PickupType.None;
                    stationTime.DropOffType = DropOffType.None;
                }
            }
        }

        public override string ToString()
        {
            return ShortName;
        }

        public string ToStringEx()
        {
            if (StopTimes.Any())
            {
                var firstStopTime = WholeTrain.StopTimes.First();
                var lastStopTime = WholeTrain.StopTimes.Last();
                return $"{ShortName} ({firstStopTime.Stop.Name} {firstStopTime.DepartureTime} - {lastStopTime.Stop.Name} {lastStopTime.ArrivalTime})";
            }
            else
            {
                return ToString();
            }
        }
    }
}
