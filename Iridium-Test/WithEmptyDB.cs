using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using Iridium.DB;
using Iridium.DB.Test;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("sqlite")]
    [TestFixture("sqlitemem")]
    [TestFixture("sqlserver")]
    [TestFixture("memory")]
    [TestFixture("mysql")]
    [TestFixture("postgres")]
    public class WithEmptyDB : TestFixture
    {
        public WithEmptyDB(string driver) : base(driver)
        {
            DB.CreateAllTables();
        }

        [SetUp]
        public void SetupTest()
        {
            DB.PurgeAll();
        }

        [Test]
        public void Events_ObjectCreated()
        {
            int counter1 = 0;
            int counter2 = 0;

            DB.RecordsWithAutonumKey.Events.ObjectCreated += (sender, args) => { counter1++; };
            DB.RecordsWithAutonumKey.Events.ObjectCreated += (sender, args) => { counter2++; };

            InsertRecord(new RecordWithAutonumKey() {Name = "A"});
            InsertRecord(new RecordWithAutonumKey() {Name = "A"});

            Assert.That(counter1, Is.EqualTo(2));
            Assert.That(counter2, Is.EqualTo(2));
        }

        [Test]
        public void Events_ObjectCreating()
        {
            int counter = 0;

            EventHandler<ObjectWithCancelEventArgs<Customer>> ev1 = (sender, args) => { counter++; };
            EventHandler<ObjectWithCancelEventArgs<Customer>> ev2 = (sender, args) => { counter++; };

            DB.Customers.Events.ObjectCreating += ev1;
            DB.Customers.Events.ObjectCreating += ev2;

            try
            {
                bool saveResult = DB.Insert(new Customer {Name = "A"});

                Assert.That(saveResult, Is.True);
                Assert.That(DB.Customers.FirstOrDefault(c => c.Name == "A"), Is.Not.Null);
                Assert.That(counter, Is.EqualTo(2));
            }
            finally
            {
                DB.Customers.Events.ObjectCreating -= ev1;
                DB.Customers.Events.ObjectCreating -= ev2;
            }
        }

        [Test]
        public void Events_ObjectCreatingWithCancel1()
        {
            int counter = 0;

            EventHandler<ObjectWithCancelEventArgs<Customer>> ev = (sender, args) => { counter++; };
            EventHandler<ObjectWithCancelEventArgs<Customer>> evWithCancel = (sender, args) => { counter++; args.Cancel = true; };

            DB.Customers.Events.ObjectCreating += ev;
            DB.Customers.Events.ObjectCreating += evWithCancel;

            try
            {
                bool saveResult = DB.Insert(new Customer() { Name = "A" });

                Assert.That(DB.Customers.FirstOrDefault(c => c.Name == "A"), Is.Null);
                Assert.That(saveResult, Is.False);
                Assert.That(counter, Is.EqualTo(2));
            }
            finally
            {
                DB.Customers.Events.ObjectCreating -= ev;
                DB.Customers.Events.ObjectCreating -= evWithCancel;
            }
        }

        [Test]
        public void Events_ObjectCreatingWithCancel2()
        {
            int counter = 0;

            EventHandler<ObjectWithCancelEventArgs<Customer>> ev = (sender, args) => { counter++; };
            EventHandler<ObjectWithCancelEventArgs<Customer>> evWithCancel = (sender, args) => { counter++; args.Cancel = true; };

            DB.Customers.Events.ObjectCreating += evWithCancel;
            DB.Customers.Events.ObjectCreating += ev;

            try
            {
                bool saveResult = DB.Insert(new Customer() { Name = "A" });

                Assert.That(saveResult, Is.False);
                Assert.That(DB.Customers.FirstOrDefault(c => c.Name == "A"), Is.Null);
                Assert.That(counter, Is.EqualTo(1));
            }
            finally
            {
                DB.Customers.Events.ObjectCreating -= ev;
                DB.Customers.Events.ObjectCreating -= evWithCancel;
            }
        }


        [Test]
        public void ManyToOne()
        {
            var customer = InsertRecord(new Customer { Name = "x" });
            var salesPerson = InsertRecord(new SalesPerson {Name = "Test"});

            var order = InsertRecord(new Order
            {
                SalesPersonID = null,
                CustomerID = customer.CustomerID
            });

            int id = order.OrderID;

            order = DB.Orders.Read(id, o => o.Customer);

            Assert.AreEqual(order.Customer.CustomerID, customer.CustomerID);

            order.SalesPersonID = salesPerson.ID;
            DB.Orders.Update(order);

            order = DB.Orders.Read(id, (o) => o.SalesPerson);

            Assert.AreEqual(salesPerson.ID, order.SalesPerson.ID);

            order.SalesPersonID = null;
            order.SalesPerson = null;
            DB.Orders.Update(order);

            order = DB.Orders.Read(id, o => o.SalesPerson);

            Assert.IsNull(order.SalesPerson);
            Assert.IsNull(order.SalesPersonID);
        }

        [Test]
        public void ReverseRelation_Generic()
        {
            Order order = new Order
            {
                Customer = new Customer {Name = "A"},
                OrderItems = new UnboundDataSet<OrderItem>
                {
                    new OrderItem {Description = "X"},
                    new OrderItem {Description = "X"},
                    new OrderItem {Description = "X"},
                    new OrderItem {Description = "X"},
                    new OrderItem {Description = "X"},
                }
            };

            var originalOrder = order;

            DB.Orders.Insert(order, o => o.Customer, o => o.OrderItems);

            order = DB.Orders.Read(originalOrder.OrderID, o => o.OrderItems);

            Assert.That(order.OrderItems, Has.Exactly(5).Items.And.All.Property(nameof(OrderItem.Order)).SameAs(order));
        }

        [Test]
        public void ReverseRelation_DataSet()
        {
            Customer customer = new Customer() {Name = "A"};

            DB.Customers.Insert(customer);

            for (int i = 0; i < 5; i++)
                DB.Orders.Insert(new Order()
                {
                    CustomerID = customer.CustomerID
                });

            customer = DB.Customers.Read(customer.CustomerID);

            Assert.That(customer.Orders, Has.Exactly(5).Items.And.All.Property("Customer").SameAs(customer));
        }

        [Test]
        public void ReverseRelation_OneToOne()
        {
            OneToOneRec1 rec1 = new OneToOneRec1();
            OneToOneRec2 rec2 = new OneToOneRec2();

            DB.Insert(rec1);
            DB.Insert(rec2);

            rec1.OneToOneRec2ID = rec2.OneToOneRec2ID;
            rec2.OneToOneRec1ID = rec1.OneToOneRec1ID;

            DB.Update(rec1);
            DB.Update(rec2);

            rec1 = DB.Read<OneToOneRec1>(rec1.OneToOneRec1ID, r=> r.Rec2 );

            Assert.That(rec1.Rec2.Rec1, Is.SameAs(rec1));
        }

        [Test]
        public void OneToManyWithOptionalRelation()
        {
            var customer = InsertRecord(new Customer { Name = "x" });
            var salesPerson = InsertRecord(new SalesPerson { Name = "Test" });

            Order[] orders =
            {
                new Order { CustomerID = customer.CustomerID, OrderDate = DateTime.Today, SalesPersonID = null},
                new Order { CustomerID = customer.CustomerID, OrderDate = DateTime.Today, SalesPersonID = salesPerson.ID}
            };

            foreach (var order in orders)
            {
                DB.Insert(order);
            }

            salesPerson = DB.SalesPeople.First();

            Assert.That(salesPerson.Orders.Count(), Is.EqualTo(1));
            Assert.That(salesPerson.Orders.First().OrderID, Is.EqualTo(orders[1].OrderID));
        }

        [Test]
        public void AsyncInsert()
        {
            const int numThreads = 100;

            var failedList = new List<string>();
            var saveTasks = new Task[numThreads];
            var customers = new Customer[numThreads];
            var createdCustomers = new List<Customer>();

            var ids = new HashSet<int>();

            for (int i = 0; i < numThreads; i++)
            {
                string name = "C" + (i + 1);

                Customer customer = new Customer { Name = name };

                customers[i] = customer;
                Task<bool> task = DB.Customers.Async().Insert(customer);

                saveTasks[i] = task.ContinueWith(t =>
                {
                    if (customer.CustomerID == 0)
                        lock (failedList)
                            failedList.Add("CustomerID == 0");

                    lock (ids)
                    {
                        if (ids.Contains(customer.CustomerID))
                            failedList.Add($"Dupicate CustomerID {customer.CustomerID} for {customer.Name}");

                        ids.Add(customer.CustomerID);
                    }

                    lock (createdCustomers)
                        createdCustomers.Add(customer);

                    DB.Customers.Async().Read(customer.CustomerID).ContinueWith(tRead =>
                    {
                        if (customer.Name != tRead.Result.Name)
                            lock (failedList)
                                failedList.Add($"Customer == ({tRead.Result.CustomerID},{tRead.Result.Name}), but should be ({customer.CustomerID},{customer.Name})");
                    });
                });
            }

            Task.WaitAll(saveTasks);
            
            Assert.That(failedList, Is.Empty);
            Assert.False(saveTasks.Any(t => t.IsFaulted));

            Assert.That(createdCustomers.Count, Is.EqualTo(numThreads));

            foreach (var fail in failedList)
            {
                Assert.Fail(fail);
            }
        }


        [Test]
        public void ParallelTest1()
        {
            const int numThreads = 100;

            Task[] tasks = new Task[numThreads];

            List<string> failedList = new List<string>();
            Customer[] customers = new Customer[numThreads];
            List<Customer> createdCustomers = new List<Customer>();

            HashSet<int> ids = new HashSet<int>();

            for (int i = 0; i < numThreads; i++)
            {
                string name = "C" + (i + 1);

                tasks[i] = Task.Run(() =>
                {
                    Customer customer = InsertRecord(new Customer {Name = name});

                    if (customer.CustomerID == 0)
                        lock (failedList)
                            failedList.Add("CustomerID == 0");

                    lock (ids)
                    {
                        if (ids.Contains(customer.CustomerID))
                            failedList.Add($"Duplicate CustomerID {customer.CustomerID} for {customer.Name}");

                        ids.Add(customer.CustomerID);
                    }

                    lock (createdCustomers)
                        createdCustomers.Add(customer);

                    var newCustomer = Ir.DataSet<Customer>().Read(customer.CustomerID);

                    if (customer.Name != newCustomer.Name)
                        lock (failedList)
                            failedList.Add($"Customer == ({newCustomer.CustomerID},{newCustomer.Name}), but should be ({customer.CustomerID},{customer.Name})");

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

            Assert.That(createdCustomers, Has.Count.EqualTo(numThreads));
        }

        private void CreateRandomPricedProducts()
        {
            Random rnd = new Random();

            InsertRecords<Product>(20, (product, i) =>
            {
                product.ProductID = "P" + i;
                product.Description = "Product " + i;
                product.Price = (decimal) (rnd.NextDouble() * 100);
                product.MinQty = 1;
            });
        }

        [Test]
        public void StartsWith()
        {
            var products = Enumerable.Range(1, 20).Select(i => new Product()
            {
                ProductID = "P" + i, Description = (char)('A'+(i%10)) + "-Product", Price = 0.0m, MinQty = 1
            });

            foreach (var product in products)
                DB.Products.Insert(product);

            var pr = (from p in DB.Products where p.Description.StartsWith("B") select p).ToArray();

            Assert.That(pr, Has.Length.EqualTo(2));
            Assert.True(pr.All(p => p.Description.StartsWith("B")));
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
                DB.Products.Insert(product);

            var pr = (from p in DB.Products where p.Description.EndsWith("B") select p).ToArray();

            Assert.That(pr, Has.Length.EqualTo(2));
            Assert.True(pr.All(p => p.Description.EndsWith("B")));
        }


        [Test]
        public void SortNumeric_Linq()
        {
            CreateRandomPricedProducts();

            var sortedProducts = from product in DB.Products orderby product.Price select product;

            Assert.That(sortedProducts.Select(product => product.Price), Is.Ordered.Ascending);

            sortedProducts = from product in DB.Products orderby product.Price descending select product;

            Assert.That(sortedProducts.Select(product => product.Price), Is.Ordered.Descending);
       }

        [Test]
        public void ManyTransactions()
        {
            for (int i = 0; i < 100; i++)
            {
                Customer customer = InsertRecord(new Customer {Name = "A"});

                Assert.IsTrue(customer.CustomerID > 0);

                int customerId = customer.CustomerID;

                customer = DB.Customers.Read(customerId);

                Assert.NotNull(customer, $"Customer ID {customerId}");
                Assert.AreEqual("A", customer.Name);

                customer.Delete();

                Assert.That(DB.Customers.Count(), Is.Zero);
            }
        }


        [Test]
        public void CreateAndReadSingleObject()
        {
            var customer = InsertRecord(new Customer { Name = "A" });

            Assert.IsTrue(customer.CustomerID > 0);

            int customerId = customer.CustomerID;

            customer = DB.Customers.Read(customerId);

            Assert.NotNull(customer,$"Customer ID {customerId}");
            Assert.AreEqual("A",customer.Name);
        }

        [Test]
        public void CreateAndUpdateSingleObject()
        {
            var customer = InsertRecord(new Customer { Name = "A" });

            customer = DB.Customers.Read(customer.CustomerID);

            customer.Name = "B";
            customer.Update();

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
            var customer = InsertRecord(new Customer { Name = "A" });

            var order = new Order
            {
                Remark = "test",
                CustomerID = customer.CustomerID
            };

            Assert.IsTrue(DB.Orders.Insert(order));

            Order order2 = DB.Orders.Read(order.OrderID, o => o.Customer);

            DB.LoadRelations(() => order.Customer);

            Assert.That(order2.Customer.Name,Is.EqualTo(order.Customer.Name));
            Assert.That(order2.Customer.CustomerID,Is.EqualTo(order.Customer.CustomerID));
            Assert.That(order2.Customer.CustomerID,Is.EqualTo(order.CustomerID));
        }

        [Test]
        public void CreateWithRelation_ManyToOne_ByRelationObject()
        {
            var customer = InsertRecord(new Customer { Name = "me" });

            var order = new Order
            {
                Remark = "test",
                Customer = customer
            };

            Assert.IsTrue(DB.Orders.Insert(order));

            Order order2 = DB.Orders.Read(order.OrderID, o => o.Customer);

            DB.LoadRelations(() => order.Customer);

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

            Assert.IsTrue(DB.Orders.Insert(order, relationsToSave: o => o.Customer));

            Order order2 = DB.Orders.Read(order.OrderID, o => o.Customer);

            DB.LoadRelations(() => order.Customer);

            Assert.AreEqual(order2.Customer.Name, order.Customer.Name);
            Assert.AreEqual(order2.Customer.CustomerID, order.Customer.CustomerID);
            Assert.AreEqual(order2.Customer.CustomerID, order.CustomerID);
        }


        [Test]
        public void CreateOrderWithNewCustomer()
        {
            var customer = InsertRecord(new Customer { Name = "me" });

            var order = new Order
            {
                Remark = "test", 
                CustomerID = customer.CustomerID
            };

            Assert.IsTrue(DB.Orders.Insert(order));

            DB.LoadRelations(() => order.Customer);

            Order order2 = DB.Orders.Read(order.OrderID , o => o.Customer);

            Assert.AreEqual(order2.Customer.Name, order.Customer.Name);
            Assert.AreEqual(order2.Customer.CustomerID, order.Customer.CustomerID);
            Assert.AreEqual(order2.Customer.CustomerID, order.CustomerID);

            DB.LoadRelations(() => order2.Customer.Orders);

            Assert.AreEqual(order2.Customer.Orders.First().CustomerID, order.CustomerID);
        }

        [Test]
        public void CreateOrderWithExistingCustomer()
        {
            Customer cust = new Customer { Name = "A" };

            cust.Insert();

            cust = DB.Customers.Read(cust.CustomerID);

            Order order = new Order { CustomerID = cust.CustomerID };


            Assert.IsTrue(DB.Orders.Insert(order));

            order = DB.Orders.Read(order.OrderID);

            Assert.IsNotNull(order);

            DB.LoadRelations(() => order.Customer);
            DB.LoadRelations(() => order.Customer.Orders);

            Assert.AreEqual(order.Customer.Name, cust.Name);
            Assert.AreEqual(order.Customer.CustomerID, cust.CustomerID);
            Assert.AreEqual(order.CustomerID, cust.CustomerID);

            Assert.AreEqual((order.Customer.Orders.First()).CustomerID, cust.CustomerID);

            order.Customer.Name = "B";
            order.Customer.Update();


            order = DB.Orders.Read(order.OrderID);

            DB.LoadRelations(() => order.Customer);

            Assert.AreEqual(order.CustomerID, cust.CustomerID);

            Assert.AreEqual("B", order.Customer.Name);
        }

        [Test]
        public void DeleteSingleObject()
        {
            var records = InsertRecords<RecordWithAutonumKey>(10, (c, i) => c.Name = "Customer " + i);

            DB.RecordsWithAutonumKey.Delete(records[5]);

            Assert.IsNull(DB.Customers.Read(records[5].Key));

            Assert.AreEqual(9,DB.RecordsWithAutonumKey.Count());
        }

        [Test]
        public void DeleteMultipleObjects()
        {
            var customers = InsertRecords<Customer>(10, (customer, i) => { customer.Name = "Customer " + i; });

            DB.Customers.Delete(c => c.Name == "Customer 2" || c.Name == "Customer 4");

            Assert.IsNotNull(DB.Customers.Read(customers[0].CustomerID));
            Assert.IsNull(DB.Customers.Read(customers[1].CustomerID));
            Assert.IsNotNull(DB.Customers.Read(customers[2].CustomerID));
            Assert.IsNull(DB.Customers.Read(customers[3].CustomerID));

            Assert.AreEqual(8, DB.Customers.Count());
        }

        [Test]
        public void DeleteWithRelationFilter()
        {
            if (Driver.StartsWith("sqlite"))
                return;

            List<Order> orders = new List<Order>();

            for (int i = 0; i < 10; i++)
            {
                Order order = new Order
                {
                    Customer = new Customer
                    {
                        Name = "Customer " + (i+1)
                    },
                    Remark = "Remark" + (i+1)
                };

                DB.Orders.Insert(order, deferSave:false, relationsToSave: o => o.Customer);

                orders.Add(order);
            }

            Assert.AreEqual(10, DB.Orders.Count());

            DB.Orders.Delete(o => o.Customer.Name == "Customer 2" || o.Customer.Name == "Customer 4");

            Assert.IsNotNull(DB.Orders.Read(orders[0].OrderID));
            Assert.IsNull(DB.Orders.Read(orders[1].OrderID));
            Assert.IsNotNull(DB.Orders.Read(orders[2].OrderID));
            Assert.IsNull(DB.Orders.Read(orders[3].OrderID));

            Assert.AreEqual(8, DB.Orders.Count());
        }

        [Test]
        public void DeleteAllObjects()
        {
            InsertRecords<Customer>(10, (customer, i) => { customer.Name = "Customer " + i; });

            Assert.That(DB.Customers.Count(), Is.EqualTo(10));

            DB.Customers.DeleteAll();

            Assert.That(DB.Customers.Count(),Is.Zero);
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
                OrderItems = new UnboundDataSet<OrderItem>
                {
                    new OrderItem {Description = "test", Qty = 5, Price = 200.0},
                    new OrderItem {Description = "test", Qty = 3, Price = 45.0}
                }
            };

            Assert.IsTrue(DB.Orders.Insert(order, o => o.Customer, o => o.OrderItems));

            order = DB.Orders.Read(order.OrderID);

            Assert.AreEqual(2, order.OrderItems.Count(), "Order items not added");

            order.OrderItems.Insert(new OrderItem { Description = "test", Qty = 2, Price = 1000.0 });

            Assert.IsTrue(DB.Orders.Update(order, o => o.OrderItems));

            order = DB.Orders.Read(order.OrderID);

            Assert.AreEqual(3, order.OrderItems.Count(), "Order item not added");

            order.OrderItems.Insert(new OrderItem { Description = "test", Qty = 3, Price = 2000.0 }, deferSave:true);

            Assert.IsTrue(DB.Orders.Update(order, o => o.OrderItems));

            order = DB.Orders.Read(order.OrderID);

            Assert.AreEqual(4, order.OrderItems.Count(), "Order item not added");

        }

        [Test]
        public void RandomCreation()
        {
            Random rnd = new Random();

            Customer cust = InsertRecord(new Customer { Name = "A" });

            double total = 0.0;

            for (int i = 0; i < 5; i++)
            {
                Order order = InsertRecord(new Order
                {
                    Customer = cust
                });

                for (int j = 0; j < 20; j++)
                {
                    int qty = rnd.Next(1, 10);
                    double price = rnd.NextDouble() * 500.0;

                    OrderItem item = new OrderItem() { Description = "test", Qty = (short)qty, Price = price, OrderID = order.OrderID };

                    DB.OrderItems.Insert(item);

                    total += qty * price;
                }


            }



            var orders = DB.Orders.ToArray();

            Assert.AreEqual(5, orders.Length);

            double total2 = DB.OrderItems.Sum(item => item.Qty*item.Price);

            Assert.AreEqual(total, total2, 0.000001);

            foreach (Order order in orders)
            {
                DB.LoadRelations(order, o => o.Customer/*, o => o.OrderItems*/);

                Assert.AreEqual(cust.CustomerID, order.Customer.CustomerID);
                Assert.AreEqual(20, order.OrderItems.Count());
                Assert.AreEqual(cust.Name, order.Customer.Name);

                DB.OrderItems.Delete(order.OrderItems.First());
            }

            total2 = DB.OrderItems.Sum(item => item.Qty * item.Price);

            Assert.That(total, Is.GreaterThan(total2));

            Assert.AreEqual(95, DB.OrderItems.Count());
        }


        [Test]
        public void CompositeKeyCreateAndRead()
        {
            DB.Insert(new RecordWithCompositeKey
            {
                Key1 = 1, 
                Key2 = 2, 
                Name = "John"
            });

            var rec = DB.Read<RecordWithCompositeKey>(new {Key1 = 1, Key2 = 2});

            Assert.NotNull(rec);
            Assert.AreEqual(1, rec.Key1);
            Assert.AreEqual(2, rec.Key2);
            Assert.AreEqual("John",rec.Name);
        }

        [Test]
        public void TransactionRollback()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
            //DB.Products.Count().Should().Be(0);

            using (var transaction = new Transaction(DB))
            {
                DB.Products.Insert(new Product() {ProductID = "X", Description = "X"});

                transaction.Rollback();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
            //DB.Products.Count().Should().Be(0);


        }

        [Test]
        public void TransactionImplicitRollback()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TransactionCommit()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
            //DB.Products.Count().Should().Be(0);

            using (var transaction = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                transaction.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(1));
            //DB.Products.Count().Should().Be(1);
        }


        [Test]
        public void NestedTransactions()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
            //DB.Products.Count().Should().Be(0);

            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    transaction2.Rollback();
                }

                transaction1.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(1));
            Assert.That(DB.Products.First().ProductID, Is.EqualTo("X"));
        }

        [Test]
        public void NestedTransactions2()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    using (var transaction3 = new Transaction(DB))
                    {
                        DB.Products.Insert(new Product() { ProductID = "Z", Description = "Z" });

                        transaction3.Commit();
                    }

                    transaction2.Rollback();
                }

                transaction1.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(1));
            Assert.That(DB.Products.First().ProductID, Is.EqualTo("X"));
        }

        [Test]
        public void NestedTransactions3()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
            
            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    using (var transaction3 = new Transaction(DB))
                    {
                        DB.Products.Insert(new Product() { ProductID = "Z", Description = "Z" });

                        transaction3.Rollback();
                    }

                    transaction2.Commit();
                }

                transaction1.Commit();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(2));
            Assert.That(DB.Products.OrderBy(p => p.ProductID).First().ProductID, Is.EqualTo("X"));
            Assert.That(DB.Products.OrderBy(p => p.ProductID).Skip(1).First().ProductID, Is.EqualTo("Y"));
        }

        [Test]
        public void NestedTransactions4()
        {
            if (!DB.DataProvider.SupportsTransactions)
                return;

            Assert.That(DB.Products.Count(), Is.EqualTo(0));

            using (var transaction1 = new Transaction(DB))
            {
                DB.Products.Insert(new Product() { ProductID = "X", Description = "X" });

                using (var transaction2 = new Transaction(DB))
                {
                    DB.Products.Insert(new Product() { ProductID = "Y", Description = "Y" });

                    using (var transaction3 = new Transaction(DB))
                    {
                        DB.Products.Insert(new Product() { ProductID = "Z", Description = "Z" });

                        transaction3.Commit();
                    }

                    transaction2.Commit();
                }

                transaction1.Rollback();
            }

            Assert.That(DB.Products.Count(), Is.EqualTo(0));
        }

        [Test]
        public void IgnoredFields()
        {
            RecordWithIgnoredFields rec = new RecordWithIgnoredFields()
            {
                FirstName = "John",
                LastName = "Doe"
            };

            DB.Insert(rec);

            rec = DB.RecordsWithIgnoredFields.First(r => r.FirstName == "John" && r.LastName == "Doe");

            Assert.That(rec.FirstName, Is.EqualTo("John"));
            Assert.That(rec.LastName, Is.EqualTo("Doe"));
            Assert.That(rec.FullName, Is.EqualTo("John Doe"));
        }

        [Test]
        public void FilterOnInterfaceFields()
        {
            DB.DataSet<RecordWithInterface>().Insert(new RecordWithInterface() {Name = "A"});
            DB.DataSet<RecordWithInterface>().Insert(new RecordWithInterface() {Name = "B"});
            DB.DataSet<RecordWithInterface>().Insert(new RecordWithInterface() {Name = "C"});

            GenericFilterOnInterfaceFields<RecordWithInterface>();
        }

        private void GenericFilterOnInterfaceFields<T>() where T:IRecordWithInterface
        {
            var dataSet = DB.DataSet<T>();

            long n = dataSet.Count(rec => rec.Name == "B");

            Assert.That(n, Is.EqualTo(1));

            n = dataSet.Count(rec => rec.Name == "D");

            Assert.That(n, Is.EqualTo(0));
        }




        [Test]
        public void InsertOrUpdate_NormalKey_Update()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            var rec = DB.RecordsWithSingleKey.Read(2);

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2"));

            rec.Name = "2A";

            var success = DB.RecordsWithSingleKey.InsertOrUpdate(rec);

            Assert.True(success);

            rec = DB.RecordsWithSingleKey.Read(2);

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2A"));
        }

        [Test]
        public void InsertOrUpdate_NormalKey_Insert()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            var rec = DB.RecordsWithSingleKey.Read(2);

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2"));

            rec.Key = 101;

            var success = DB.RecordsWithSingleKey.InsertOrUpdate(rec);

            Assert.True(success);

            rec = DB.RecordsWithSingleKey.Read(101);

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2"));
        }

    }
}