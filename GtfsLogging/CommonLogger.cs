using System.Collections.Generic;
using System.IO;

namespace GtfsLogging
{
    /// <summary>
    /// Zaznamenává kategorizované chyby ze škály <see cref="LogMessageType"/>, navíc k nim umí vždy dát nějaký objekt jako parametr.
    /// </summary>
    public class CommonLogger : BaseTextLogger, ICommonLogger
    {
        /// <summary>
        /// U loggerů, které podporují <see cref="LogMessageType"/> se v logu zobrazí od každého
        /// typu nejvýše tolik zpráv, kolik udává tato hodnota.
        /// </summary>
        public int MaxSimilarLogRecords { get; private set; }

        private TextWriter _writerForErrors;
        private readonly Dictionary<LogMessageType, int> _messageTypeCounts;

        public CommonLogger(TextWriter writer, int maxSimilarLogRecords = int.MaxValue, TextWriter writerForErrors = null)
            : base(writer)
        {
            MaxSimilarLogRecords = maxSimilarLogRecords;
            _writerForErrors = writerForErrors;
            _messageTypeCounts = new Dictionary<LogMessageType, int>();
        }

        public void Log(LogMessageType messageType, string message, object obj = null)
        {
            if (!_messageTypeCounts.ContainsKey(messageType))
            {
                _messageTypeCounts.Add(messageType, 0);
            }

            if (_messageTypeCounts[messageType] < MaxSimilarLogRecords)
            {
                var outputStr = $"{messageType}: {message}" + (obj != null ? $" {obj}" : "");
                Writer.WriteLine(outputStr);
                if (LogMessageTypeHelper.IsError(messageType) && _writerForErrors != null)
                {
                    _writerForErrors.WriteLine(outputStr);
                }
            }
            else if (_messageTypeCounts[messageType] == MaxSimilarLogRecords)
            {
                Writer.WriteLine($"   + další instance chyby {messageType} (nezobrazuje se pro vysoký počet výskytů).");
            }
        }

        public bool Assert(bool condition, LogMessageType messageType, string message, object obj = null)
        {
            if (!condition)
            {
                Log(messageType, message, obj);
            }

            return condition;
        }
    }
}
