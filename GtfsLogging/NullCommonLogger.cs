namespace GtfsLogging
{
    public class NullCommonLogger : ICommonLogger
    {
        public bool Assert(bool condition, LogMessageType messageType, string message, object obj = null)
        {
            return true;
        }

        public void Log(LogMessageType messageType, string message, object obj = null)
        {
        }

        public void Close()
        {
        }
    }
}
