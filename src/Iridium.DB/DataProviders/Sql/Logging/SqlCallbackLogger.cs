using System;
using System.Collections.Generic;

namespace Iridium.DB
{
    public class SqlCallbackLogger : ISqlLogger
    {
        private readonly Action<string, Dictionary<string, object>, TimeSpan> _callback;

        public SqlCallbackLogger(Action<string, Dictionary<string, object>, TimeSpan> callback)
        {
            _callback = callback;
        }

        public void LogSql(string sql, Dictionary<string, object> parameters, TimeSpan timeTaken)
        {
            _callback(sql, parameters, timeTaken);
        }
    }
}