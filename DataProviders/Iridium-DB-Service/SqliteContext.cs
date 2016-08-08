using System;
using Iridium.DB;

namespace Iridium.DB.SqlService
{
    public class SqlServiceContext : StorageContext
    {
        /*
        public static void Use(string dbFileName)
        {
            Ir.DB = new SqlServiceContext(dbFileName);
        }*/

        public SqlServiceContext() : base(new SqlServiceDataProvider())
        {
        }

        public SqlServiceContext(string server, string login, string password) : base(new SqlServiceDataProvider(server,login,password))
        {
        }
    }
}