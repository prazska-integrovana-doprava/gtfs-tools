using AswModel.Extended.Logging;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;
using System.Collections.Generic;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává zastávky <see cref="Zastavka"/> z ASW JŘ a ukládá je do databáze <see cref="TheAswDatabase.Stops"/>. Resolvuje případné duplicity se záznamy, které už v databázi jsou.
    /// </summary>
    class StopProcessor : IProcessor<Zastavka>
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private TheAswDatabase db;

        public StopProcessor(TheAswDatabase db)
        {
            this.db = db;
        }

        public void Process(Zastavka xmlStop)
        {
            var serviceAsBits = ServiceDaysBitmap.FromBitmapString(xmlStop.KJ);
            if (serviceAsBits.IsEmpty)
                return;

            var zoneIds = new List<ZoneInfo>();
            AddZone(zoneIds, xmlStop.IDS, xmlStop.TarifniPasma, xmlStop);
            AddZone(zoneIds, xmlStop.IDS2, xmlStop.TarifniPasma2, xmlStop);
            AddZone(zoneIds, xmlStop.IDS3, xmlStop.TarifniPasma3, xmlStop);

            var stopRecord = new Stop()
            {
                NodeId = xmlStop.CUzlu,
                StopId = xmlStop.CZast,
                ServiceAsBits = serviceAsBits,
                PlatformCode = xmlStop.Stanoviste,
                Name = xmlStop.Nazev,
                CommonName = xmlStop.Nazev2,
                IdosName = xmlStop.Nazev7,
                Position = new Coordinates()
                {
                    JtskX = xmlStop.sX,
                    JtskY = xmlStop.sY,
                    GpsLatitude = xmlStop.Lat,
                    GpsLongitude = xmlStop.Lng,
                },
                MunicipalityName = xmlStop.NazevObce,
                WheelchairAccessibility = xmlStop.BezbarierovostNestanovena ? WheelchairAccessibility.Undefined 
                                        : (xmlStop.Bezbarierova ? WheelchairAccessibility.Accessible 
                                        : (xmlStop.BezbarierovaCastecne ? WheelchairAccessibility.PartiallyAccessible 
                                        : WheelchairAccessibility.NotAccessible)),
                CisNumber = xmlStop.CisloCIS,
                OisNumber = xmlStop.CisloOIS,
                RegionCode = xmlStop.Kraj,
                IsPublic = xmlStop.Verejna,
                Zones = zoneIds.ToArray(),
            };

            stopRecord.ZoneRegionType = GetZoneRegionType(stopRecord);

            if (stopRecord.IsPublic && (stopRecord.Position.GpsLatitude == 0.0 || stopRecord.Position.GpsLongitude == 0.0))
            {
                // může generovat false alarmy, pokud má zastávka více záznamů platnosti a některý z nich nemá souřadnici, ale není použitý (pak generuje warning, ale ten nevadí)
                dataLog.Log(LogMessageType.WARNING_STOP_USED_ZERO_COORDINATES, $"Zastávka {stopRecord} má nulovou některou ze souřadnic, přesto že je použitá.");
            }

            if (stopRecord.Name.EndsWith(" LD"))
            {
                stopRecord.Name = xmlStop.Nazev6;
                stopRecord.CommonName = xmlStop.Nazev6;
            }
            
            if (!db.Stops.AddOrMergeVersion(StopDatabase.CreateKey(stopRecord.NodeId, stopRecord.StopId), stopRecord, AreStopsEqual, null))
            {
                dataLog.Log(LogMessageType.ERROR_STOP_CONFLICTED_RECORDS, $"Zastávka {stopRecord} má konfliktní záznamy. Záznam s platností {stopRecord.ServiceAsBits} bude ignorován.");
            }
        }

        private void AddZone(List<ZoneInfo> zoneInfo, int idsId, string zoneId, Zastavka xmlStop)
        {
            var tariffSystem = db.TariffSystems.GetValueOrDefault(idsId);
            if (tariffSystem != null)
            {
                zoneInfo.Add(new ZoneInfo() { TariffSystemShortName = tariffSystem, ZoneId = zoneId });
            }
            else
            {
                dataLog.Log(LogMessageType.WARNING_STOP_IDS_NOT_FOUND, $"IDS {idsId} není definováno.", xmlStop);
            }
        }

        private ZoneRegionType GetZoneRegionType(Stop aswStop)
        {
            if (aswStop.PidZoneId == "-")
            {
                return ZoneRegionType.StopOutsidePidNotAvailable;
            }

            dataLog.Assert(!string.IsNullOrWhiteSpace(aswStop.RegionCode), LogMessageType.WARNING_STOP_REGION_INVALID, "Zastávka nemá zadaný kraj", aswStop);

            switch (aswStop.RegionCode)
            {
                case "A":
                    return ZoneRegionType.StopInPrague;

                case "S":
                    return ZoneRegionType.StopInCentralBohemia;

                case "K":
                case "P":
                case "C":
                    return ZoneRegionType.StopOutsideInPid;

                default:
                    return ZoneRegionType.StopOutsidePidLimited;
            }
        }

        private bool AreStopsEqual(Stop first, Stop second)
        {
            return first.Name == second.Name && first.PidZoneId == second.PidZoneId && first.Position.Equals(second.Position)
                && first.PlatformCode == second.PlatformCode && first.IsPublic == second.IsPublic;
        }
    }
}
