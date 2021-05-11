using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "memory")]
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class BasicTests : TestFixtureWithEmptyDB
    {
        public BasicTests(string driver) : base(driver)
        {
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

                    var newCustomer = DB.Customers.Read(customer.CustomerID);

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

                DB.Delete(customer);

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

            DB.Update(customer);

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

            DB.Insert(cust);

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

            DB.Update(order.Customer);


            order = DB.Orders.Read(order.OrderID);

            DB.LoadRelations(() => order.Customer);

            Assert.AreEqual(order.CustomerID, cust.CustomerID);

            Assert.AreEqual("B", order.Customer.Name);
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

            Assert.IsTrue(DB.Orders.Insert(order, o => o.Customer, o => o.OrderItems));

            order = DB.Orders.Read(order.OrderID, o => o.OrderItems);

            Assert.AreEqual(2, order.OrderItems.Count(), "Order items not added");

            order.OrderItems.Add(new OrderItem { Description = "test", Qty = 2, Price = 1000.0 });

            Assert.IsTrue(DB.Orders.Update(order, o => o.OrderItems));

            order = DB.Orders.Read(order.OrderID, o => o.OrderItems);

            Assert.AreEqual(3, order.OrderItems.Count(), "Order item not added");

            order.OrderItems.Add(new OrderItem { Description = "test", Qty = 3, Price = 2000.0 });

            Assert.IsTrue(DB.Orders.Update(order, o => o.OrderItems));

            order = DB.Orders.Read(order.OrderID, o=>o.OrderItems);

            Assert.AreEqual(4, order.OrderItems.Count(), "Order item not added");
        }

        [Test]
        public void TwoLevelToManyCreation()
        {
            Parent parent = new Parent()
            {
                Name = "Parent 1",
                Children = new List<Child>()
                {
                    new Child()
                    {
                        Name = "Child 1A",
                        Children = new List<GrandChild>()
                        {
                            new GrandChild() {Name = "Grandchild 1AX"},
                            new GrandChild() {Name = "Grandchild 1AY"},
                            new GrandChild() {Name = "Grandchild 1AZ"},
                        }
                    },
                    new Child()
                    {
                        Name = "Child 1B",
                        Children = new List<GrandChild>()
                        {
                            new GrandChild() {Name = "Grandchild 1BX"},
                            new GrandChild() {Name = "Grandchild 1BY"},
                            new GrandChild() {Name = "Grandchild 1BZ"},
                        }
                    },
                }
            };

            DB.DataSet<Parent>().Purge();
            DB.DataSet<Child>().Purge();
            DB.DataSet<GrandChild>().Purge();

            DB.Insert(parent, p => p.Children.With(c => c.Children));

            Assert.That(DB.DataSet<Parent>().Count(), Is.EqualTo(1));
            Assert.That(DB.DataSet<Child>().Count(), Is.EqualTo(2));
            Assert.That(DB.DataSet<GrandChild>().Count(), Is.EqualTo(6));
        }

        [Test]
        public void BetweenInt()
        {
            var customers = InsertRecords<Customer>(100, (customer, i) =>
            {
                customer.Name = $"Customer {(char)('A' + 1)}";
                customer.Age = i + 1;
            });

            var filtererdCustomers = DB.Customers.Where(c => c.Age.IsBetween(12,20)).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(9));

            Assert.That(filtererdCustomers.Select(c => c.Age.Value), Is.EquivalentTo(new[] {12,13,14,15,16,17,18,19,20}));
        }

        [Test]
        public void BetweenStrings()
        {
            var customers = InsertRecords<Customer>(20, (customer, i) =>
            {
                customer.Name = $"Customer {(char)('A' + i)}";
                customer.Age = i + 1;
            });

            var filtererdCustomers = DB.Customers.Where(c => c.Name.IsBetween("Customer D","Customer H")).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(5));

            Assert.That(filtererdCustomers[0].Name, Is.EqualTo("Customer D"));
            Assert.That(filtererdCustomers[4].Name, Is.EqualTo("Customer H"));
        }

        [Test]
        public void IsAnyOfIntArray()
        {
            var customers = InsertRecords<Customer>(100, (customer, i) => customer.Name = $"Customer {i + 1}");

            //var someCustomerIds = customers.Skip(5).Take(50).Select(c => c.CustomerID).ToArray();

            var filtererdCustomers = DB.Customers.Where(c => c.CustomerID.IsAnyOf(customers.Skip(5).Take(50).Select(c2 => c2.CustomerID))).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(50));

            Assert.That(filtererdCustomers.Select(c => c.CustomerID), Is.EquivalentTo(customers.Skip(5).Take(50).Select(c => c.CustomerID)));
        }

        [Test]
        public void IsAnyOfIntEnumerable()
        {
            var customers = InsertRecords<Customer>(100, (customer, i) => customer.Name = $"Customer {i + 1}");

            var someCustomerIds = customers.Skip(5).Take(50).Select(c => c.CustomerID);

            var filtererdCustomers = DB.Customers.Where(c => c.CustomerID.IsAnyOf(someCustomerIds)).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(50));

            Assert.That(filtererdCustomers.Select(c => c.CustomerID), Is.EquivalentTo(someCustomerIds));
        }

        [Test]
        public void IsAnyOfIntParams()
        {
            var customers = InsertRecords<Customer>(100, (customer, i) =>
            {
                customer.Age = i + 1;
                customer.Name = $"Customer {i + 1}";
            });

            var filtererdCustomers = DB.Customers.Where(c => c.Age.IsAnyOf(12,13,14)).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(3));

            Assert.That(filtererdCustomers.Select(c => c.Age.Value), Is.EquivalentTo(new[] {12,13,14}));
        }

        [Test]
        public void NotIsAnyOfIntParams()
        {
            var customers = InsertRecords<Customer>(100, (customer, i) =>
            {
                customer.Age = i + 1;
                customer.Name = $"Customer {i + 1}";
            });

            var filtererdCustomers = DB.Customers.Where(c => !c.Age.IsAnyOf(12,13,14)).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(97));
        }

        [Test]
        public void IsNotAnyOfIntParams()
        {
            var customers = InsertRecords<Customer>(100, (customer, i) =>
            {
                customer.Age = i + 1;
                customer.Name = $"Customer {i + 1}";
            });

            var filtererdCustomers = DB.Customers.Where(c => c.Age.IsNotAnyOf(12,13,14)).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(97));
        }

        [Test]
        public void AnyOfStringParams()
        {
            var customers = InsertRecords<Customer>(10, (customer, i) => customer.Name = $"Customer {i + 1}");

            var filtererdCustomers = DB.Customers.Where(c => c.Name.IsAnyOf("Customer 3","Customer 6","Customer 8")).OrderBy(c => c.CustomerID).ToArray();

            Assert.That(filtererdCustomers.Length, Is.EqualTo(3));
            Assert.That(filtererdCustomers[0].Name, Is.EqualTo("Customer 3"));
            Assert.That(filtererdCustomers[1].Name, Is.EqualTo("Customer 6"));
            Assert.That(filtererdCustomers[2].Name, Is.EqualTo("Customer 8"));
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
                DB.LoadRelations(order, o => o.Customer, o => o.OrderItems);

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

        [Test]
        public void FilterOnInterfaceFields2()
        {
            var dataSet = DB.DataSet<RecordWithInterface, IRecordWithInterface>();

            dataSet.Insert(new RecordWithInterface() { Name = "A" });
            dataSet.Insert(new RecordWithInterface() { Name = "B" });
            dataSet.Insert(new RecordWithInterface() { Name = "C" });

            long n = dataSet.Count(rec => rec.Name == "B");

            Assert.That(n, Is.EqualTo(1));

            n = dataSet.Count(rec => rec.Name == "D");

            Assert.That(n, Is.EqualTo(0));
        }

        [Test]
        public void FilterOnInterfaceFieldsDynamicType()
        {
            var dataSet = DB.DataSet<IRecordWithInterface>(typeof(RecordWithInterface));

            dataSet.Insert(new RecordWithInterface() { Name = "A" });
            dataSet.Insert(new RecordWithInterface() { Name = "B" });
            dataSet.Insert(new RecordWithInterface() { Name = "C" });

            long n = dataSet.Count(rec => rec.Name == "B");

            Assert.That(n, Is.EqualTo(1));

            n = dataSet.Count(rec => rec.Name == "D");

            Assert.That(n, Is.EqualTo(0));
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

        [Test]
        public void InsertOrUpdate_CompositeKey_Update()
        {
            InsertRecords<RecordWithCompositeKey>(10, (r, i) => { r.Key1 = i; r.Key2 = i+100; r.Name = i.ToString(); });

            var rec = DB.RecordsWithCompositeKey.Read(new {Key1=2,Key2=102});

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2"));

            rec.Name = "2A";

            var success = DB.RecordsWithCompositeKey.InsertOrUpdate(rec);

            Assert.True(success);

            rec = DB.RecordsWithCompositeKey.Read(new { Key1 = 2, Key2 = 102 });

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2A"));
        }

        [Test]
        public void InsertOrUpdate_CommpositeKey_Insert()
        {
            InsertRecords<RecordWithCompositeKey>(10, (r, i) => { r.Key1 = i; r.Key2 = i + 100; r.Name = i.ToString(); });

            var rec = DB.RecordsWithCompositeKey.Read(new { Key1 = 2, Key2 = 102 });

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2"));

            rec.Key1 = 1001;

            var success = DB.RecordsWithCompositeKey.InsertOrUpdate(rec);

            Assert.True(success);

            rec = DB.RecordsWithCompositeKey.Read(new { Key1 = 1001, Key2 = 102 });

            Assert.That(rec, Is.Not.Null);
            Assert.That(rec.Name, Is.EqualTo("2"));
        }

        [Test]
        public void AfterReadActions()
        {
            InsertRecords<Customer>(10, (r, i) => { r.Name = "Customer " + i; });

            var customers = DB.Customers.WithActions(c => c.Age = c.Name.Length, c => c.Name = c.Name.Substring(1));

            foreach (var customer in customers)
            {
                Assert.That(customer.Age == customer.Name.Length+1);
            }

        }

        [Test]
        public void SqlLogging()
        {
            List<TimedSqlLogEntry> sqlStatements = new List<TimedSqlLogEntry>();

            if (DB.DataProvider is ISqlDataProvider sqlProvider)
            {
                sqlProvider.SqlLogger = new SqlToTextLogger(entry => sqlStatements.Add(entry));

                DB.Insert(new Customer() {Name = "me"});

                Assert.That(sqlStatements.Count, Is.Not.Zero);

                sqlProvider.SqlLogger = null;
                sqlStatements.Clear();

                DB.Insert(new Customer() { Name = "me" });

                Assert.That(sqlStatements.Count, Is.Zero);
            }
        }

        [Test]
        public void SqlLoggingContext()
        {
            if (DB.DataProvider is ISqlDataProvider sqlProvider)
            {
                using (var sqlLogger = DB.StartSqlLogging())
                {
                    Assert.That(sqlLogger.LogEntries.Count, Is.Zero);
                    Assert.That(sqlLogger.TotalTime.TotalMilliseconds, Is.Zero);

                    DB.Insert(new Customer() { Name = "me" });

                    Assert.That(sqlLogger.LogEntries.Count, Is.Not.Zero);
                    Assert.That(sqlLogger.TotalTime.TotalMilliseconds, Is.GreaterThan(0.0));
                }

                Assert.That(sqlProvider.SqlLogger, Is.Null);
            }
        }

    }

}