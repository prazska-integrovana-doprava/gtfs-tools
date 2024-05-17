using AswModel.Extended;
using GtfsModel.Enumerations;
using GtfsProcessor.DataClasses;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Transformuje zmergované tripy <see cref="MergedTripGroup"/> do GTFS tripů <see cref="GtfsModel.Extended.Trip"/>.
    /// 
    /// Přitom aktualizuje u GTFS linek seznamy jejich tripů a u kalendářů seznam tripů daného kalendáře.
    /// 
    /// Také konstruuje mapování <see cref="GtfsModel.Extended.StopTime"/> na ASW návazné poznámky <see cref="Remark"/> pro
    /// další zpracování garantovaných návazností.
    /// </summary>
    class TripsTransformation
    {
        private IDictionary<Route, GtfsModel.Extended.Route> routesTransformation;
        private IDictionary<Stop, StopVariantsMapping> stopsTransformation;
        private IDictionary<MergedTripGroup, ShapeEx> shapeAssignment;
        private IDictionary<MergedTripGroup, GtfsModel.Extended.CalendarRecord> calendarAssignment;
        private TripIdPersistentDb tripIdPersistentDb;

        // sem se ukládá mapování při generování (protože jsme líní a nechceme si to předávat všude parametrem)
        private IDictionary<MergedTripGroup, GtfsModel.Extended.Trip> transformedTrips;

        private BikeAllowanceDefinition bikeAllowanceDefinition = new BikeAllowanceDefinition();

        /// <summary>
        /// Vytvoří instanci TripsTransformation
        /// </summary>
        /// <param name="routesTransformation">Mapování ASW linek na GTFS linky</param>
        /// <param name="stopsTransformation">Mapování ASW zastávek na GTFS zastávky</param>
        /// <param name="shapeAssignment">Přiřazení tras spojům</param>
        /// <param name="calendarAssignment">Přiřazení kalendářů spojům</param>
        /// <param name="tripIdPersistentDb">Databáze ID spojů pro generování GTFS ID</param>
        public TripsTransformation(IDictionary<Route, GtfsModel.Extended.Route> routesTransformation, IDictionary<Stop, StopVariantsMapping> stopsTransformation, 
            IDictionary<MergedTripGroup, ShapeEx> shapeAssignment, IDictionary<MergedTripGroup, GtfsModel.Extended.CalendarRecord> calendarAssignment, TripIdPersistentDb tripIdPersistentDb)
        {
            this.routesTransformation = routesTransformation;
            this.stopsTransformation = stopsTransformation;
            this.shapeAssignment = shapeAssignment;
            this.calendarAssignment = calendarAssignment;
            this.tripIdPersistentDb = tripIdPersistentDb;
        }

        /// <summary>
        /// Vygeneruje GTFS Tripy a vrací mapování GTFS stop times na ASW návazné poznámky (kvůli pozdějšímu generování návazností)
        /// </summary>
        /// <param name="trips">ASW skupiny tripů (sloučených po identických)</param>
        /// <param name="stopTimesWithTimedTransferRemarks">Sem se uloží mapování návazných poznámek na GTFS stop times</param>
        public IDictionary<MergedTripGroup, GtfsModel.Extended.Trip> TransformTripsToGtfs(IEnumerable<MergedTripGroup> trips, out Dictionary<GtfsModel.Extended.StopTime, List<Remark>> stopTimesWithTimedTransferRemarks)
        {
            transformedTrips = new Dictionary<MergedTripGroup, GtfsModel.Extended.Trip>();
            stopTimesWithTimedTransferRemarks = new Dictionary<GtfsModel.Extended.StopTime, List<Remark>>();
            foreach (var trip in trips)
            {
                TransformAndAddGtfsTrip(trip, stopTimesWithTimedTransferRemarks);
            }

            return transformedTrips;
        }

        // Převede trip do GTFS, přidá ho do 'transformedTrips' a do 'stopTimesWithTimedTransferRemarks'.
        // Může se volat i rekurzivně kvůli prev/next spojům v blocích, validní je i trip = null (pak vrací null).
        // Každý trip se generuje jen jednou, pokud už byl vygenerován dříve, vrací se výsledek z cache 'transformedTrips'
        private GtfsModel.Extended.Trip TransformAndAddGtfsTrip(MergedTripGroup trip, Dictionary<GtfsModel.Extended.StopTime, List<Remark>> stopTimesWithTimedTransferRemarks)
        {
            if (trip == null)
                return null;

            if (transformedTrips.ContainsKey(trip))
                return transformedTrips[trip];

            var gtfsId = tripIdPersistentDb.GetGtfsIdForTrip(trip);
            var blockId = "";
            if (trip.PreviousPublicTripInBlock != null)
            {
                var firstTripInGtfs = TransformAndAddGtfsTrip(trip.FirstTripInBlock, stopTimesWithTimedTransferRemarks);
                blockId = firstTripInGtfs.GtfsId;
            }
            else if (trip.NextPublicTripInBlock != null)
            {
                // má následníky, ale nemá předchůdce => ergo je první v netriviálním blocku a určuje block id
                blockId = gtfsId;
            }

            var route = routesTransformation[trip.Route];

            var resultGtfsTrip = new GtfsModel.Extended.Trip()
            {
                BlockId = blockId,
                CalendarRecord = calendarAssignment[trip],
                DirectionId = trip.DirectionId == 0 ? Direction.Outbound : Direction.Inbound,
                GtfsId = gtfsId,
                Headsign = GetHeadsign(trip, trip.PublicStopTimes.Last().Stop),
                IsExceptional = trip.IsExceptional,
                PreviousTripInBlock = TransformAndAddGtfsTrip(trip.PreviousPublicTripInBlock, stopTimesWithTimedTransferRemarks),
                Route = route,
                Shape = shapeAssignment[trip],
                ShortName = "",
                SubAgency = trip.Agency != null ? route.SubAgencies.FirstOrDefault(a => a.SubAgencyId == trip.Agency.Id) : null, // může se stát, že dopravce není zadán, anebo jde o dopravce, který není na lince uveden v číselníku, pak ho nebudeme uvádět ani u spoje (ale teoreticky bychom mohli, jen nenalezneme příslušnou sub agency, museli bychom předávat rovnou číslo dopravce)
                WheelchairAccessible = trip.IsWheelchairAccessible ? GtfsModel.Enumerations.WheelchairAccessibility.Possible : GtfsModel.Enumerations.WheelchairAccessibility.NotPossible,
                // BikesAllowed nastavíme později podle StopTimes
                // NextPublicTripInBlock se nastaví, až se bude generovat onen následný trip (jinak bychom udělali nekonečnou rekurzi)
            };

            if (trip.PreviousPublicTripInBlock != null)
            {
                resultGtfsTrip.PreviousTripInBlock.NextTripInBlock = resultGtfsTrip;
            }

            // TODO co kdybychom ten první arrival a poslední departure nechali nevyplněné? bude to fungovat i s blocky?
            resultGtfsTrip.StopTimes = GenerateStopTimes(trip, resultGtfsTrip, stopTimesWithTimedTransferRemarks).ToList();
            resultGtfsTrip.StopTimes.First().ArrivalTime = resultGtfsTrip.StopTimes.First().DepartureTime;
            resultGtfsTrip.StopTimes.Last().DepartureTime = resultGtfsTrip.StopTimes.Last().ArrivalTime;
            resultGtfsTrip.BikesAllowed = bikeAllowanceDefinition.SetBikesAllowedForTrip(resultGtfsTrip.PublicStopTimes.Select(st => st.BikesAllowed));

            resultGtfsTrip.Route.Trips.Add(resultGtfsTrip);
            resultGtfsTrip.CalendarRecord.Trips.Add(resultGtfsTrip);
            transformedTrips.Add(trip, resultGtfsTrip);
            return resultGtfsTrip;
        }

        // generuje stoptimes, při tom jim doplňuje kilometráž ze vzorového shape a hlídá stop headsigns podle změn směrových orientací
        private IEnumerable<GtfsModel.Extended.StopTime> GenerateStopTimes(MergedTripGroup ownerTrip, GtfsModel.Extended.Trip gtfsTrip, 
            Dictionary<GtfsModel.Extended.StopTime, List<Remark>> stopTimesWithTimedTransferRemarks)
        {
            var gtfsStopTimeList = new List<GtfsModel.Extended.StopTime>();
            int sequenceNumber = 1;
            var shape = shapeAssignment[ownerTrip];
            for (int i = 0; i < ownerTrip.StopTimes.Length; i++)
            {
                // jdeme to for cyklem, protože potřebujeme číst z Shape.PointsForStopTimes a kdybychom něco (neveřejného) přeskočili, přestanou štimovat indexy...
                var stopTime = ownerTrip.StopTimes[i];
                if (stopTime.IsPublic)
                {
                    var currentNodeNumber = stopTime.Stop.NodeId;
                    var nextNodeNumbers = ownerTrip.StopTimes.Skip(i + 1).SkipWhile(st => st.Stop.NodeId == currentNodeNumber).Select(st => st.Stop.NodeId);
                    var prevStopTimeBikes = gtfsStopTimeList.LastOrDefault()?.BikesAllowed;

                    var gtfsStopTime = new GtfsModel.Extended.StopTime()
                    {
                        ArrivalTime = stopTime.ArrivalTime,
                        BikesAllowed = bikeAllowanceDefinition.GetBikesAllowedForStopTime(ownerTrip.TrafficType, ownerTrip.Route.LineName, stopTime.Remarks, currentNodeNumber, nextNodeNumbers, prevStopTimeBikes),
                        DepartureTime = stopTime.DepartureTime,
                        DropOffType = stopTime.BoardingOnly ? DropOffType.None : stopTime.IsRequestStop ? DropOffType.DriverRequest : DropOffType.Regular,
                        PickupType = stopTime.ExitOnly ? PickupType.None : stopTime.IsRequestStop ? PickupType.DriverRequest : PickupType.Regular,
                        SequenceNumber = sequenceNumber++,
                        Stop = stopsTransformation[stopTime.Stop].GetGtfsStop(ownerTrip.LineType),
                        Trip = gtfsTrip,
                        StopHeadsign = null, // pokud bude potřeba ho přepsat, přepisujeme až zpětně, viz níž
                        ShapeDistanceTraveledMeters = shape.PointsForStopTimes[i].DistanceTraveledMeters,
                        TripOperationType = stopTime.TripOperationType,
                    };

                    if (stopTime.DirectionChange)
                    {
                        // v této zastávce se mění čelní orientace, ergo všechny předchozí stoptimes mají jako headsign tuto zastávku;
                        // postupujeme směrem k první zastávce a zarazíme se, pokud už mají zastávky definovaný jiný stop headsign
                        // (tj. pokud bychom byli druhá či další změna směrové orientace)
                        var headsign = GetHeadsign(ownerTrip, stopTime.Stop);
                        foreach (var prevStopTime in Enumerable.Reverse(gtfsStopTimeList))
                        {
                            if (!string.IsNullOrEmpty(prevStopTime.StopHeadsign))
                            {
                                break; // již má headsign => konec
                            }

                            prevStopTime.StopHeadsign = headsign;
                        }
                    }

                    gtfsStopTimeList.Add(gtfsStopTime);

                    if (stopTime.Remarks.Any(st => st.IsTimedTransfer))
                    {
                        stopTimesWithTimedTransferRemarks.Add(gtfsStopTime, stopTime.Remarks.Where(st => st.IsTimedTransfer).ToList());
                    }
                }
            }

            return gtfsStopTimeList;
        }
        
        // vrátí headsign pro trip (správně zformuje název zastávky)
        private string GetHeadsign(MergedTripGroup trip, Stop headsignStop)
        {
            if (trip.TrafficType == AswTrafficType.Bus && headsignStop.MunicipalityName == "Praha"
                && (trip.LineType == AswLineType.SuburbanTransport || trip.LineType == AswLineType.RegionalTransport)) 
                    // ten RegionalTransport by tam asi nemusel být, bo tyhle linky nesmí do Prahy, ale kdyby to náhodou někdo v číselníku zvojtil, raději s tím budeme počítat
            {
                return $"Praha,{headsignStop.CommonName}";
            }
            else if (headsignStop.CommonName == "Letiště" || headsignStop.CommonName == "Terminál 1" || headsignStop.CommonName == "Terminál 2")
            {
                if (trip.Route.LineNumber == 100 && trip.ReferenceTrip.OwnerRun.Any(r => r.RunNumber == 1))
                {
                    return headsignStop.CommonName + " / Airport ✈";
                }
                return headsignStop.CommonName + " / Airport";
            }
            else
            {
                return headsignStop.CommonName;
            }
        }
    }
}
