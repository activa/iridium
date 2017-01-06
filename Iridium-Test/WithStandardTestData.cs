using FluentAssertions;
using System;
using System.Linq;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("sqlite")]
    [TestFixture("sqlserver")]
    [TestFixture("memory")]
    //[TestFixture("mysql")]
    public class WithStandardTestData : TestFixture
    {
        private const int NUM_CUSTOMERS = 20;
        private const int NUM_PRODUCTS = 5;
        private int FIRST_CUSTOMERID = 0;
        
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
                    Price = (decimal) (rnd.Next(100, 20000) / 100.0)
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

            FIRST_CUSTOMERID = customers[0].CustomerID;


        }

        [Test]
        public void StringLenthExpression()
        {
            var selectedCustomers = DB.Customers.Where(c => c.Name.Length == 10);

            selectedCustomers.Should().HaveCount(9);
            selectedCustomers.FirstOrDefault().Name.Length.Should().Be(10);
        }

        [Test]
        public void Range_TakeAndSkip()
        {
            var selectedCustomers = DB.Customers.OrderBy(c => c.CustomerID).Skip(5).Take(10).ToArray();

            selectedCustomers.Length.Should().Be(10);
            selectedCustomers[0].CustomerID.Should().Be(6);
        }

        [Test]
        public void Range_TakeOnly()
        {
            var selectedCustomers = DB.Customers.OrderBy(c => c.CustomerID).Take(10).ToArray();

            selectedCustomers.Length.Should().Be(10);
            selectedCustomers[0].CustomerID.Should().Be(1);
            selectedCustomers[9].CustomerID.Should().Be(10);
        }

        [Test]
        public void Range_SkipOnly()
        {
            var selectedCustomers = DB.Customers.OrderBy(c => c.CustomerID).Skip(5).ToArray();

            selectedCustomers.Length.Should().Be(15);
            selectedCustomers[0].CustomerID.Should().Be(6);
        }

        [Test]
        public void Sorting_SortAlpha_ManyToOne()
        {
            var sortedItems = (from item in DB.OrderItems orderby item.Order.Remark select item).WithRelations(item => item.Order);

            sortedItems.Should().BeInAscendingOrder(item => item.Order.Remark);

            sortedItems = (from item in DB.OrderItems.WithRelations(item => item.Order) orderby item.Order.Remark select item);

            sortedItems.Should().BeInAscendingOrder(item => item.Order.Remark);
        }

        [Test]
        public void Sorting_SortAlpha_ManyToOne_Multiple()
        {
            var sortedItems = (from item in DB.OrderItems orderby item.Order.Remark, item.Price descending select item).WithRelations(o => o.Order);

            AssertHelper.AssertSorting(sortedItems, (prev, current) => (prev.Order.Remark.CompareTo(current.Order.Remark) < 0) || (current.Order.Remark == prev.Order.Remark && (current.Price <= prev.Price)));
        }

        [Test]
        public void Sorting_SortAggregate()
        {
            var sortedOrders = (from order in DB.Orders orderby order.OrderItems.Sum(item => item.Price) select order).WithRelations(o => o.OrderItems);


            AssertHelper.AssertSorting(sortedOrders, (prev, now) => prev.OrderItems.Sum(item => item.Price) <= now.OrderItems.Sum(item => item.Price));
            
        }

        [Test]
        public void PrefetchRelations_OneToMany()
        {
            var customers = DB.Customers.Where(c => c.Name == "Customer 10").WithRelations(c => c.Orders).ToArray();

            customers.Length.Should().Be(1);
            customers[0].Orders.Should().NotBeNull();
            customers[0].Orders.Count().Should().Be(9);
        }

        [Test]
        public void AutoloadRelations_OneToMany()
        {
            var customer = DB.Customers.Read(2);

            customer.Should().NotBeNull();
            customer.Orders.Should().NotBeNull();
            customer.Orders.Count().Should().Be(1);
        }


        [Test]
        public void AutoloadRelations_OneToMany_Deep()
        {
            var customer = DB.Customers.Read(2);//TODO

            customer.Should().NotBeNull();
            customer.Orders.Should().NotBeNull();
            customer.Orders.Count().Should().Be(1);

            Order order = DB.Orders.Read(customer.Orders.First().OrderID);

            order.Should().NotBeNull();

            DB.LoadRelations(order, _ => _.Customer);

            order.Customer.Should().NotBeNull();
            order.Customer.CustomerID.Should().Be(customer.CustomerID);
            order.Customer.Orders.Should().NotBeNull();
            order.Customer.Orders.Count().Should().Be(1);





        }


        [Test]
        public void PrefetchRelations_ManyToOne()
        {
            var orders = DB.Orders.WithRelations(o => o.Customer).ToArray();

            orders.Length.Should().Be(90);
            orders[0].Customer.CustomerID.Should().Be(orders[0].CustomerID);

            //TODO: check query stats
        }


        [Test]
        public void PrefetchRelations_ManyToOne_Deep()
        {
            var orderItems = DB.OrderItems.WithRelations(item => item.Order, item => item.Product, item => item.Order.Customer).ToArray();

            orderItems.Length.Should().Be(220);
            orderItems[0].Order.OrderID.Should().Be(orderItems[0].OrderID);
            orderItems[0].Product.Should().BeNull();
            orderItems[2].Product.ProductID.Should().Be(orderItems[2].ProductID);
            orderItems[0].Order.Customer.CustomerID.Should().Be(orderItems[0].Order.CustomerID);

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

            items.Length.Should().Be(80);
            items[0].ProductID.Should().BeNull();

            string nullProductId = null;

            items = DB.OrderItems.Where(item => item.ProductID == nullProductId).ToArray();

            items.Length.Should().Be(80);
            items[0].ProductID.Should().BeNull();

            items = DB.OrderItems.Where(item => item.ProductID != null).ToArray();

            items.Length.Should().Be(140);
            items[0].ProductID.Should().NotBeNull();
        }

        [Test]
        public void ScalarAll()
        {
            DB.Customers.All(c => c.CustomerID > 0).Should().BeTrue();
            DB.Customers.All(c => c.CustomerID > 1).Should().BeFalse();
        }

        [Test]
        public void ScalarAny()
        {
            DB.Customers.Any(c => c.CustomerID == 0).Should().BeFalse();
            DB.Customers.Any(c => c.CustomerID > 0).Should().BeTrue();
        }

        [Test]
        public void ScalarAny_Chained()
        {
            DB.Customers.Where(c => c.CustomerID == 0).Any().Should().BeFalse();
            DB.Customers.Where(c => c.CustomerID > 0).Any().Should().BeTrue();
            DB.Customers.Where(c => c.CustomerID == 0).Where(c => c.CustomerID > 0).Any().Should().BeFalse();
            DB.Customers.Where(c => c.CustomerID > 0).Where(c => c.CustomerID > 1).Any().Should().BeTrue();

            DB.Customers.Where(c => c.CustomerID == 0).Any(c => c.CustomerID > 0).Should().BeFalse();
            DB.Customers.Where(c => c.CustomerID > 0).Any(c => c.CustomerID > 1).Should().BeTrue();
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

            orders.Count().Should().Be(2);
            orders.ToArray().Length.Should().Be(2); // force enumeration
            orders.First().CustomerID.Should().Be(3);

            orders = DB.Orders.Where(o => o.Customer.CustomerID == 3 && fn(o.Customer,9));

            Assert.AreEqual(0, orders.Count());
            orders.ToArray().Length.Should().Be(0); // force enumeration
        }


        [Test]
        public void ManyToOneFiltering_Lambda()
        {
            var orders = DB.Orders.Where(o => o.Customer.CustomerID == 3);

            Assert.AreEqual(2, orders.Count());

            orders = DB.Orders.Where(o => o.Customer.CustomerID == 3 && o.Customer.Name == "Customer 3");

            Assert.AreEqual(2, orders.Count());

            orders.First().CustomerID.Should().Be(3);
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

            var adhocProducts = DB.Query<Adhoc_Product>("select ProductID,Description,Price,Price as Price2 from Product order by ProductID", null).ToArray();

            adhocProducts.Length.Should().Be(NUM_PRODUCTS);
            adhocProducts[0].ProductID.Should().Be("A");
            ((decimal)adhocProducts[0].Price2).Should().Be(adhocProducts[0].Price);

            DB.Products.Select(p => p.Price).SequenceEqual(adhocProducts.Select(p => p.Price)).Should().BeTrue();
        }

        [Test]
        public void Adhoc_Scalar()
        {
            if (!DB.DataProvider.SupportsSql)
                return;

            var maxPrice1 = DB.QueryScalar<decimal>("select max(Price) from Product where Price < @price", new { price = 100.0m });
            var maxPrice2 = DB.Products.Max(p => p.Price, p => p.Price < 100.0m);

            maxPrice1.Should().BeGreaterThan(0.0m);
            maxPrice1.Should().BeLessThan(100.0m);
            maxPrice1.Should().Be(maxPrice2);
        }

    }
}