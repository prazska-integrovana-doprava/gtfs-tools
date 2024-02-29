namespace AswModel.Extended.Logging
{
    class NullIgnoredTripsLogger : IIgnoredTripsLogger
    {
        public void Log(string reason, Trip obj)
        {
        }

        public void Close()
        {
        }
    }
}
