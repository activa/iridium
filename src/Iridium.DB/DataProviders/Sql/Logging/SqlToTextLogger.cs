using System;
using System.Collections.Generic;
using System.Text;
using Iridium.Reflection;

namespace Iridium.DB
{
    public class SqlToTextLogger : ISqlLogger
    {
        private readonly Action<TimedSqlLogEntry> _callback;
        private readonly bool _replaceParameters;

        public SqlToTextLogger(Action<TimedSqlLogEntry> callback, bool replaceParameters = false)
        {
            _callback = callback;
            _replaceParameters = replaceParameters;
        }

        public void LogSql(string sql, Dictionary<string, object> parameters, TimeSpan timeTaken)
        {
            StringBuilder s = new StringBuilder(sql);

            if (parameters != null && parameters.Count > 0)
            {
                if (!_replaceParameters)
                    s.Append(" - ");

                List<string> paramList = new List<string>();

                foreach (var param in parameters)
                {
                    string paramValue;
                    var typeInspector = param.Value?.GetType().Inspector();

                    if (typeInspector == null)
                    {
                        paramValue = "null";
                    }
                    else
                    {
                        if (typeInspector.Is(TypeFlags.String))
                            paramValue = $"'{param.Value}'";
                        else
                            paramValue = param.Value.ToString();
                    }

                    if (_replaceParameters)
                    {
                        s.Replace(param.Key, paramValue);
                        continue;
                    }

                    paramList.Add($"{param.Key}={paramValue}");
                }

                if (!_replaceParameters)
                    s.Append(string.Join(" , ", paramList));
            }

            _callback(new TimedSqlLogEntry(timeTaken, s.ToString()));
        }
    }
}