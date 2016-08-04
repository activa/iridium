using System;
using System.Collections.Generic;
using System.Text;
using Iridium.DB;

namespace Iridium.DB.MySql
{
    public class MySqlContext : DbContext
    {
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
