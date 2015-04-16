using Windows.Storage;
using Velox.DB.Sql.Sqlite.Native;

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

        public MyContext(IDataProvider dataProvider) : base(dataProvider)
        {
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
        }

        public void CreateAllTables()
        {
            CreateTable<Product>();
            CreateTable<Order>();
            CreateTable<Customer>();
            CreateTable<OrderItem>();
            CreateTable<SalesPerson>();
            CreateTable<PaymentMethod>();
            CreateTable<CustomerPaymentMethodLink>();
        }

        private static MyContext _instance;

        public static MyContext Instance
        {
//            get { return _instance ?? (_instance = new MyContext(new MemoryDataProvider())); }
            get { return _instance ?? (_instance = new SqliteStorage()); }
        }
    }

    public class SqliteStorage : MyContext
    {
        public SqliteStorage() : base(new SqliteDataProvider(ApplicationData.Current.LocalFolder.Path + "\\velox.sqlite")) { }
    }
}
