using System;
using System.Collections.Generic;
using System.Text;
using Iridium.DB;

namespace Iridium.DB.Postgres
{
    public class PostgresContext : StorageContext
    {
        public PostgresContext() : base(new PostgresDataProvider())
        {
        }

        public PostgresContext(string connectionString) : base(new PostgresDataProvider(connectionString))
        {
        }

        public string ConnectionString
        {
            get { return ((PostgresDataProvider)DataProvider).ConnectionString; }
            set { ((PostgresDataProvider)DataProvider).ConnectionString = value; }
        }
    }
}
