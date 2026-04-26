namespace JdfToGtfsProcessor
{
    public class AppSettings
    {
        public string? JdfFolder { get; set; }
        public string? StopDataFile { get; set; }
        public string? LogFolder { get; set; }
        public string? OutputFolder { get; set; }
        public string? TrainGtfsFolder { get; set; }
        public string? BusToBusTransfersFile { get; set; }
        public string? TrainToBusTransfersFile { get; set; }
    }
}
