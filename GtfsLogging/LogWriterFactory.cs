using System;
using System.IO;

namespace GtfsLogging
{
    /// <summary>
    /// Slouží ke zjednodušené tvorbě TextWriterů do souboru
    /// </summary>
    public class LogWriterFactory
    {
        public string _logPath;

        public LogWriterFactory(string logPath)
        {
            _logPath = logPath;
        }

        /// <summary>
        /// Vytvoří instanci <see cref="StreamWriter"/> pro zápis do souboru v zadané logovací složce
        /// </summary>
        /// <param name="loggerName"></param>
        /// <returns></returns>
        public TextWriter CreateWriterToFile(string loggerName)
        {
            var writer = new StreamWriter(Path.Combine(_logPath, $"{DateTime.Now:yyyyMMdd_HHmm}_{loggerName}.txt"));
            return writer;
        }
    }
}
