using System;
using System.Collections.Generic;
using System.Text;

namespace Iridium.DB.SqlServer
{
    public class SqlServerContext : DbContext
    {
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
