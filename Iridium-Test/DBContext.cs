using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using Iridium.DB;
using Iridium.DB.MySql;
using Iridium.DB.Postgres;
using Iridium.DB.SqlServer;
using NUnit.Framework;

/*
using Iridium.DB.MySql;
using Iridium.DB.Postgres;
using Iridium.DB.SqlServer;
*/

namespace Iridium.DB.Test
{

    public static class Program
    {
        public static void Main()
        {
        }
    }

    public static class TestConfiguration
    {
        public static IEnumerable<TestFixtureData> FixtureSource()
        {
            if (!string.IsNullOrEmpty(TestContext.Parameters["db"]))
            {
                string[] databases = TestContext.Parameters["db"].Split('/');

                return databases.Select(db => new TestFixtureData(db));
            }

            return new[]
            {
                new TestFixtureData("sqlitemem"),
                new TestFixtureData("memory"),
                new TestFixtureData("sqlite"),
                new TestFixtureData("sqlserver"),
                new TestFixtureData("mysql"),
                new TestFixtureData("postgres"),

            };
        }
    }


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
        public IDataSet<RecordWithSingleKey> RecordsWithSingleKey;
        public IDataSet<RecordWithAutonumKey> RecordsWithAutonumKey;
        public IDataSet<RecordWithIgnoredFields> RecordsWithIgnoredFields;
        public IDataSet<RecordWithInterface> RecordsWithInterface;
        public IDataSet<RecordWithRelationToSelf> RecordsWithRelationToSelf;
        public IDataSet<RecordWithParent> RecordsWithParent;
        public IDataSet<RecordWithChildren> RecordsWithChildren;

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
            RecordsWithSingleKey.Purge();
            RecordsWithAutonumKey.Purge();
            RecordsWithIgnoredFields.Purge();
            RecordsWithRelationToSelf.Purge();
            RecordsWithParent.Purge();
            RecordsWithChildren.Purge();
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
            CreateTable<RecordWithSingleKey>(recreateTable: true);
            CreateTable<RecordWithAutonumKey>(recreateTable: true);
            CreateTable<OneToOneRec1>(recreateTable:true);
            CreateTable<OneToOneRec2>(recreateTable:true);
            CreateTable<RecordWithIgnoredFields>(recreateTable: true);
            CreateTable<RecordWithInterface>(recreateTable: true);
            CreateTable<RecordWithRelationToSelf>(recreateTable: true);
            CreateTable<RecordWithParent>(recreateTable: true);
            CreateTable<RecordWithChildren>(recreateTable: true);
        }

        private static readonly Dictionary<string, Func<DBContext>> _contextFactories;

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

        public static Func<DBContext> GetContextFactory(string driver)
        {
            return _contextFactories[driver];
        }
    }

    public class MySqlStorage : DBContext
    {
        public MySqlStorage() : base(new MySqlDataProvider($"Server={SERVERS.MYSQL};Database=velox;UID=velox;PWD=velox")) { }

        public override string ToString() => "mysql";
    }

    public class PostgresStorage : DBContext
    {
        public PostgresStorage() : base(new PostgresDataProvider($"Host={SERVERS.POSTGRES};Database=velox;Username=velox;Password=velox")) { }

        public override string ToString() => "postgres";
    }

    public class SqlServerStorage : DBContext
    {
        public SqlServerStorage() : base(new SqlServerDataProvider($"Server={SERVERS.SQLSERVER};Database=velox;UID=velox;PWD=velox")) { }

        public override string ToString() => "sqlserver";
    }

    public class SqliteStorage : DBContext
    {
        public SqliteStorage() : base(new SqliteDataProvider("velox.sqlite")) { }
        public override string ToString() => "sqlite";
    }

    public class SqliteMemStorage : DBContext
    {
        public SqliteMemStorage() : base(new SqliteDataProvider(":memory:")) { }
        public override string ToString() => "sqlite-memory";

    }



    public class MemoryStorage : DBContext
    {
        public MemoryStorage() : base(new MemoryDataProvider()) { }
        public override string ToString() => "memory";

    }
}
