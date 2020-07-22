using System;

namespace Iridium.DB
{
    public class TimedSqlLogEntry
    {
        public TimedSqlLogEntry()
        {
        }

        public TimedSqlLogEntry(TimeSpan timeTaken, string sql)
        {
            TimeTaken = timeTaken;
            Sql = sql;
        }

        public TimeSpan TimeTaken { get; set; }
        public string Sql { get; set; }
    }
}