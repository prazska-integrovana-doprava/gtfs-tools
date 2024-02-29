using CommonLibrary;
using CzpttModel;
using CzpttModel.Kango;
using GtfsLogging;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainsEditor.CommonLogic;
using TrainsEditor.CommonModel;

namespace TrainsEditor.ExportModel
{
    /// <summary>
    /// Jeden vlak. Může po trase měnit číslo nebo linku, což je pak reprezentováno více prvky v <see cref="LineTrips"/>
    /// </summary>
    class Train
    {
        /// <summary>
        /// Kdy je vlak vypraven (od StartDate)
        /// </summary>
        public ServiceDaysBitmap ServiceBitmap { get; set; }

        /// <summary>
        /// První datum, kdy vlak jede
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Poslední den, kdy vlak jede
        /// </summary>
        public DateTime EndDate { get { return StartDate.AddDays(ServiceBitmap.Length - 1); } }
        
        /// <summary>
        /// SŽDC identifikátor vlaku (bez varianty)
        /// </summary>
        public string TrIdCompanyAndCoreAndYear { get; set; }

        /// <summary>
        /// Varianta spoje
        /// </summary>
        public string TrIdVariant { get; set; }

        /// <summary>
        /// SŽDC identifikátor datové zprávy, která vlak popisuje (bez varianty).
        /// Může být shodné s <see cref="TrIdCore"/>, ale může být i jiné (pak je v <see cref="TrIdCore"/> ID vlaku, který nahrazuje)
        /// </summary>
        public string PaIdCore { get; set; }

        /// <summary>
        /// Soubor, ze kterého vlak pochází (pro debug účely)
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Datum a čas, kdy záznam o vlaku vzniknul
        /// </summary>
        public DateTime DataCreationTime { get; set; }
        
        /// <summary>
        /// Všechna zastavení vlaku
        /// </summary>
        public List<StationTime> StopTimes { get; set; }

        /// <summary>
        /// Vlak rozdělený po linkách
        /// </summary>
        public List<TrainTrip> LineTrips { get; set; }

        /// <summary>
        /// True, pokud soubor obsahoval protičasy (odjezd po příjezdu nebo příjezd po předchozím odjezdu)
        /// </summary>
        public bool HasInvalidTimes => FirstInvalidTime.HasValue;

        /// <summary>
        /// První protičas na trase
        /// </summary>
        public Time? FirstInvalidTime { get; set; }

        /// <summary>
        /// Vrátí všechna čísla, která vlak na své trase vystřídá.
        /// </summary>
        public string[] GetTrainNumbersUnique()
        {
            return LineTrips.Where(lt => !string.IsNullOrEmpty(lt.ShortName)).Select(lt => lt.ShortName).Distinct().ToArray();
        }

        public delegate void TrainWithEmptyLineHandler(List<StationTime> stationTimes);

        /// <summary>
        /// Transformuje CZPTTCISMessage XML soubor na záznam Train. Pokud neexistuje dost veřejných zastávek, vrací null.
        /// </summary>
        /// <param name="trainFile">Načtený soubor o vlaku (musí být vlak a nikoliv CANCEL zpráva - zrušení vlaku)</param>
        /// <param name="stopDb">Databáze stanic, zastávek a dalších bodů</param>
        /// <param name="lineDb">Databáze linek</param>
        /// <param name="loaderLog">Logování načítání dat</param>
        /// <param name="processLog">Logování zpracování</param>
        /// <param name="emptyLineHandler">Procedura, která se zavolá, když se narazí na vlak s nevyplněnou linkou</param>
        public static Train Create(SingleTrainFile trainFile, StationDatabase stopDb, RouteDatabase lineDb, ICommonLogger loaderLog, ISimpleLogger processLog,
            TrainWithEmptyLineHandler emptyLineHandler)
        {
            var czpttMessage = trainFile.TrainData;
            var paId = czpttMessage.Identifiers.First(i => i.ObjectType == CompositeIdentifierPlannedType.ObjectTypeEnum.PA);
            var trId = czpttMessage.Identifiers.First(i => i.ObjectType == CompositeIdentifierPlannedType.ObjectTypeEnum.TR);
            var resultTrain = new Train()
            {
                FileName = trainFile.FileFullPath,
                TrIdCompanyAndCoreAndYear = trainFile.TrIdCompanyAndCoreAndYear,
                TrIdVariant = trId.Variant,
                PaIdCore = paId.Core,
                DataCreationTime = trainFile.CreationDateTime,
            };

            resultTrain.SetTrimmedServiceBitmap(trainFile.StartDate, trainFile.BitmapEx);

            var networkSpecificParamsProvider = new TrainNetworkSpecificParamsProvider(czpttMessage);

            // prochází stanice a pokud tam vlak staví, přidá ji do seznamu
            // pokud se změní číslo vlaku nebo linka, vrátíme, co máme, a konstruujeme další vlak
            var stationTimes = new List<StationTime>();
            CZPTTLocation prevLocation = null;
            StationTime prevPublicStationTime = null;
            foreach (var location in czpttMessage.CZPTTInformation.CZPTTLocation)
            {
                var isLocationFirst = location == czpttMessage.CZPTTInformation.CZPTTLocation.First();
                var isLocationLast = location == czpttMessage.CZPTTInformation.CZPTTLocation.Last();
                var isWheelchairAccessible = networkSpecificParamsProvider.FindCentralNotesForLocation(location, CentralNoteCode.WheelchairTransportAndPickup, CentralNoteCode.WheelchairTransportAvailable).Any();

                var stationTime = StationTime.Create(location, czpttMessage, isWheelchairAccessible, prevLocation, stopDb, isLocationFirst || isLocationLast, loaderLog);
                if (stationTime == null)
                    continue;

                resultTrain.CorrectDeparturesBeforeArrivals(stationTime, stationTimes.LastOrDefault()?.DepartureTime, loaderLog);
                resultTrain.StopTimes.Add(stationTime);
                if (stationTime.IsValid)
                {
                    stationTimes.Add(stationTime);
                }

                if (prevPublicStationTime != null
                    && (prevPublicStationTime.TrainNumberOnDeparture != stationTime.TrainNumberOnDeparture 
                        || prevPublicStationTime.TrainLineOnDeparture != stationTime.TrainLineOnDeparture
                        || prevPublicStationTime.IsSubstituteTransportOnDeparture != stationTime.IsSubstituteTransportOnDeparture))
                {
                    // nesmíme použít 'prevConstructedStationTime', protože potřebujeme vlastní referenci (bude jiná linka)
                    stationTime = StationTime.Create(location, czpttMessage, isWheelchairAccessible, prevLocation, stopDb, isLocationFirst || isLocationLast, loaderLog);
                    if (stationTime.IsValid && stationTime.IsPublic)
                    {
                        var lineTrain = TrainTrip.Construct(resultTrain, stationTimes, lineDb, loaderLog, processLog, emptyLineHandler);
                        if (lineTrain != null)
                            resultTrain.LineTrips.Add(lineTrain);

                        resultTrain.CorrectDeparturesBeforeArrivals(stationTime, lineTrain?.StopTimes?.LastOrDefault()?.ArrivalTime, loaderLog); //arrival je schválně, departure z poslední stanice je nesmysl
                        stationTimes = new List<StationTime>();
                        if (stationTime.IsPublicOnDeparture && !stationTime.IsSubstituteTransportOnDeparture)
                        {
                            stationTimes.Add(stationTime);
                        }
                    }
                }

                prevLocation = location;
                if (stationTime.IsValid && stationTime.IsPublic)
                {
                    prevPublicStationTime = stationTime;
                }
            }

            if (stationTimes.Any2())
            {
                var lineTrain = TrainTrip.Construct(resultTrain, stationTimes, lineDb, loaderLog, processLog, emptyLineHandler);
                if (lineTrain != null)
                    resultTrain.LineTrips.Add(lineTrain);
            }

            if (!resultTrain.LineTrips.Any())
            {
                // nevznikly žádné vlaky (asi málo veřejných stanic)
                return null;
            }

            if (resultTrain.StopTimes.First().DepartureTime < new Time(0, 50, 0))
            {
                resultTrain.Add24Hours();
            }

            resultTrain.RemoveNonpublicStopsAtStartEnd();
            foreach (var lineTrip in resultTrain.LineTrips)
            {
                lineTrip.SetPublicPartOfStationTimes(loaderLog);
                lineTrip.Headsign = resultTrain.StopTimes.LastOrDefault()?.StationName;
            }

            return resultTrain;
        }

        private Train()
        {
            LineTrips = new List<TrainTrip>();
            StopTimes = new List<StationTime>();
        }

        // odstraní nuly na konci a začátku bitmapy (mohly vzniknout například přepisem vlaku jiným vlakem)
        protected void SetTrimmedServiceBitmap(DateTime startDate, CalendarValue[] bitmapEx)
        {
            var lastNonzeroIndex = bitmapEx.Length - 1;
            var firstNonzeroIndex = 0;

            while (lastNonzeroIndex >= 0 && bitmapEx[lastNonzeroIndex] != CalendarValue.Active)
            {
                lastNonzeroIndex--;
            }

            while (firstNonzeroIndex <= lastNonzeroIndex && bitmapEx[firstNonzeroIndex] != CalendarValue.Active)
            {
                firstNonzeroIndex++;
            }

            StartDate = startDate.AddDays(firstNonzeroIndex);
            ServiceBitmap = new ServiceDaysBitmap(bitmapEx.Skip(firstNonzeroIndex).Take(lastNonzeroIndex - firstNonzeroIndex + 1).Select(val => val == CalendarValue.Active).ToArray());
        }

        protected void RemoveNonpublicStopsAtStartEnd()
        {
            while (StopTimes.Any() && !StopTimes.First().IsPublic)
            {
                StopTimes.RemoveAt(0);
            }

            while (StopTimes.Any() && !StopTimes.Last().IsPublic)
            {
                StopTimes.RemoveAt(StopTimes.Count - 1);
            }
        }

        // Kontrola a korekce protičasů (nesmí upravovat minulá data, takže upravuje jen stoptime)
        protected void CorrectDeparturesBeforeArrivals(StationTime stopTime, Time? prevStopTimeDeparture, ICommonLogger loaderLog)
        {
            if (stopTime.ArrivalTime > stopTime.DepartureTime)
            {
                FirstInvalidTime = FirstInvalidTime ?? stopTime.DepartureTime;
                loaderLog.Log(LogMessageType.WARNING_STOPTIME_PREMATURE_DEPARTURE, $"Čas příjezdu do {stopTime.Stop.Name} je po času odjezdu z této stanice. Nastavuji čas příjezdu z {stopTime.ArrivalTime} na {stopTime.DepartureTime}.", this);
                stopTime.ArrivalTime = stopTime.DepartureTime;
            }

            if (prevStopTimeDeparture != null && prevStopTimeDeparture.Value > stopTime.ArrivalTime)
            {
                FirstInvalidTime = FirstInvalidTime ?? stopTime.ArrivalTime;
                if (prevStopTimeDeparture.Value > stopTime.DepartureTime)
                {
                    loaderLog.Log(LogMessageType.WARNING_STOPTIME_PREMATURE_ARRIVAL, $"Čas příjezdu i odjezdu z {stopTime.Stop.Name} je před časem odjezdu z předchozí stanice. Nastavuji časy příjezdu i odjezdu z {stopTime.ArrivalTime}–{stopTime.DepartureTime} na {prevStopTimeDeparture.Value}.", this);
                    stopTime.ArrivalTime = prevStopTimeDeparture.Value;
                    stopTime.DepartureTime = prevStopTimeDeparture.Value;
                }
                else
                {
                    loaderLog.Log(LogMessageType.WARNING_STOPTIME_PREMATURE_ARRIVAL, $"Čas příjezdu do {stopTime.Stop.Name} je před časem odjezdu z předchozí stanice. Nastavuji čas příjezdu z {stopTime.ArrivalTime} na {prevStopTimeDeparture.Value}.", this);
                    stopTime.ArrivalTime = prevStopTimeDeparture.Value;
                }
            }
        }

        protected void Add24Hours()
        {
            StartDate = StartDate.AddDays(-1);
            foreach (var stationTime in LineTrips.SelectMany(lt => lt.StopTimes))
            {
                stationTime.ArrivalTime = stationTime.ArrivalTime.AddDay();
                stationTime.DepartureTime = stationTime.DepartureTime.AddDay();
            }

            if (FirstInvalidTime.HasValue)
            {
                FirstInvalidTime = FirstInvalidTime.Value.AddDay();
            }
        }

        public override string ToString()
        {
            return $"TR {TrIdCompanyAndCoreAndYear}_{TrIdVariant} (PA {PaIdCore})";
        }
    }

}
