namespace GtfsLogging
{
    /// <summary>
    /// Nejjednodušší logger, prostě píše do souboru řádky tak jak přicházejí
    /// </summary>
    public interface ISimpleLogger : ILogger
    {
        void Log(string text);
    }
}
