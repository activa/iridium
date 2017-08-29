using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using Iridium.DB;
using Iridium.DB.MySql;
using Iridium.DB.Postgres;
using Iridium.DB.SqlServer;

namespace Iridium.DB.Test
{
    public class SERVERS
    {
        public const string SQLSERVER = "192.168.1.100";
        public const string MYSQL = "192.168.1.100";
        public const string POSTGRES = "192.168.1.32";
    }


    public class DBContext : StorageContext
    {

        public IDataSet<Order> Orders { get; set; }
        public IDataSet<Customer> Customers { get; set; }
        public IDataSet<OrderItem> OrderItems { get; set; }
        public IDataSet<SalesPerson> SalesPeople { get; set; }
        public IDataSet<Product> Products;
        public IDataSet<PaymentMethod> PaymentMethods { get; set; }
        public IDataSet<CustomerPaymentMethodLink> CustomerPaymentMethodLinks { get; set; }
        public IDataSet<RecordWithAllTypes> RecordsWithAllTypes;
        public IDataSet<RecordWithCompositeKey> RecordsWithCompositeKey;
        public IDataSet<RecordWithIgnoredFields> RecordsWithIgnoredFields;
        public IDataSet<RecordWithInterface> RecordsWithInterface;

        public DBContext(IDataProvider dataProvider) : base(dataProvider)
        {
            /*
            Ir.Config.NamingConvention = new NamingConvention()
            {
                PrimaryKeyName = "ID",
                OneToManyKeyName = NamingConvention.RELATION_CLASS_NAME + "ID",
                ManyToOneKeyName = NamingConvention.RELATION_CLASS_NAME + "ID"
            };
             */
        }

        public void PurgeAll()
        {
            Customers.Purge();
            OrderItems.Purge();
            Orders.Purge();
            OrderItems.Purge();
            SalesPeople.Purge();
            Products.Purge();
            PaymentMethods.Purge();
            CustomerPaymentMethodLinks.Purge();
            RecordsWithAllTypes.Purge();
            RecordsWithCompositeKey.Purge();
            RecordsWithIgnoredFields.Purge();
            

        }

        public void CreateAllTables()
        {
            CreateTable<Product>(recreateTable: true);
            CreateTable<Order>(recreateTable: true);
            CreateTable<Customer>(recreateTable: true);
            CreateTable<OrderItem>(recreateTable: true);
            CreateTable<SalesPerson>(recreateTable: true);
            CreateTable<PaymentMethod>(recreateTable: true);
            CreateTable<CustomerPaymentMethodLink>(recreateTable: true);
            CreateTable<RecordWithAllTypes>(recreateTable: true);
            CreateTable<RecordWithCompositeKey>(recreateTable: true);
            CreateTable<OneToOneRec1>(recreateTable:true);
            CreateTable<OneToOneRec2>(recreateTable:true);
            CreateTable<RecordWithIgnoredFields>(recreateTable: true);
            CreateTable<RecordWithInterface>(recreateTable: true);
        }

        private static Dictionary<string, Func<DBContext>> _contextFactories;
        private static Dictionary<string,DBContext> _contexts = new Dictionary<string, DBContext>();

        static DBContext()
        {
            _contextFactories = new Dictionary<string,Func<DBContext>>()
            {
                { "sqlite", () => new SqliteStorage() },
                { "sqlitemem", () => new SqliteMemStorage() },
                { "sqlserver", () => new SqlServerStorage() },
                { "mysql", () => new MySqlStorage() },
                { "postgres", () => new PostgresStorage() },
                { "memory", () => new MemoryStorage() }
            };

        }

        public static DBContext Get(string driver)
        {
            if (_contexts.ContainsKey(driver))
                return _contexts[driver];

            _contexts[driver] = _contextFactories[driver]();

            return _contexts[driver];
        }
    }

    public class MySqlStorage : DBContext
    {
        public MySqlStorage() : base(new MySqlDataProvider($"Server={SERVERS.MYSQL};Database=velox;UID=velox;PWD=velox")) { }
    }

    public class PostgresStorage : DBContext
    {
        public PostgresStorage() : base(new PostgresDataProvider($"Host={SERVERS.POSTGRES};Database=velox;Username=velox;Password=velox")) { }
    }

    public class SqlServerStorage : DBContext
    {
        public SqlServerStorage() : base(new SqlServerDataProvider($"Server={SERVERS.SQLSERVER};Database=velox;UID=velox;PWD=velox")) { }
    }

    public class SqliteStorage : DBContext
    {
        public SqliteStorage() : base(new SqliteDataProvider("velox.sqlite")) { }
    }

    public class SqliteMemStorage : DBContext
    {
        public SqliteMemStorage() : base(new SqliteDataProvider(":memory:")) { }
    }



    public class MemoryStorage : DBContext
    {
        public MemoryStorage() : base(new MemoryDataProvider()) { }
    }
}
