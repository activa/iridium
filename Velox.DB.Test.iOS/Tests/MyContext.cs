using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using Velox.DB.Sql.Sqlite;

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
            get { return _instance ?? (_instance = new SqliteStorage(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "velox.sqlite"))); }
            //get { return _instance ?? (_instance = new MemoryStorage()); }
        }
    }

    public class SqliteStorage : MyContext
    {
        public SqliteStorage(string fn) : base(new SqliteDataProvider(fn, true))
        {
            
        }
    }

    public class MemoryStorage : MyContext
    {
        public MemoryStorage() : base(new MemoryDataProvider()) { }
    }
}
