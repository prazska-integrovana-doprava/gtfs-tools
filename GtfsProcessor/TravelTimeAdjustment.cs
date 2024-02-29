using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AswModel.Extended;
using CsvSerializer;
using CsvSerializer.Attributes;

namespace GtfsProcessor
{
    class TravelTimeAdjustment
    {
        [CsvField("from_node_id", 1)]
        public int FromNodeId { get; set; }

        [CsvField("from_stop_id", 2)]
        public int FromStopId { get; set; }

        [CsvField("to_node_id", 3)]
        public int ToNodeId { get; set; }

        [CsvField("to_stop_id", 4)]
        public int ToStopId { get; set; }

        [CsvField("traffic_type", 5)]
        public int TrafficTypeAsw { get; set; }

        [CsvField("orig_value", 6)]
        public int ValueToAdjustSeconds { get; set; }

        [CsvField("replacement", 7)]
        public int ValueToBeSetSeconds { get; set; }

        public static void Proceed(TheAswDatabase db)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\replacements.csv");
            var adjustments = CsvFileSerializer.DeserializeFile<TravelTimeAdjustment>(path);
            if (adjustments.Any())
            {
                Console.WriteLine("Koriguji jízdní doby dle replacements.csv");
            }
            else
            {
                Console.WriteLine($"{path} prázdný/nenalezen...");
            }

            foreach (var adjustment in adjustments)
            {
                foreach (var trip in db.GetAllPublicTrips())
                {
                    if ((int) trip.Route.TrafficType != adjustment.TrafficTypeAsw)
                        continue;

                    StopTime prevStopTime = null;
                    foreach (var stopTime in trip.PublicStopTimes)
                    {
                        if (prevStopTime != null && prevStopTime.Stop.NodeId == adjustment.FromNodeId && prevStopTime.Stop.StopId == adjustment.FromStopId
                            && stopTime.Stop.NodeId == adjustment.ToNodeId && stopTime.Stop.StopId == adjustment.ToStopId
                            && (stopTime.ArrivalTime - prevStopTime.DepartureTime) == adjustment.ValueToAdjustSeconds)
                        {
                            stopTime.ArrivalTime = prevStopTime.DepartureTime.AddSeconds(adjustment.ValueToBeSetSeconds);
                            if (stopTime.ArrivalTime > stopTime.DepartureTime)
                            {
                                stopTime.DepartureTime = stopTime.ArrivalTime;
                            }
                        }

                        prevStopTime = stopTime;
                    }
                }
            }
        }
    }
}
