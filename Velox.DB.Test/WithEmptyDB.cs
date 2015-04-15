using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Velox.DB.TextExpressions;

namespace Velox.DB.Test
{
    [TestFixture]
    public class WithEmptyDB
    {
        private MyContext DB = MyContext.Instance;

        [SetUp]
        public void SetupTest()
        {
            DB.PurgeAll();
        }

        [TestFixtureSetUp]
        public void CreateTables()
        {
            DB.CreateAllTables();
        }

        [Test]
        public void ManyToOne()
        {
            Customer customer = new Customer { Name = "x" };

            customer.Save();

            SalesPerson salesPerson = new SalesPerson();
            salesPerson.Name = "Test";
            salesPerson.Save();

            var order = new Order
            {
                SalesPersonID = null,
                CustomerID = customer.CustomerID
            };

            DB.Orders.Save(order);

            int id = order.OrderID;

            order = DB.Orders.Read(id, o => o.Customer);

            Assert.AreEqual(order.Customer.CustomerID, customer.CustomerID);

            order.SalesPersonID = salesPerson.ID;
            order.Save();

            order = DB.Orders.Read(id, (o) => o.SalesPerson);

            Assert.AreEqual(salesPerson.ID, order.SalesPerson.ID);

            order.SalesPersonID = null;
            order.SalesPerson = null;
            order.Save();

            order = DB.Orders.Read(id, o => o.SalesPerson);

            Assert.IsNull(order.SalesPerson);
            Assert.IsNull(order.SalesPersonID);


        }

        [Test]
        public void AsyncInsert()
        {
            const int numThreads = 20;

            List<string> failedList = new List<string>();
            Task<bool>[] saveTasks = new Task<bool>[numThreads];
            Customer[] customers = new Customer[numThreads];
            List<Customer> createdCustomers = new List<Customer>();

            for (int i = 0; i < numThreads; i++)
            {
                string name = "" + (char)(i + 'A');

                Customer customer = new Customer { Name = name };

                customers[i] = customer;
                saveTasks[i] = DB.Customers.Async().Save(customer);

                saveTasks[i].ContinueWith(t =>
                {
                    if (customer.CustomerID == 0)
                        lock (failedList)
                            failedList.Add("CustomerID == 0");

                    createdCustomers.Add(customer);

                    DB.Customers.Async().Read(customer.CustomerID).ContinueWith(tRead =>
                    {
                        if (customer.Name != tRead.Result.Name)
                            lock (failedList)
                                failedList.Add("Name == " + customer.Name);
                    });
                });
            }


            Task.WaitAll(saveTasks);

            Assert.That(createdCustomers.Count, Is.EqualTo(numThreads));

            foreach (var fail in failedList)
            {
                Assert.Fail(fail);
            }
        }


        [Test]
        public void ParallelTest1()
        {
            const int numThreads = 20;

            Task[] tasks = new Task[numThreads];

            List<string> failedList = new List<string>();
            List<Customer> createdCustomers = new List<Customer>();

            for (int i = 0; i < numThreads; i++)
            {
                var data = i;

                tasks[i] = Task.Factory.StartNew(() =>
                {
                    string name = "" + (char) (((int) data) + 'A');

                    Customer customer = new Customer { Name = name };

                    customer.Save();

                    if (customer.CustomerID == 0)
                        lock (failedList)
                            failedList.Add("CustomerID == 0");

                    Vx.DataSet<Customer>().Read(customer.CustomerID);

                    if (customer.Name != name)
                        lock (failedList)
                            failedList.Add("Name == " + customer.Name);

                    lock (createdCustomers)
                        createdCustomers.Add(customer);
                });
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }

            foreach (var fail in failedList)
            {
                Assert.Fail(fail);
            }

            Assert.That(createdCustomers.Count, Is.EqualTo(numThreads));
        }

        private void CreateRandomPricedProducts()
        {
            Random rnd = new Random();

            var products = Enumerable.Range(1, 20).Select(i => new Product() { ProductID = "P" + i, Description = "Product " + i, Price = (decimal)(rnd.NextDouble() * 100), MinQty = 1 });

            foreach (var product in products)
                DB.Products.Create(product);


        }

        [Test]
        public void StartsWith()
        {
            var products = Enumerable.Range(1, 20).Select(i => new Product()
            {
                ProductID = "P" + i, Description = (char)('A'+(i%10)) + "-Product", Price = 0.0m, MinQty = 1
            });

            foreach (var product in products)
                DB.Products.Create(product);

            var pr = (from p in DB.Products where p.Description.StartsWith("B") select p).ToArray();

            Assert.That(pr.Count(), Is.EqualTo(2));
            Assert.That(pr.All(p => p.Description.StartsWith("B")));

        }

        [Test]
        public void EndsWith()
        {
            var products = Enumerable.Range(1, 20).Select(i => new Product()
            {
                ProductID = "P" + i,
                Description = "Product-"+(char)('A' + (i % 10)),
                Price = 0.0m,
                MinQty = 1
            });

            foreach (var product in products)
                DB.Products.Create(product);

            var pr = (from p in DB.Products where p.Description.EndsWith("B") select p).ToArray();

            Assert.That(pr.Count(), Is.EqualTo(2));
            Assert.That(pr.All(p => p.Description.EndsWith("B")));

        }


        [Test]
        public void SortNumeric_Linq()
        {
            CreateRandomPricedProducts();

            var sortedProducts = from product in DB.Products orderby product.Price select product;

            AssertHelper.AssertSorting(sortedProducts, p => p.Price, Is.GreaterThanOrEqualTo);

            sortedProducts = from product in DB.Products orderby product.Price descending select product;

            AssertHelper.AssertSorting(sortedProducts, p => p.Price, Is.LessThanOrEqualTo);
        }


        [Test]
        public void SortDouble_Expression()
        {
            CreateRandomPricedProducts();

            AssertHelper.AssertSorting(DB.Products.OrderBy(new TextQueryExpression("Price")), p => p.Price, Is.GreaterThanOrEqualTo);
            AssertHelper.AssertSorting(DB.Products.OrderBy(new TextQueryExpression("Price"), SortOrder.Descending), p => p.Price, Is.LessThanOrEqualTo);
        }

        [Test]
        public void CreateAndReadSingleObject()
        {
            Customer customer = new Customer { Name = "A" };

            customer.Save();

            Assert.IsTrue(customer.CustomerID > 0);

            customer = DB.Customers.Read(customer.CustomerID);

            Assert.AreEqual("A",customer.Name);
        }

        [Test]
        public void CreateAndUpdateSingleObject()
        {
            Customer customer = new Customer { Name = "A" };

            customer.Save();

            customer = DB.Customers.Read(customer.CustomerID);

            customer.Name = "B";
            customer.Save();

            customer = DB.Customers.Read(customer.CustomerID);

            Assert.AreEqual("B",customer.Name);
        }

        [Test]
        public void ReadNonexistantObject()
        {
            Customer customer = DB.Customers.Read(70000);

            Assert.IsNull(customer);
        }



        [Test]
        public void CreateWithRelation_ManyToOne_ByID()
        {
            Customer customer = new Customer { Name = "A" };

            customer.Save();

            var order = new Order
            {
                Remark = "test",
                CustomerID = customer.CustomerID
            };

            Assert.IsTrue(order.Save());

            Order order2 = DB.Orders.Read(order.OrderID, o => o.Customer);

            Vx.LoadRelations(() => order.Customer);

            Assert.That(order2.Customer.Name, Is.EqualTo(order.Customer.Name));
            Assert.That(order2.Customer.CustomerID, Is.EqualTo(order.Customer.CustomerID));
            Assert.That(order2.Customer.CustomerID, Is.EqualTo(order.CustomerID));
        }

        [Test]
        public void CreateWithRelation_ManyToOne_ByRelationObject()
        {
            Customer customer = new Customer() { Name = "me" };

            customer.Save();

            var order = new Order
            {
                Remark = "test",
                Customer = customer
            };

            Assert.IsTrue(order.Save());

            Order order2 = DB.Orders.Read(order.OrderID, o => o.Customer);

            Vx.LoadRelations(() => order.Customer);

            Assert.AreEqual(order2.Customer.Name, order.Customer.Name);
            Assert.AreEqual(order2.Customer.CustomerID, order.Customer.CustomerID);
            Assert.AreEqual(order2.Customer.CustomerID, order.CustomerID);
        }

        [Test]
        public void CreateWithRelation_ManyToOne_ByRelationObject_New()
        {
            Customer customer = new Customer() { Name = "me" };

            var order = new Order
            {
                Remark = "test",
                Customer = customer
            };

            Assert.IsTrue(order.Save(true));

            Order order2 = DB.Orders.Read(order.OrderID, o => o.Customer);

            Vx.LoadRelations(() => order.Customer);

            Assert.AreEqual(order2.Customer.Name, order.Customer.Name);
            Assert.AreEqual(order2.Customer.CustomerID, order.Customer.CustomerID);
            Assert.AreEqual(order2.Customer.CustomerID, order.CustomerID);
        }


        [Test]
        public void CreateOrderWithNewCustomer()
        {
            Customer customer = new Customer() {Name = "me"};

            customer.Save();

            var order = new Order
            {
                Remark = "test", 
                CustomerID = customer.CustomerID
            };

            Assert.IsTrue(order.Save());

            Vx.LoadRelations(() => order.Customer);

            Order order2 = DB.Orders.Read(order.OrderID , o => o.Customer);

            Assert.AreEqual(order2.Customer.Name, order.Customer.Name);
            Assert.AreEqual(order2.Customer.CustomerID, order.Customer.CustomerID);
            Assert.AreEqual(order2.Customer.CustomerID, order.CustomerID);

            Vx.LoadRelations(() => order2.Customer.Orders);

            Assert.AreEqual(order2.Customer.Orders.First().CustomerID, order.CustomerID);
        }

        [Test]
        public void CreateOrderWithExistingCustomer()
        {
            Customer cust = new Customer { Name = "A" };

            cust.Save();

            cust = DB.Customers.Read(cust.CustomerID);

            Order order = new Order();

            order.CustomerID = cust.CustomerID;

            Assert.IsTrue(order.Save());

            order = DB.Orders.Read(order.OrderID);

            Vx.LoadRelations(() => order.Customer);
            Vx.LoadRelations(() => order.Customer.Orders);

            Assert.AreEqual(order.Customer.Name, cust.Name);
            Assert.AreEqual(order.Customer.CustomerID, cust.CustomerID);
            Assert.AreEqual(order.CustomerID, cust.CustomerID);

            Assert.AreEqual((order.Customer.Orders.First()).CustomerID, cust.CustomerID);

            order.Customer.Name = "B";
            order.Customer.Save();


            order = DB.Orders.Read(order.OrderID);

            Vx.LoadRelations(() => order.Customer);

            Assert.AreEqual(order.CustomerID, cust.CustomerID);

            Assert.AreEqual("B", order.Customer.Name);
        }

        [Test]
        public void DeleteSingleObject()
        {
            List<Customer> customers = new List<Customer>();

            for (int i = 0; i < 10; i++)
            {
                Customer customer = new Customer() {Name = "Customer " + (i + 1)};

                customer.Save();

                customers.Add(customer);
            }

            DB.Customers.Delete(customers[5]);

            Assert.IsNull(DB.Customers.Read(customers[5].CustomerID));

            Assert.AreEqual(9,DB.Customers.Count());
        }

        [Test]
        public void DeleteMultipleObjects()
        {
            List<Customer> customers = new List<Customer>();

            for (int i = 0; i < 10; i++)
            {
                Customer customer = new Customer() { Name = "Customer " + (i + 1) };

                customer.Save();

                customers.Add(customer);
            }

            DB.Customers.Delete(c => c.Name == "Customer 2" || c.Name == "Customer 4");

            Assert.IsNotNull(DB.Customers.Read(customers[0].CustomerID));
            Assert.IsNull(DB.Customers.Read(customers[1].CustomerID));
            Assert.IsNotNull(DB.Customers.Read(customers[2].CustomerID));
            Assert.IsNull(DB.Customers.Read(customers[3].CustomerID));

            Assert.AreEqual(8, DB.Customers.Count());
        }

        [Test]
        public void CreateOrderWithNewItems()
        {
            Order order = new Order
            {
                Customer = new Customer
                {
                    Name = "test"
                },
                OrderItems = new List<OrderItem>
                {
                    new OrderItem {Description = "test", Qty = 5, Price = 200.0},
                    new OrderItem {Description = "test", Qty = 3, Price = 45.0}
                }
            };

            Assert.IsTrue(order.Save(true));

            order = DB.Orders.Read(order.OrderID, o => o.OrderItems);

            //double totalPrice = Convert.ToDouble(order.OrderItems.GetScalar("Qty * Price", CSAggregate.Sum));

            Assert.AreEqual(2, order.OrderItems.Count, "Order items not added");
            //Assert.AreEqual(1135.0, totalPrice, "Incorrect total amount");

            order.OrderItems.Add(new OrderItem { Description = "test", Qty = 2, Price = 1000.0 });

            Assert.IsTrue(order.Save(true));

            order = DB.Orders.Read(order.OrderID, o => o.OrderItems);

            //totalPrice = Convert.ToDouble(order.OrderItems.GetScalar("Qty * Price", CSAggregate.Sum));

            Assert.AreEqual(3, order.OrderItems.Count, "Order item not added");
            //Assert.AreEqual(3135.0, totalPrice, "Total price incorrect");

            /*
            order.OrderItems.DeleteAll();

            order = Order.Read(order.OrderID);

            Assert.AreEqual(0, order.OrderItems.Count, "Order items not deleted");

            Assert.IsTrue(order.Delete());
                */

        }

        [Test]
        public void RandomCreation()
        {
            Random rnd = new Random();

            Customer cust = new Customer();
            cust.Name = "A";
            cust.Save();

            double total = 0.0;

            for (int i = 0; i < 5; i++)
            {
                Order order = new Order
                {
                    Customer = cust
                };


                order.Save();

                for (int j = 0; j < 20; j++)
                {
                    int qty = rnd.Next(1, 10);
                    double price = rnd.NextDouble() * 500.0;

                    OrderItem item = new OrderItem() { Description = "test", Qty = (short)qty, Price = price, OrderID = order.OrderID };

                    item.Save();

                    total += qty * price;
                }


            }



            var orders = DB.Orders.ToArray();

            Assert.AreEqual(5, orders.Length);

            double total2 = DB.OrderItems.Sum(item => item.Qty*item.Price);

            Assert.AreEqual(total, total2, 0.000001);

            foreach (Order order in orders)
            {
                Vx.LoadRelations(order, o => o.Customer, o => o.OrderItems);

                Assert.AreEqual(cust.CustomerID, order.Customer.CustomerID);
                Assert.AreEqual(20, order.OrderItems.Count);
                Assert.AreEqual(cust.Name, order.Customer.Name);

                DB.OrderItems.Delete(order.OrderItems.First());
            }

            total2 = DB.OrderItems.Sum(item => item.Qty * item.Price);

            Assert.That(total, Is.GreaterThan(total2));

            Assert.AreEqual(95, DB.OrderItems.Count());
        }


        /*


[Test]
public void ManyToMany()
{
    for (int i = 0; i < numIterations; i++)
    {
        DeleteData();

        Customer cust1 = new Customer();
        Customer cust2 = new Customer();
        Customer cust3 = new Customer();

        cust1.Name = "Cust1";
        cust2.Name = "Cust2";
        cust3.Name = "Cust3";

        SalesPerson sp1 = SalesPerson.New();
        SalesPerson sp2 = SalesPerson.New();

        sp1.Name = "SP1";
        sp1.SalesPersonType = SalesPersonType.External;

        sp2.Name = "SP2";
        sp2.SalesPersonType = SalesPersonType.Internal;

        cust1.Save();
        cust2.Save();
        cust3.Save();
        sp1.Save();
        sp2.Save();

        sp1 = SalesPerson.Read(sp1.ID);

        Assert.AreEqual(SalesPersonType.External, sp1.SalesPersonType);

        Order order;

        order = Order.New();
        order.SalesPerson = sp1;
        order.Customer = cust1;
        order.Save();

        order = Order.New();
        order.SalesPerson = sp2;
        order.Customer = cust1;
        order.Save();

        order = Order.New();
        order.SalesPerson = sp2;
        order.Customer = cust2;
        order.Save();

        order = Order.New();
        order.SalesPerson = sp1;
        order.Customer = cust3;
        order.Save();

        cust1 = Customer.Read(cust1.CustomerID);
        cust2 = Customer.Read(cust2.CustomerID);
        cust3 = Customer.Read(cust3.CustomerID);

        Assert.AreEqual(2, cust1.SalesPeople.Count);
        Assert.AreEqual(1, cust2.SalesPeople.Count);
        Assert.AreEqual(1, cust3.SalesPeople.Count);

        Assert.AreEqual(2, Convert.ToInt32(cust1.SalesPeople.GetScalar("*", CSAggregate.Count)));
        Assert.AreEqual(1, Convert.ToInt32(cust2.SalesPeople.GetScalar("*", CSAggregate.Count)));
        Assert.AreEqual(1, Convert.ToInt32(cust3.SalesPeople.GetScalar("*", CSAggregate.Count)));

        cust1 = Customer.Read(cust1.CustomerID);
        cust2 = Customer.Read(cust2.CustomerID);
        cust3 = Customer.Read(cust3.CustomerID);

        Assert.AreEqual(2, cust1.SalesPeople.Count);
        Assert.AreEqual(1, cust2.SalesPeople.Count);
        Assert.AreEqual(1, cust3.SalesPeople.Count);

        Assert.AreEqual(2, Convert.ToInt32(cust1.SalesPeople.GetScalar("*", CSAggregate.Count)));
        Assert.AreEqual(1, Convert.ToInt32(cust2.SalesPeople.GetScalar("*", CSAggregate.Count)));
        Assert.AreEqual(1, Convert.ToInt32(cust3.SalesPeople.GetScalar("*", CSAggregate.Count)));

    }
}

[Test]
public void PureManyToMany()
{
    DeleteData();

    PaymentMethod[] methods = new PaymentMethod[] { PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New() };

    methods[0].Name = "Bank"; methods[0].MonthlyCost = 5;
    methods[1].Name = "Credit Card"; methods[1].MonthlyCost = 50;
    methods[2].Name = "PayPal"; methods[2].MonthlyCost = 10;
    methods[3].Name = "Cash"; methods[3].MonthlyCost = 20;
    methods[4].Name = "Bancontact"; methods[4].MonthlyCost = 100;

    foreach (PaymentMethod method in methods)
        method.Save();

    for (int i = 0; i < 10; i++)
    {
        Customer customer = new Customer();

        customer.Name = "customer" + (i + 1);

        Random rnd = new Random();
        int nMethods = 0;

        for (int j = 0; j < 5 || nMethods < 1; j++)
        {
            if ((rnd.Next() % 2) == 0)
            {
                Assert.IsNotNull(PaymentMethod.Read(methods[j % 5].PaymentMethodID));

                customer.PaymentMethods.Add(methods[j % 5]);
                nMethods++;
            }
        }

        customer.Save();

        int customerID = customer.CustomerID;

        customer = Customer.Read(customerID);

        Assert.AreEqual(nMethods, customer.PaymentMethods.Count);

        customer.PaymentMethods.Remove(customer.PaymentMethods[0]);

        customer.Save();

        Assert.AreEqual(nMethods - 1, customer.PaymentMethods.Count);

        customer = Customer.Read(customerID);

        Assert.AreEqual(nMethods - 1, customer.PaymentMethods.Count);
    }
}


[Test]
public void ReadUniqueKey()
{
    DeleteData();

    for (int i = 0; i < numIterations * 10; i++)
    {
        Order order = Order.New();

        order.Customer = new Customer();
        order.Customer.Name = "cust" + (i + 1);

        order.Save();
    }

    Random rnd = new Random();

    for (int i = 0; i < numIterations * 20; i++)
    {
        int n = rnd.Next(1, numIterations * 10);

        Customer cust = Customer.ReadUsingUniqueField("Name", "cust" + n);

        Assert.IsNotNull(cust);
        Assert.AreEqual("cust" + n, cust.Name);
    }
}

[Test]
public void ObjectNotFound()
{
    DeleteData();

    Order order = Order.New();

    order.Customer = new Customer();
    order.Customer.Name = "me";

    order.Save();


    int orderID = order.OrderID;

    Order order2 = Order.Read(orderID);

    Assert.IsNotNull(order2);

    try
    {
        Order.Read(orderID + 4234);

        Assert.Fail("No exception thrown");
    }
    catch (CSObjectNotFoundException)
    {
    }

}

[Test]
public void ObjectNotFoundSafe()
{
    DeleteData();

    Order order = Order.New();

    order.Customer = new Customer();
    order.Customer.Name = "me";

    order.Save();

    int orderID = order.OrderID;

    Order order2 = Order.ReadSafe(orderID);

    Assert.IsNotNull(order2);

    order2 = Order.ReadSafe(orderID + 4234);

    Assert.IsNull(order2);
}

[Test]
public void DeleteFromCollections()
{
    DeleteData();

    Order order = Order.New();

    order.Customer = new Customer();
    order.Customer.Name = "me";


    for (int i = 0; i < 100; i++)
    {
        OrderItem item = order.OrderItems.AddNew();

        item.Description = "test" + (i + 1);
        item.Price = i;
        item.Qty = (short)i;
    }

    order.Save();

    order = Order.Read(order.OrderID);

    Assert.AreEqual(order.OrderItems.Count, 100);

    foreach (OrderItem item in order.OrderItems)
    {
        if ((item.Qty % 5) == 0)
            item.MarkForDelete();
    }

    order.Save();

    Assert.AreEqual(order.OrderItems.Count, 80);

    order = Order.Read(order.OrderID);

    Assert.AreEqual(order.OrderItems.Count, 80);

    CSList<OrderItem> items = new CSList<OrderItem>(order.OrderItems);

    items.AddFilter("Qty < 50");

    Assert.AreEqual(40, items.Count);

    items.DeleteAll();

    order = Order.Read(order.OrderID);

    Assert.AreEqual(40, order.OrderItems.Count);

    order.OrderItems.AddFilter("Qty >= 80");

    Assert.AreEqual(16, order.OrderItems.Count);

    order.OrderItems.DeleteAll();

    Assert.AreEqual(0, order.OrderItems.Count);

    order = Order.Read(order.OrderID);

    Assert.AreEqual(24, order.OrderItems.Count);


}

[Test]
public void NullableFields()
{
    DeleteData();

    Order order = Order.New();

    order.Customer = new Customer();
    order.Customer.Name = "blabla";

    order.Save();

    int orderId = order.OrderID;

    order = Order.Read(orderId);

    Assert.IsNull(order.SalesPersonID);

    order.SalesPerson = SalesPerson.New();
    order.SalesPerson.Name = "Salesperson";

    order.Save();

    order = Order.Read(orderId);

    Assert.IsNotNull(order.SalesPersonID);

    order.SalesPersonID = null;

    order.Save();

    order = Order.Read(orderId);

    Assert.IsNull(order.SalesPersonID);
}

[Test]
public void OrderBy()
{
    DeleteData();

    Order order;

    order = Order.New();
    order.Customer = new Customer();
    order.Customer.Name = "Alain";
    order.OrderDate = new DateTime(2005, 1, 10);
    order.Save();

    order = Order.New();
    order.Customer = new Customer();
    order.Customer.Name = "Luc";
    order.OrderDate = new DateTime(2005, 1, 6);
    order.Save();

    order = Order.New();
    order.Customer = new Customer();
    order.Customer.Name = "Gerard";
    order.OrderDate = new DateTime(2005, 1, 8);
    order.Save();

    CSList<Order> orders = Order.List();

    orders.OrderBy = "OrderDate";

    Assert.AreEqual(new DateTime(2005, 1, 6), orders[0].OrderDate);
    Assert.AreEqual(new DateTime(2005, 1, 10), orders[2].OrderDate);

    orders = new OrderCollection();

    orders.OrderBy = "Customer.Name";

    Assert.AreEqual(orders[0].Customer.Name, "Alain");
    Assert.AreEqual(orders[2].Customer.Name, "Luc");

    CSList<OrderItem> orderItems = new CSList<OrderItem>("Order.Customer.Name=''").OrderedBy("Order.Customer.Name");

    Assert.AreEqual(0, orderItems.Count);

    Assert.AreEqual("Luc", Order.GetScalar("Customer.Name", CSAggregate.Max, "Customer.Name<>'Alain'"));
    Assert.AreEqual("Alain", Order.GetScalar("Customer.Name", CSAggregate.Min));

}

[Test]
public void ComplexFilters()
{
    DeleteData();

    PaymentMethod[] methods = { PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New() };

    methods[0].Name = "Bank"; methods[0].MonthlyCost = 5;
    methods[1].Name = "Credit Card"; methods[1].MonthlyCost = 50;
    methods[2].Name = "PayPal"; methods[2].MonthlyCost = 10;
    methods[3].Name = "Cash"; methods[3].MonthlyCost = 20;
    methods[4].Name = "Bancontact"; methods[4].MonthlyCost = 100;


    foreach (PaymentMethod method in methods)
        method.Save();

    Order order = Order.New();

    order.Customer = new Customer();
    order.Customer.Name = "test";
    order.Customer.PaymentMethods.Add(methods[2]);
    order.Customer.PaymentMethods.Add(methods[4]);

    order.OrderItems.Add(OrderItem.New("test", 5, 200.0));
    order.OrderItems.Add(OrderItem.New("test", 3, 45.0));

    order.Save();

    order = Order.New();

    order.Customer = new Customer();
    order.Customer.Name = "blabla";

    order.OrderItems.Add(OrderItem.New("test", 15, 100.0));
    order.OrderItems.Add(OrderItem.New("test2", 6, 35.0));

    order.SalesPerson = SalesPerson.New();
    order.SalesPerson.Name = "SalesPerson1";

    order.Save();



    Assert.AreEqual(2, new CSList<OrderItem>("Order.Customer.Name = 'blabla'").Count);
    Assert.AreEqual(2, new CSList<OrderItem>("len(Order.Customer.Name) = 6").Count);
    Assert.AreEqual(2, new CSList<OrderItem>("len(Order.Customer.Name) = @len", new { len = 6 }).Count);
    //Assert.AreEqual(2, new CSList<OrderItem>("left(Order.Customer.Name,3) = 'bla'").Count);
    Assert.AreEqual(1, new CSList<Order>("countdistinct(OrderItems.Description) = 1").Count);
    Assert.AreEqual(1, new CSList<Order>("countdistinct(OrderItems.Description) = 2").Count);
    Assert.AreEqual(1, new OrderCollection("max(OrderItems.Price) = 200").Count);
    Assert.AreEqual(1, new OrderCollection("sum(OrderItems.Price) = 245").Count);
    Assert.AreEqual(2, new CSList<Order>("count(OrderItems) = 2").Count);
    Assert.AreEqual(2, new CSList<Order>("has(OrderItems)").Count);
    Assert.AreEqual(1, Customer.List("len(Name)=4").Count);
    Assert.AreEqual(1, new CSList<Customer>("count(PaymentMethods)>0").Count);
    Assert.AreEqual(1, new CSList<Customer>("sum(PaymentMethods.MonthlyCost)=110").Count);
    Assert.AreEqual(1, new CSList<Customer>("sum(PaymentMethods.MonthlyCost where Name='PayPal')=10").Count);
    Assert.AreEqual(1, new CSList<Customer>("count(PaymentMethods where PaymentMethodID = @MethodID) > 0", "@MethodID", methods[2].PaymentMethodID).Count);
    Assert.AreEqual(1, new CSList<Customer>("count(PaymentMethods where Name = @MethodName) > 0", "@MethodName", methods[2].Name).Count);
    Assert.AreEqual(1, new CSList<Customer>("has(PaymentMethods)").Count);

    Assert.AreEqual(1, new OrderCollection("count(OrderItems where Price > 100.0) = 1").Count);
}

[Test]
public void Transactions()
{
    DeleteData();

    Customer customer = new Customer();

    customer.Name = "Test1";
    customer.Save();

    Assert.AreEqual(1, new CSList<Customer>().Count);

    using (CSTransaction transaction = new CSTransaction(CSIsolationLevel.ReadCommitted))
    {
        Customer customer2 = new Customer();
        customer2.Name = "Test2";
        customer2.Save();

        transaction.Commit();
    }

    Assert.AreEqual(2, new CSList<Customer>().Count);

    using (new CSTransaction(CSIsolationLevel.ReadUncommitted))
    {
        Customer customer3 = new Customer();
        customer3.Name = "Test3";
        customer3.Save();

        Assert.AreEqual(3, new CSList<Customer>().Count);

        Customer customer4 = new Customer();
        customer4.Name = "Test4";
        customer4.Save();

        Assert.AreEqual(4, new CSList<Customer>().Count);

        // We don't do a commit, so a rollback will be performed
    }

    Assert.AreEqual(2, new CSList<Customer>().Count);

}

[Test]
public void ReadFirst()
{
    DeleteData();

    Customer customer = new Customer();

    customer.Name = "Bob";
    customer.Save();

    customer = new Customer();
    customer.Name = "Mike";
    customer.Save();

    customer = Customer.ReadFirst("Name=@Name", "@Name", "Bob");

    Assert.AreEqual(customer.Name, "Bob");
}

[Test]
public void CompositeKey()
{
    DeleteData();

    Customer customer = new Customer();
    customer.Name = "Blabla";
    customer.Save();

    int customerID = customer.CustomerID;

    PaymentMethod[] methods = new PaymentMethod[] { PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New() };

    methods[0].Name = "Bank"; methods[0].MonthlyCost = 5;
    methods[1].Name = "Credit Card"; methods[1].MonthlyCost = 50;
    methods[2].Name = "PayPal"; methods[2].MonthlyCost = 10;
    methods[3].Name = "Cash"; methods[3].MonthlyCost = 20;
    methods[4].Name = "Bancontact"; methods[4].MonthlyCost = 100;

    foreach (PaymentMethod method in methods)
        method.Save();

    CustomerPaymentMethodLink link = CustomerPaymentMethodLink.New();
    link.CustomerID = customerID;
    link.PaymentMethodID = methods[0].PaymentMethodID;
    link.Save();

    link = CustomerPaymentMethodLink.Read(customerID, methods[0].PaymentMethodID);

    Assert.IsNotNull(link);
    Assert.AreEqual(customerID, link.CustomerID);
    Assert.AreEqual(methods[0].PaymentMethodID, link.PaymentMethodID);

}

[Test]
public void ObjectEvents()
{
    DeleteData();

    int customerCreated = 0;

    ObjectEventHandler<Customer> eventDelegate = delegate(Customer obj, EventArgs e) { customerCreated = obj.CustomerID; };

    Customer.AnyObjectCreated += eventDelegate;

    Customer customer = new Customer();
    customer.Name = "Blabla";
    customer.Save();

    Assert.AreEqual(customer.CustomerID, customerCreated);

    Customer.AnyObjectCreated -= eventDelegate;
}

[Test]
public void ObjectParamaters()
{
    SetupTestData();

    foreach (Customer customer in new CSList<Customer>())
    {
        Assert.AreEqual(4, Order.List("Customer=@Customer", "@Customer", customer).Count);
    }
}

[Test]
public void ListPredicates()
{
    DeleteData();

    Customer customer = new Customer();

    customer.Name = "Philippe";
    customer.Save();

    customer = new Customer();

    customer.Name = "Dirk";
    customer.Save();

    customer = new Customer();

    customer.Name = "Paul";
    customer.Save();

    CSList<Customer> customers = Customer.List().FilteredBy(delegate(Customer c) { return c.Name.StartsWith("P"); });

    Assert.AreEqual(2, customers.Count);

    customers = customers.FilteredBy(delegate(Customer c) { return c.Name.EndsWith("e"); });

    Assert.AreEqual(1, customers.Count);
}

[Test]
public void DefaultSort()
{
    DeleteData();

    Random rnd = new Random();

    Customer cust = new Customer();
    cust.Name = "Blabla";

    double total = 0.0;

    for (int i = 0; i < 5; i++)
    {
        Order order = Order.New();

        order.Customer = cust;

        for (int j = 0; j < 50; j++)
        {
            int qty = rnd.Next(1, 10);
            double price = rnd.NextDouble() * 500.0;

            order.OrderItems.Add(OrderItem.New("test", (short)qty, price));

            total += qty * price;
        }

        order.Save();

        order = Order.Read(order.OrderID);

        double lastPrice = 0.0;

        foreach (OrderItem orderItem in order.OrderItems)
        {
            Assert.IsTrue(orderItem.Price >= lastPrice);

            lastPrice = orderItem.Price;
        }

        int lastQty = 0;

        foreach (OrderItem orderItem in order.OrderItems.OrderedBy("Qty"))
        {
            Assert.IsTrue(orderItem.Qty >= lastQty);

            lastQty = orderItem.Qty;
        }

    }


}


[QueryExpression("select Name,count(*) as NumOrders from tblCustomers inner join tblOrders on tblOrders.CustomerID=tblCustomers.CustomerID group by Name order by Name")]
internal class TestQueryClass
{
    public string Name;
    public int NumOrders;
}


[QueryExpression("select Name,count(*) as NumOrders from tblCustomers inner join tblOrders on tblOrders.CustomerID=tblCustomers.CustomerID group by Name order by Name")]
internal class TestQuery : CSTypedQuery<TestQuery>
{
    public string Name;
    public int NumOrders;
}

[Test]
public void TypedQuery()
{
    SetupTestData();

    TestQueryClass[] items = CSDatabase.RunQuery<TestQueryClass>();

    Assert.AreEqual(5, items.Length);
    Assert.AreEqual(4, items[3].NumOrders);
    Assert.AreEqual("Customer 1", items[0].Name);
    Assert.AreEqual("Customer 2", items[1].Name);
    Assert.AreEqual("Customer 3", items[2].Name);
    Assert.AreEqual("Customer 4", items[3].Name);

    TestQueryClass item = CSDatabase.RunSingleQuery<TestQueryClass>();

    Assert.IsNotNull(item);
    Assert.AreEqual("Customer 1", item.Name);

    Assert.AreEqual(5, TestQuery.Run().Length);
}


[Test]
public void Paging()
{
    DeleteData();

    for (int i = 1; i <= 70; i++)
    {
        Customer customer = new Customer();
        Order order = Order.New();

        customer.Name = "Customer" + i.ToString("0000");
        customer.Save();

        order.Customer = customer;
        order.OrderItems.Add(OrderItem.New("test", 4, 10));
        order.Save();
    }

    CSList<Order> orders;

    CSList<Customer> customers = Customer.OrderedList("Name").Range(11, 10);

    Assert.AreEqual(10, customers.Count);
    Assert.AreEqual("Customer0011", customers[0].Name);
    Assert.AreEqual("Customer0020", customers[9].Name);

    orders = Order.OrderedList("Customer.Name , OrderID").Range(51, 10);

    Assert.AreEqual(10, orders.Count);
    Assert.AreEqual("Customer0051", orders[0].Customer.Name);
    Assert.AreEqual("Customer0060", orders[9].Customer.Name);




}

[Test]
public void Paging2()
{
    DeleteData();

    Customer customer = new Customer();
    customer.Name = "Customer";
    customer.Save();

    for (int i = 1; i <= 5; i++)
    {
        Order order = Order.New();


        order.Customer = customer;
        order.OrderItems.Add(OrderItem.New("test", 4, 10));
        order.Save();
    }

    CSList<Order> orders = Order.List("has(OrderItems where Qty > 1)").OrderedBy("OrderID").Range(2, 2);

    Assert.AreEqual(2, orders.Count);

}
 * */
    }
}