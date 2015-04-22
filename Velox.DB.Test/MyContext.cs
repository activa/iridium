using System.ComponentModel.Design;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using Velox.DB.MySql;
using Velox.DB.Sqlite;
using Velox.DB.SqlServer;

namespace Velox.DB.Test
{
    public class MyContext : Vx.Context
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

        public MyContext(IDataProvider dataProvider) : base(dataProvider)
        {
            /*
            Vx.Config.NamingConvention = new NamingConvention()
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
        }

        private static MyContext _instance;

        public static MyContext Instance
        {
//            get { return _instance ?? (_instance = new MemoryStorage()); }
//            get { return _instance ?? (_instance = new SqlServerStorage()); }
//            get { return _instance ?? (_instance = new MySqlStorage()); }
            get { return _instance ?? (_instance = new SqliteStorage()); }
        }
    }

    public class MySqlStorage : MyContext
    {
        public MySqlStorage() : base(new MySqlDataProvider("Server=192.168.1.2;Database=velox;UID=velox;PWD=velox")) { }
    }

    public class SqlServerStorage : MyContext
    {
        public SqlServerStorage() : base(new SqlServerDataProvider("Server=MINI;Database=velox;UID=velox;PWD=velox")) { }
    }

    public class SqliteStorage : MyContext
    {
        public SqliteStorage() : base(new SqliteDataProvider("velox.sqlite")) { }
    }

    public class MemoryStorage : MyContext
    {
        public MemoryStorage() : base(new MemoryDataProvider()) { }
    }
}
