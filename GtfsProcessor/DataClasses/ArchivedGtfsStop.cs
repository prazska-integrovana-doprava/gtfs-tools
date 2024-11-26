using CsvSerializer.Attributes;
using GtfsModel;
using System;

namespace GtfsProcessor.DataClasses
{
    class ArchivedGtfsStop : GtfsStop
    {
        [CsvField("archived_until", 91)]
        public DateTime ArchivedUntil { get; set; }

        public ArchivedGtfsStop()
        {
        }

        public ArchivedGtfsStop(GtfsStop stopData, DateTime archivedUntil)
            : base(stopData)
        {
            ArchivedUntil = archivedUntil;
        }
    }
}
