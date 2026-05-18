using CommonLibrary;
using GtfsModel.Extended;

namespace JdfToGtfsProcessor.Stops
{
    /// <summary>
    /// Vytváří GTFS zastávky pro dané stanoviště
    /// (hodí se, protože jich můžeme potřebovat vytvořit víc pro různá pásma, viz <see cref="StopDatabase"/>)
    /// </summary>
    class GtfsStopFactory
    {
        public string? GtfsId { get; set; }

        public string? Name { get; set; }

        public int CisId { get; set; }

        public GpsCoordinates Position { get; set; }

        public string? PlatformCode { get; set;}

        public Stop CreateStop()
        {
            return new Stop()
            {
                GtfsId = GtfsId,
                Name = Name,
                CisId = CisId,
                Position = Position,
                PlatformCode = PlatformCode
            };
        }
    }
}
