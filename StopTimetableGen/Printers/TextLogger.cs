using StopTimetableGen.StopTimetableModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StopTimetableGen.Printers
{
    /// <summary>
    /// Víceméně testovací třída pro ladění, dělá textový dump <see cref="LineTimetables"/>.
    /// </summary>
    class TextLogger
    {
        private LineTimetables lineTimetables;
        private TextWriter writer;

        public TextLogger(LineTimetables lineTimetables)
        {
            this.lineTimetables = lineTimetables;
        }

        public void LogToFile()
        {
            writer = new StreamWriter($"{lineTimetables.LineNumber}.log");
            var directions = lineTimetables.StopTimetables.Select(st => st.Direction).Distinct();
            foreach (var direction in directions)
            {
                var stopTimetables = lineTimetables.StopTimetables.Where(st => st.Direction == direction);
                LogDirection(direction, stopTimetables);
                writer.WriteLine();
            }

            writer.Close();
        }

        private void LogDirection(int direction, IEnumerable<StopTimetable> stopTimetables)
        {
            foreach (var stopTimetable in stopTimetables)
            {
                writer.WriteLine(stopTimetable);
                foreach (var stop in stopTimetable.Stops)
                {
                    writer.WriteLine(stop);
                }

                writer.WriteLine();

                foreach (var weekdaySubset in stopTimetable.DayColumns)
                {
                    writer.WriteLine($"- Stop {stopTimetable.SrcStopName} on {weekdaySubset}");
                    for (int i = stopTimetable.FirstHour; i <= stopTimetable.LastHour; i++)
                    {
                        var hour = i % 24;
                        writer.Write($"   {hour,2} | ");
                        foreach (var departure in weekdaySubset.Hours[hour])
                        {
                            writer.Write($" {departure}");
                        }

                        writer.WriteLine();
                    }

                    writer.WriteLine();
                }
            }
        }
    }
}
