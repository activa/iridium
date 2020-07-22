using System;
using System.Collections.Generic;

namespace Iridium.DB
{
    public interface ISqlLogger
    {
        void LogSql(string sql, Dictionary<string, object> parameters, TimeSpan timeTaken);
    }
}