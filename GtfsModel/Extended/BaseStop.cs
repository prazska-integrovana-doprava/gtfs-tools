using CommonLibrary;
using GtfsModel.Enumerations;
using GtfsModel.Functions;
using System;
using System.Collections.Generic;

namespace GtfsModel.Extended
{
    /// <summary>
    /// Rozhraní pro typy zastávek, které mohou mít parent station
    /// </summary>
    public interface IStopWithParent
    {
        Station ParentStation { get; set; }
    }

    /// <summary>
    /// Rozhraní pro všechno, co se nakonec umí tvářit jako GTFS stop záznam - záznamy ze stops.txt (nadstavba nad <see cref="GtfsStop"/>).
    /// </summary>
    public abstract class BaseStop
    {
        /// <summary>
        /// GTFS identifikátor záznamu
        /// </summary>
        public string GtfsId { get; set; }

        /// <summary>
        /// Poloha zastávky na mapě
        /// </summary>
        public GpsCoordinates Position { get; set; }

        /// <summary>
        /// Typ zastávky
        /// </summary>
        public abstract LocationType LocationType { get; }

        /// <summary>
        /// Vytvoří info o zastávce / stanici / ... z GTFS záznamu.
        /// </summary>
        /// <param name="gtfsStop">GTFS záznam</param>
        /// <param name="stopDbWithStations">Databáze zastávek. Při načítání stanice není potřeba, při načítání ostatních bodů však potřeba být může kvůli dohledání parent station.</param>
        public static BaseStop Construct(GtfsStop gtfsStop, IDictionary<string, BaseStop> stopDbWithStations)
        {
            switch (gtfsStop.LocationType)
            {
                case LocationType.Station:
                    return new Station()
                    {
                        GtfsId = gtfsStop.Id,
                        Name = gtfsStop.Name,
                        Position = new GpsCoordinates(gtfsStop.Latitude, gtfsStop.Longitude),
                        WheelchairBoarding = gtfsStop.WheelchairBoarding,
                        ZoneId = gtfsStop.ZoneId,
                        AswNodeId = gtfsStop.AswNodeId,
                        ZoneRegionType = gtfsStop.ZoneRegionType,
                    };

                case LocationType.Stop:
                    var stop = new Stop()
                    {
                        GtfsId = gtfsStop.Id,
                        Name = gtfsStop.Name,
                        ParentStation = (Station)stopDbWithStations.GetValueOrDefault(gtfsStop.ParentStationId),
                        PlatformCode = gtfsStop.PlatformCode,
                        Position = new GpsCoordinates(gtfsStop.Latitude, gtfsStop.Longitude),
                        WheelchairBoarding = gtfsStop.WheelchairBoarding,
                        ZoneId = gtfsStop.ZoneId,
                        AswNodeId = gtfsStop.AswNodeId,
                        AswStopId = gtfsStop.AswStopId,
                        ZoneRegionType = gtfsStop.ZoneRegionType,
                    };

                    IdentifierManagement.ParseStopId(stop);
                    return stop;

                case LocationType.Entrance:
                    return new StationEntrance()
                    {
                        GtfsId = gtfsStop.Id,
                        Name = gtfsStop.Name,
                        ParentStation = (Station)stopDbWithStations.GetValueOrDefault(gtfsStop.ParentStationId),
                        Position = new GpsCoordinates(gtfsStop.Latitude, gtfsStop.Longitude),
                        WheelchairBoarding = gtfsStop.WheelchairBoarding,
                    };

                case LocationType.GenericNode:
                    return new GenericNode()
                    {
                        GtfsId = gtfsStop.Id,
                        ParentStation = (Station)stopDbWithStations.GetValueOrDefault(gtfsStop.ParentStationId),
                        Position = new GpsCoordinates(gtfsStop.Latitude, gtfsStop.Longitude),
                    };

                case LocationType.BoardingArea:
                    return new BoardingArea()
                    {
                        GtfsId = gtfsStop.Id,
                        ParentPlatform = (Stop)stopDbWithStations.GetValueOrDefault(gtfsStop.ParentStationId),
                        Position = new GpsCoordinates(gtfsStop.Latitude, gtfsStop.Longitude),
                    };

                default:
                    throw new ArgumentException($"Unsupported Location type {gtfsStop.LocationType} of stop {gtfsStop.Id}");
            }
        }

        public abstract GtfsStop ToGtfsStop();
    }
}
