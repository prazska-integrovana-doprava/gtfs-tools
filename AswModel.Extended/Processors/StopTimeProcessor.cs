using AswModel.Extended.Logging;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;
using System.Collections.Generic;
using System.Linq;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává jednotlivá zastavení <see cref="Zastaveni"/> spojů pro <see cref="TripProcessor"/>.
    /// Na rozdíl od ostatních processorů neukládá přímo do databáze, ale vrací v metodě process nadřazenému Trip Processoru, který si to pak zpracuje
    /// </summary>
    class StopTimeProcessor
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private ITrajectoryDbLogger trajLog = Loggers.TrajectoryDbLoggerInstance;
        private AswSingleFileFeed feedFile;
        private TheAswDatabase db;

        public StopTimeProcessor(AswSingleFileFeed feedFile, TheAswDatabase db)
        {
            this.feedFile = feedFile;
            this.db = db;
        }

        public StopTime Process(Zastaveni xmlStopTime, Trip ownerTrip, StopTime previousStopTime, bool inLoop)
        {
            if (xmlStopTime.CUzlu == 0 || xmlStopTime.CZast == 0) return null;
            if (xmlStopTime.Prijezd == -1 && xmlStopTime.Odjezd == -1) return null;

            var arrivalTime = new Time(xmlStopTime.Prijezd != -1 ? xmlStopTime.Prijezd - xmlStopTime.PrijezdPoPosunu * 3600 : xmlStopTime.Odjezd - xmlStopTime.OdjezdPoPosunu * 3600);
            var departureTime = new Time(xmlStopTime.Odjezd != -1 ? xmlStopTime.Odjezd - xmlStopTime.OdjezdPoPosunu * 3600 : xmlStopTime.Prijezd - xmlStopTime.PrijezdPoPosunu * 3600);
            if (previousStopTime != null && arrivalTime < previousStopTime.DepartureTime)
            {
                dataLog.Log(LogMessageType.WARNING_STOPTIME_PREMATURE_ARRIVAL, $"Zastavení s příjezdem {arrivalTime} následuje po odjezdu v {previousStopTime.DepartureTime} (spoj {ownerTrip}, cestování časem?). Provádím korekci času.", xmlStopTime);
                arrivalTime = previousStopTime.DepartureTime;
            }

            if (departureTime < arrivalTime)
            {
                dataLog.Log(LogMessageType.WARNING_STOPTIME_PREMATURE_DEPARTURE, $"Zastavení s odjezdem {departureTime} následuje po příjezdu v {arrivalTime} (spoj {ownerTrip}, cestování časem?). Provádím korekci času.", xmlStopTime);
                departureTime = arrivalTime;
            }

            var stop = FindStop(xmlStopTime, ownerTrip.TripId, ownerTrip.ServiceAsBits);
            if (stop == null) return null;

            if (ownerTrip.TripType != TripOperationType.Regular && xmlStopTime.CTypVyk == (int) TripOperationType.Regular)
            {
                // pokud je alespoň část spoje regulérní, je regulérní celý spoj
                ownerTrip.TripType = TripOperationType.Regular;
            }

            var remarks = ProcessRemarks(xmlStopTime.PozID, xmlStopTime);
            var boardingAllowed = xmlStopTime.Cestujici && !inLoop;

            var trackVariant = new ShapeFragmentDescriptor(ownerTrip.CompanyId, previousStopTime?.Stop, stop, xmlStopTime.VarTr);
            ShapeFragment trackToThisStop = null;
            if (previousStopTime != null && xmlStopTime.VarTr != 0)
            {
                trackToThisStop = ProcessTrack(trackVariant, ownerTrip, 
                    previousStopTime.BoardingAllowed && boardingAllowed && !previousStopTime.IsDepot && !xmlStopTime.DeponovaciMisto && ownerTrip.IsPublic);
            }

            return new StopTime()
            {
                Trip = ownerTrip,
                ArrivalTime = arrivalTime,
                DepartureTime = departureTime,
                Stop = stop,
                IsRequestStop = xmlStopTime.NaZnameni,
                TrackVariantDescriptor = trackVariant,
                TrackToThisStop = trackToThisStop,
                DirectionChange = xmlStopTime.ZmenaSmeruOkruzniLinky,
                Remarks = remarks.ToArray(),
                BoardingAllowed = boardingAllowed,
                IsDepot = xmlStopTime.DeponovaciMisto,
                TripOperationType = (TripOperationType) xmlStopTime.CTypVyk,
            };
        }

        private Stop FindStop(Zastaveni xmlStopTime, int ownerTrip, ServiceDaysBitmap serviceAsBits)
        {
            if (xmlStopTime.CUzlu == 0 && xmlStopTime.CZast == 0)
            {
                // se občas objeví, těžko říct, co to znamená
                return null;
            }

            Stop stop;
            if (!db.Stops.FindOrDefault(StopDatabase.CreateKey(xmlStopTime.CUzlu, xmlStopTime.CZast), serviceAsBits, out stop))
            {
                if (stop != null)
                {
                    dataLog.Log(LogMessageType.INFO_STOPTIME_MISSING_STOP_VERSION, $"Zastavení odkazuje na zastávku {xmlStopTime.CUzlu}/{xmlStopTime.CZast}, která nemá verzi pro platnost {serviceAsBits} spoje {ownerTrip}. Používám verzi zastávky platnou v {stop.ServiceAsBits}.", xmlStopTime);
                }
                else
                {
                    dataLog.Log(LogMessageType.ERROR_STOPTIME_MISSING_STOP, $"Zastavení odkazuje na zastávku {xmlStopTime.CUzlu}/{xmlStopTime.CZast}, která neexistuje: (využito spojem {ownerTrip}). Ignoruji záznam.", xmlStopTime);
                    return null;
                }
            }

            if (stop.Position.GpsLatitude == 0.0 || stop.Position.GpsLongitude == 0.0)
            {
                if (stop.IsPublic)
                {
                    dataLog.Log(LogMessageType.ERROR_STOP_ZERO_COORDINATES, $"Zastavení odkazuje na zastávku {stop} s nulovou některou ze souřadnic (využito spojem {ownerTrip})", xmlStopTime);
                }

                return null; // Google zastávku s nulovou souřadnicí nesnese, musíme ji tedy vyřadit
            }
            
            return stop;
        }

        // načte poznámky z tagu "po"; může jich být více oddělených mezerou
        private IEnumerable<Remark> ProcessRemarks(List<int> remarkIds, Zastaveni xmlStopTime)
        {
            foreach (var remarkId in remarkIds)
            {
                Remark remark;
                if (!feedFile.Remarks.TryGetValue(remarkId, out remark))
                {
                    dataLog.Log(LogMessageType.ERROR_STOPTIME_MISSING_REMARK, $"Zastavení odkazuje na poznámku {remarkId}, která neexistuje", xmlStopTime);
                    continue;
                }

                yield return remark;
            }
        }

        private ShapeFragment ProcessTrack(ShapeFragmentDescriptor descriptor, Trip trip, bool isOnPublicPart)
        {
            ShapeFragment fragment;
            if (db.ShapeFragments.FindOrDefault(descriptor, trip.ServiceAsBits, out fragment))
            {
                return fragment;
            }
            else if (fragment != null)
            {
                // existuje trasa, ale bitmapově neodpovídá
                trajLog.LogPartiallyMissing(descriptor, trip);
                fragment.ServiceAsBits = db.ShapeFragments.Find(descriptor).ExtendBitmapToMaximum(fragment);
                return fragment;
            }
            else
            {
                // trasa vůbec neexistuje
                if (isOnPublicPart) // TODO formálně to není úplně dobře, kdyby bylo neveřejné zastavení uprostřed spoje, může nám ta trasa chybět (ale běžně se to asi neděje, spíš se to dělá neveřejnou zastávkou a to nám tady neva)
                {
                    trajLog.LogMissing(descriptor, trip);
                }

                return null;
            }
        }
    }
}
