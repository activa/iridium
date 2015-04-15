using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;
using Velox.DB.Core;

namespace Velox.DB.Sql.Sqlite
{
    public class SqliteDataProvider : SqlAdoDataProvider<SqliteConnection, SqliteDialect>
    {
        private readonly string _longTextType = "TEXT";

        public SqliteDataProvider(string connectionString) : base(connectionString)
        {
        }

        public SqliteDataProvider(string fileName, bool useDateTimeTicks)
            : this("Data Source=" + fileName + ";DateTimeFormat=" + (useDateTimeTicks ? "Ticks" : "ISO8601"))
        {
        }



        public override void ClearConnectionPool()
        {
            SqliteConnection.ClearAllPools();
        }
    }
}