using System;
using System.Diagnostics;
using System.Linq;
//using Iridium.DB.Postgres;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "memory")]
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class WithStandardTestData : TestFixture
    {
        private const int NUM_CUSTOMERS = 20;
        private const int NUM_PRODUCTS = 5;
        private int FIRST_CUSTOMERID = 0;
        
        /*
        Products: (5)
           {
              ProductID: "A" ... "E"
              Description: "Product 1" ... "Product 5"
              MinQty: 1 ... 5
              Price: random
           }
             
         */
        public WithStandardTestData(string driver) : base(driver)
        {
            DB.CreateAllTables();
            DB.PurgeAll();

            Random rnd = new Random();

            Customer[] customers = new Customer[NUM_CUSTOMERS];
            Product[] products = new Product[NUM_PRODUCTS];

            for (int productIndex = 0; productIndex < NUM_PRODUCTS; productIndex++)
            {
                Product product = new Product()
                {
                    ProductID = "" + (char)('A' + productIndex), 
                    Description = "Product " + (productIndex + 1), 
                    MinQty = 1,
                    Price = 1m + (decimal) (rnd.Next(100, 20000) / 100.0)
                };

                DB.Products.Insert(product);

                products[productIndex] = product;
            }

            for (int customerIndex = 0; customerIndex < NUM_CUSTOMERS; customerIndex++)
            {
                Customer customer = new Customer
                {
                    Name = "Customer " + (customerIndex + 1)
                };

                customer.Save();

                customers[customerIndex] = customer;

                for (int orderIndex = 0; orderIndex < customerIndex%10; orderIndex++)
                {
                    var order = new Order
                    {
                        CustomerID = customers[customerIndex].CustomerID,
                        Remark = new string((char) ('A' + rnd.Next(0, 26)), 1),
                        OrderDate = DateTime.Now
                    };

                    DB.Orders.Save(order);

                    for (int itemIndex = 0; itemIndex < customerIndex%5; itemIndex++)
                    {
                        var item = new OrderItem
                        {
                            OrderID = order.OrderID,
                            
                            Description = "Item " + (orderIndex + 1) + "/" + (itemIndex + 1),
                            Qty = (short) rnd.Next(1, 10),
                            Price = (double) products[itemIndex].Price,
                            ProductID = itemIndex == 0 ? null : products[itemIndex].ProductID
                        };

                        DB.OrderItems.Save(item);
                    }
                }
            }

            DB.PaymentMethods.Insert(new PaymentMethod() {Name = "Cash"});
            DB.PaymentMethods.Insert(new PaymentMethod() { Name = "Visa" });
            DB.PaymentMethods.Insert(new PaymentMethod() { Name = "Mastercard" });
            DB.PaymentMethods.Insert(new PaymentMethod() { Name = "Amex" });

            FIRST_CUSTOMERID = customers[0].CustomerID;
        }

        [Test]
        public void StringLenthExpression()
        {
            var selectedCustomers = DB.Customers.Where(c => c.Name.Length == 10);

            Assert.That(selectedCustomers.Count(), Is.EqualTo(9));
            Assert.That(selectedCustomers.First().Name, Has.Length.EqualTo(10));
        }

        [Test]
        public void Range_TakeAndSkip()
        {
            var selectedCustomers = DB.Customers.OrderBy(c => c.CustomerID).Skip(5).Take(10).ToArray();

            Assert.That(selectedCustomers, Has.Length.EqualTo(10));
            Assert.That(selectedCustomers[0].CustomerID, Is.EqualTo(6));
        }

        [Test]
        public void Range_TakeOnly()
        {
            var selectedCustomers = DB.Customers.OrderBy(c => c.CustomerID).Take(10).ToArray();

            Assert.That(selectedCustomers, Has.Length.EqualTo(10));
            Assert.That(selectedCustomers[0].CustomerID, Is.EqualTo(1));
            Assert.That(selectedCustomers[9].CustomerID, Is.EqualTo(10));
        }

        [Test]
        public void Range_TakeOnly_MixedFilter()
        {
            Func<Customer,bool> customFilter = c => c.CustomerID > 1;

            var selectedCustomers = DB.Customers.OrderBy(c => c.CustomerID).Where(c => c.CustomerID<=10 && customFilter(c)).Take(5).ToArray();

            Assert.That(selectedCustomers, Has.Length.EqualTo(5));
            Assert.That(selectedCustomers[0].CustomerID, Is.EqualTo(2));
            Assert.That(selectedCustomers[4].CustomerID, Is.EqualTo(6));
        }

        [Test]
        public void Range_SkipOnly()
        {
            var selectedCustomers = DB.Customers.OrderBy(c => c.CustomerID).Skip(5).ToArray();

            Assert.That(selectedCustomers, Has.Length.EqualTo(15));
            Assert.That(selectedCustomers[0].CustomerID, Is.EqualTo(6));
        }

        [Test]
        public void Sorting_SortAlpha_ManyToOne()
        {
            var sortedItems = (from item in DB.OrderItems orderby item.Order.Remark select item).WithRelations(item => item.Order);

            Assert.That(sortedItems.Select(item => item.Order.Remark), Is.Ordered);

            sortedItems = (from item in DB.OrderItems.WithRelations(item => item.Order) orderby item.Order.Remark select item);

            Assert.That(sortedItems.Select(item => item.Order.Remark), Is.Ordered);
        }

        [Test]
        public void Sorting_SortAlpha_ManyToOne_Multiple()
        {
            var sortedItems = (from item in DB.OrderItems orderby item.Order.Remark, item.Price descending select item).WithRelations(o => o.Order);

            Assert.That(sortedItems.Select(item => item.Order.Remark), Is.Ordered);
        }

        [Test]
        public void Sorting_SortAggregate()
        {
            var sortedOrders = (from order in DB.Orders where order.OrderItems.Any() orderby order.OrderItems.Sum(item => item.Price) select order).WithRelations(o => o.OrderItems);

            Assert.That(sortedOrders.Select(order => order.OrderItems.Sum(item => item.Price)), Is.Ordered);
        }

        [Test]
        public void PrefetchRelations_OneToMany()
        {
            var customers = DB.Customers.Where(c => c.Name == "Customer 10").WithRelations(c => c.Orders).ToArray();

            Assert.That(customers, Has.Length.EqualTo(1));
            Assert.That(customers[0].Orders, Is.Not.Null);
            Assert.That(customers[0].Orders.Count(), Is.EqualTo(9));
        }

        [Test]
        public void AutoloadRelations_OneToMany()
        {
            var customer = DB.Customers.Read(2);

            Assert.That(customer, Is.Not.Null);
            Assert.That(customer.Orders, Is.Not.Null);
            Assert.That(customer.Orders.Count(), Is.EqualTo(1));
        }


        [Test]
        public void AutoloadRelations_OneToMany_Deep()
        {
            var customer = DB.Customers.Read(2);//TODO

            Assert.That(customer, Is.Not.Null);
            Assert.That(customer.Orders, Is.Not.Null);
            Assert.That(customer.Orders.Count(), Is.EqualTo(1));

            Order order = DB.Orders.Read(customer.Orders.First().OrderID);

            Assert.That(order, Is.Not.Null);

            DB.LoadRelations(order, o => o.Customer);

            Assert.That(order.Customer, Is.Not.Null);
            Assert.That(order.Customer.CustomerID, Is.EqualTo(customer.CustomerID));
            Assert.That(order.Customer.Orders, Is.Not.Null);
            Assert.That(order.Customer.Orders.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Relations_ManyToOne_Explicit()
        {
            var orders = DB.Orders.ToArray();

            Assert.That(orders, Has.Length.EqualTo(90));

            DB.LoadRelation(() => orders[0].Customer);

            Assert.That(orders[0].Customer, Is.Not.Null);
            Assert.That(orders[0].Customer.CustomerID, Is.EqualTo(orders[0].CustomerID));
        }

        [Test]
        public void Relations_ManyToOne_Explicit_ByInterface()
        {
            var orders = DB.Orders.ToArray();

            Assert.That(orders.Length, Is.EqualTo(90));

            IOrder order = orders[0];

            DB.LoadRelation(() => order.Customer);

            Assert.That(order.Customer, Is.Not.Null);

            Assert.That(order.Customer.CustomerID, Is.EqualTo(orders[0].CustomerID));
        }


        [Test]
        public void PrefetchRelations_ManyToOne()
        {
            var orders = DB.Orders.WithRelations(o => o.Customer).ToArray();

            Assert.That(orders, Has.Length.EqualTo(90));
            Assert.That(orders[0].Customer.CustomerID, Is.EqualTo(orders[0].CustomerID));

            //TODO: check query stats
        }

        [Test]
        public void PrefetchRelations_ManyToOne_Deep()
        {
            var orderItems = DB.OrderItems.WithRelations(item => item.Order, item => item.Product, item => item.Order.Customer).OrderBy(item => item.OrderItemID).ToArray();

            Assert.That(orderItems, Has.Length.EqualTo(220));
            Assert.That(orderItems[0].Order.OrderID, Is.EqualTo(orderItems[0].OrderID));
            Assert.That(orderItems[0].Product, Is.Null);
            Assert.That(orderItems[2].Product.ProductID, Is.EqualTo(orderItems[2].ProductID));
            Assert.That(orderItems[0].Order.Customer.CustomerID, Is.EqualTo(orderItems[0].Order.CustomerID));

            //TODO: check query stats
        }


        [Test]
        public void SimpleFiltering_Lambda()
        {
            var customers = DB.Customers.Where(customer => customer.Name == "Customer 10").ToArray();

            Assert.AreEqual(1, customers.Length);

            Assert.AreEqual(10, customers[0].CustomerID);
        }

        [Test]
        public void SimpleFiltering_Lambda_NullCheck()
        {
            var items = DB.OrderItems.Where(item => item.ProductID == null).ToArray();

            Assert.That(items, Has.Length.EqualTo(80));
            Assert.That(items, Has.All.Property(nameof(OrderItem.ProductID)).Null);

            string nullProductId = null;

            items = DB.OrderItems.Where(item => item.ProductID == nullProductId).ToArray();

            Assert.That(items, Has.Length.EqualTo(80));
            Assert.That(items, Has.All.Property(nameof(OrderItem.ProductID)).Null);

            items = DB.OrderItems.Where(item => item.ProductID != null).ToArray();

            Assert.That(items, Has.Length.EqualTo(140));
            Assert.That(items, Has.All.Property(nameof(OrderItem.ProductID)).Not.Null);
        }

        [Test]
        public void ScalarAll()
        {
            Assert.That(DB.Customers.All(c => c.CustomerID > 0), Is.True);
            Assert.That(DB.Customers.All(c => c.CustomerID > 1), Is.False);
        }

        [Test]
        public void ScalarAny()
        {
            Assert.That(DB.Customers.Any(c => c.CustomerID == 0), Is.False);
            Assert.That(DB.Customers.Any(c => c.CustomerID > 0), Is.True);
        }

        [Test]
        public void ScalarAny_Chained()
        {
            Assert.That(DB.Customers.Where(c => c.CustomerID == 0).Any(), Is.False);
            Assert.That(DB.Customers.Where(c => c.CustomerID > 0).Any(), Is.True);
            Assert.That(DB.Customers.Where(c => c.CustomerID == 0).Where(c => c.CustomerID > 0).Any(), Is.False);
            Assert.That(DB.Customers.Where(c => c.CustomerID > 0).Where(c => c.CustomerID > 1).Any(), Is.True);

            Assert.That(DB.Customers.Where(c => c.CustomerID == 0).Any(c => c.CustomerID > 0), Is.False);
            Assert.That(DB.Customers.Where(c => c.CustomerID > 0).Any(c => c.CustomerID > 1), Is.True);
        }

        [Test]
        public void SimpleFiltering_Linq()
        {
            var customers = (from c in DB.Customers where c.Name == "Customer 10" select c).ToArray();

            Assert.AreEqual(1, customers.Length);

            Assert.AreEqual(10, customers[0].CustomerID);
        }

        [Test]
        public void FilteringWithNonSupportedLambda()
        {
            Func<Customer, int, bool> fn = (customer, len) => customer.Name.Length == len;

            var orders = DB.Orders.Where(o => o.Customer.CustomerID == 3 && fn(o.Customer, 10));

            Assert.That(orders.Count(), Is.EqualTo(2));
            Assert.That(orders.ToArray(), Has.Length.EqualTo(2)); // force enumeration
            Assert.That(orders.First().CustomerID, Is.EqualTo(3));

            orders = DB.Orders.Where(o => o.Customer.CustomerID == 3 && fn(o.Customer,9));

            Assert.AreEqual(0, orders.Count());
            Assert.That(orders.ToArray(), Has.Length.EqualTo(0));
        }


        [Test]
        public void ManyToOneFiltering_Lambda()
        {
            var orders = DB.Orders.Where(o => o.Customer.CustomerID == 3);

            Assert.AreEqual(2, orders.Count());

            orders = DB.Orders.Where(o => o.Customer.CustomerID == 3 && o.Customer.Name == "Customer 3");

            Assert.AreEqual(2, orders.Count());

            Assert.That(orders.First().CustomerID, Is.EqualTo(3));
        }

        [Test]
        public void ManyToOneFiltering_Contains_Lambda()
        {
            var orders = DB.Orders.Where(o => o.Customer.Name.Contains("Customer 3") || o.Remark == "blabla");

            Assert.That(orders.ToArray().Length, Is.EqualTo(2));
            Assert.That(orders.Count(), Is.EqualTo(2));

            Assert.That(orders.First().CustomerID, Is.EqualTo(3));
        }

        [Test]
        public void ManyToOneFiltering_Lambda_Chained()
        {
            var orders = DB.Orders.Where(o => o.Customer.CustomerID == 3).Where(o => o.Customer.Name == "Customer 3");

            Assert.AreEqual(2, orders.Count());
        }

        [Test]
        public void ManyToOneFiltering_Linq()
        {
            var orders = from o in DB.Orders where o.Customer.CustomerID == 3 select o;

            Assert.AreEqual(2, orders.Count());

            orders = from o in DB.Orders where o.Customer.CustomerID == 3 && o.Customer.Name == "Customer 3" select o;

            Assert.AreEqual(2, orders.Count());
        }

        [Test]
        public void ManyToOneFiltering_Deep_Lambda()
        {
            var orderItems = DB.OrderItems.Where(o => o.Order.Customer.CustomerID == 3 && o.Order.Customer.Name == "Customer 3");

            Assert.AreEqual(2 * 2, orderItems.Count());

            orderItems = DB.OrderItems.Where(o => o.Order.Customer.CustomerID == 3);

            Assert.AreEqual(2 * 2, orderItems.Count());
        }

        [Test]
        public void OneToManyFiltering_Lambda()
        {
            var customers = DB.Customers.Where(c => c.Orders.Count() == 4);

            Assert.AreEqual(2, customers.Count());
        }

        [Test]
        public void OneToManyFiltering_Deep_Lambda()
        {
            var customers = DB.Customers.Where(c => c.Orders.Count(o => o.OrderItems.Count() == 4) == 4);

            Assert.AreEqual(2, customers.Count());
        }

        [Test]
        public void MixedRelationFiltering1_Lambda()
        {
            var customers = DB.Customers.Where(c => c.Orders.Count(o => o.Customer.Name == "Customer 5") == 4);

            Assert.AreEqual(1, customers.Count());

            customers = DB.Customers.Where(c => c.Orders.Count(o => o.Customer.Name == c.Name) == 4);

            Assert.AreEqual(2, customers.Count());

        }

        [Test]
        public void MixedRelationFiltering1_Linq()
        {
            var customers = from c in DB.Customers where c.Orders.Count(o => o.Customer.Name == "Customer 5") == 4 select c;

            Assert.AreEqual(1, customers.Count());

            customers = from c in DB.Customers where c.Orders.Count(o => o.Customer.Name == c.Name) == 4 select c;

            Assert.AreEqual(2, customers.Count());

        }

        [Test]
        public void MixedRelationFiltering2_Lambda()
        {
            var orders = DB.Orders.Where(o => o.Customer.Orders.Count() == 1);

            Assert.AreEqual(2, orders.Count());

            var customers = DB.Customers.Where(c => c.CustomerID == 3 && c.Orders.Any(order => order.OrderItems.Count(item => item.OrderID == order.OrderID) == order.OrderItems.Count()));

            Assert.AreEqual(1, customers.Count());
        }

        [Test]
        public void MixedRelationFiltering2_Lambda_Chained()
        {
            var customers = DB.Customers.Where(c => c.CustomerID == 3).Where(c => c.Orders.Any(order => order.OrderItems.Count(item => item.OrderID == order.OrderID) == order.OrderItems.Count()));

            Assert.AreEqual(1, customers.Count());
        }

        private class Adhoc_Product
        {
            public string ProductID;
            public string Description { get; set; }
            public decimal Price;
            public double Price2;
        }

        [Test]
        public void Adhoc_Mapped()
        {
            if (!DB.DataProvider.SupportsSql)
                return;

            Func<string, string> quoteField = s => s;
            Func<string, string> quoteTable = s => s;

            if (DB.DataProvider is SqlDataProvider)
            {
                quoteField = s => ((SqlDataProvider) DB.DataProvider).SqlDialect.QuoteField(s);
                quoteTable = s => ((SqlDataProvider) DB.DataProvider).SqlDialect.QuoteTable(s);
            }

            var adhocProducts = DB.SqlQuery<Adhoc_Product>($"select {quoteField("ProductID")},{quoteField("Description")},{quoteField("Price")},{quoteField("Price")} as {quoteField("Price2")} from {quoteTable("Product")} order by {quoteField("ProductID")}").ToArray();

            Assert.That(adhocProducts, Has.Length.EqualTo(NUM_PRODUCTS));
            Assert.That(adhocProducts[0].ProductID, Is.EqualTo("A"));
            Assert.That((decimal)adhocProducts[0].Price2, Is.EqualTo(adhocProducts[0].Price));
            Assert.That(DB.Products.OrderBy(p => p.ProductID).Select(p => p.ProductID), Is.EquivalentTo(adhocProducts.Select(p => p.ProductID)));
        }

        [Test]
        public void Adhoc_Scalar()
        {
            if (!DB.DataProvider.SupportsSql)
                return;

            Func<string, string> quoteField = s => s;
            Func<string, string> quoteTable = s => s;

            if (DB.DataProvider is SqlDataProvider sqlDataProvider)
            {
                quoteField = s => sqlDataProvider.SqlDialect.QuoteField(s);
                quoteTable = s => sqlDataProvider.SqlDialect.QuoteTable(s);
            }

            var maxPrice1 = DB.SqlQueryScalar<decimal>($"select max({quoteField("Price")}) from {quoteTable("Product")} where {quoteField("Price")} < @price", new { price = 100.0m });
            var maxPrice2 = DB.Products.Max(p => p.Price, p => p.Price < 100.0m);

            Assert.That(maxPrice1, Is.GreaterThan(0m));
            Assert.That(maxPrice1, Is.LessThan(100m));
            Assert.That(maxPrice2, Is.EqualTo(maxPrice2));
        }
    }
}