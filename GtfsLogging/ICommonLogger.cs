namespace GtfsLogging
{
    /// <summary>
    /// Zaznamenává kategorizované chyby ze škály <see cref="LogMessageType"/>, navíc k nim umí vždy dát nějaký objekt jako parametr.
    /// </summary>
    public interface ICommonLogger : ILogger
    {
        /// <summary>
        /// Zaznamená chybu nebo zprávu
        /// </summary>
        /// <param name="message">Text chyby</param>
        void Log(LogMessageType messageType, string message, object obj = null);

        /// <summary>
        /// Ověří, že daná podmínka platí a pokud ne, zaloguje chybu/varování se zadanou zprávou.
        /// </summary>
        /// <param name="condition">Podmínka, která má platit</param>
        /// <param name="message">Zpráva k zalogování, pokud podmínka neplatí</param>
        /// <returns>True, pokud je podmínka platná, jinak false.</returns>
        bool Assert(bool condition, LogMessageType messageType, string message, object obj = null);
    }
}
