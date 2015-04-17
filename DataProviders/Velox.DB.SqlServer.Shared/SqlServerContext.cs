using System;
using System.Collections.Generic;
using System.Text;

namespace Velox.DB.SqlServer
{
    public class SqlServerContext : Vx.Context
    {
        public static void Use(string connectionString)
        {
            Vx.DB = new SqlServerContext(connectionString);
        }

        public SqlServerContext() : base(new SqlServerDataProvider())
        {
        }

        public SqlServerContext(string dbFileName) : base(new SqlServerDataProvider(dbFileName))
        {
        }

        public string ConnectionString
        {
            get { return ((SqlServerDataProvider)DataProvider).ConnectionString; }
            set { ((SqlServerDataProvider)DataProvider).ConnectionString = value; }
        }
    }
}
