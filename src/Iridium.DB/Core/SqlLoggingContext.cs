using System;
using System.Collections.Generic;

namespace Iridium.DB
{
    public class SqlLoggingContext : IDisposable
    {
        private readonly ISqlDataProvider _dataProvider;
        private readonly List<TimedSqlLogEntry> _logEntries = new List<TimedSqlLogEntry>();
        private TimeSpan _totalTime = TimeSpan.Zero;
        
        public SqlLoggingContext(IDataProvider dataProvider, bool replaceParameters, Action<TimedSqlLogEntry> onLog)
        {
            _dataProvider = dataProvider as ISqlDataProvider;

            if (_dataProvider != null)
            {
                _dataProvider.SqlLogger = new SqlToTextLogger(entry =>
                {
                    onLog?.Invoke(entry);
                    lock (_logEntries)
                    {
                        _logEntries.Add(entry);
                        _totalTime = _totalTime.Add(entry.TimeTaken);
                    }
                }, replaceParameters);
            }
        }

        public IReadOnlyCollection<TimedSqlLogEntry> LogEntries => _logEntries;
        public TimeSpan TotalTime => _totalTime;

        public void Dispose()
        {
            _dataProvider.SqlLogger = null;
        }
    }
}