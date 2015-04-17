using System;
using System.Collections.Generic;
using System.Text;

namespace Velox.DB.MySql
{
    public class MySqlContext : Vx.Context
    {
        public static void Use(string connectionString)
        {
            Vx.DB = new MySqlContext(connectionString);
        }

        public MySqlContext() : base(new MySqlDataProvider())
        {
        }

        public MySqlContext(string dbFileName) : base(new MySqlDataProvider(dbFileName))
        {
        }

        public string ConnectionString
        {
            get { return ((MySqlDataProvider)DataProvider).ConnectionString; }
            set { ((MySqlDataProvider)DataProvider).ConnectionString = value; }
        }
    }
}
