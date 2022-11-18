using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class ProjectionTests : TestFixtureWithEmptyDB
    {
        public ProjectionTests(string driver) : base(driver)
        {
        }

        private class Wrapper<T>
        {
            private T _obj;

            public Wrapper(T obj)
            {
                _obj = obj;
            }

            public T Obj
            {
                get => _obj;
                set => _obj = value;
            }
        }

        [SetUp]
        public void CreateData()
        {
            var salesPeople = InsertRecords<SalesPerson>(10, (person, i) =>
            {
                person.Name = "SP" + i;
                person.SalesPersonType = SalesPersonType.Internal;
            });

            var customers = InsertRecords<Customer>(10, (customer, i) =>
            {
                customer.Name = $"Customer {(char)('A' + (i - 1))}";
                customer.Age = i + 1;
            });

            InsertRecords<Order>(10, (order, i) =>
            {
                order.CustomerID = customers[i - 1].CustomerID;
                order.Remark = "xyz" + i;
                order.SalesPersonID = salesPeople[i - 1].ID;
            });
        }

        [Test]
        public void SingleField()
        {
            var orders = DB.Orders.OrderBy(o => o.OrderID);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var results = orders.Select(o => o.Remark).ToList();

                Assert.That(results[0], Is.EqualTo("xyz1"));
                Assert.That(results.Count, Is.EqualTo(10));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(1));
            }
        }

        [Test]
        public void SingleFieldExpression()
        {
            var orders = DB.Orders.OrderBy(o => o.OrderID);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var results = orders.Select(o => o.Remark + "a").ToList();

                Assert.That(results[0], Is.EqualTo("xyz1a"));
                Assert.That(results.Count, Is.EqualTo(10));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(1));
            }
        }

        [Test]
        public void TwoFieldExpression()
        {
            var customers = DB.Customers.OrderBy(o => o.CustomerID);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var results = customers.Select(c => new { age = c.Age*2, name = c.Name }).ToList();

                Assert.That(results[0].age, Is.EqualTo(4));
                Assert.That(results[1].age, Is.EqualTo(6));
                Assert.That(results[0].name, Is.EqualTo("Customer A"));
                Assert.That(results.Count, Is.EqualTo(10));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(2));
            }
        }

        [Test]
        public void SingleFieldFromRelation()
        {
            var orders = DB.Orders.OrderBy(o => o.OrderID);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var results = orders.Select(o => o.Customer.Name).ToList();

                Assert.That(results[0], Is.EqualTo("Customer A"));
                Assert.That(results[1], Is.EqualTo("Customer B"));
                Assert.That(results.Count, Is.EqualTo(10));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(2)); // one extra field for the primary key of the related record
            }
        }

        [Test]
        public void SingleFieldPlusFieldFromRelation()
        {
            var orders = DB.Orders.OrderBy(o => o.OrderID);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var results = orders.Select(o => new { o.Remark, o.Customer.Name }).ToList();

                Assert.That(results[0].Name, Is.EqualTo("Customer A"));
                Assert.That(results[0].Remark, Is.EqualTo("xyz1"));
                Assert.That(results[1].Name, Is.EqualTo("Customer B"));
                Assert.That(results.Count, Is.EqualTo(10));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(3)); // one extra field for the primary key of the related record
            }
        }

        [Test]
        public void SingleFieldPlusRelatedRecord()
        {
            var orders = DB.Orders.OrderBy(o => o.OrderID);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var results = orders.Select(o => new { o.Remark, o.Customer }).ToList();

                Assert.That(results[0].Customer.Name, Is.EqualTo("Customer A"));
                Assert.That(results[0].Remark, Is.EqualTo("xyz1"));
                Assert.That(results[1].Customer.Name, Is.EqualTo("Customer B"));
                Assert.That(results.Count, Is.EqualTo(10));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(4)); // one extra field for the primary key of the related record
            }
        }


        [Test]
        public void Test1()
        {
            var orders = DB.Orders.OrderBy(c => c.Customer.Name);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var ids = orders.Select(c => new { age = c.Customer.Age * 2, remark = c.Remark }).ToList();

                Assert.That(ids[0].age, Is.EqualTo(4));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(3)); // one extra field for the primary key of the related record
            }

            using (var loggingContext = DB.StartSqlLogging())
            {
                var ids = orders.Select(c => new { age = c.Customer.Age * 2, remark = c.Remark, customer = c.Customer }).ToList();

                int id = ids[0].customer.CustomerID;
                string sql = loggingContext.LogEntries.First().Sql;

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(4));
            }

            using (var loggingContext = DB.StartSqlLogging())
            {
                var ids = orders.WithRelations(c => c.Customer).Select(c => c).ToList();

                Assert.That(ids[0].Customer.CustomerID, Is.EqualTo(1));
                Assert.That(ids[0].Customer.Age, Is.EqualTo(2));

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(8));
            }

        }

        [Test]
        public void Test2()
        {
            var filtererdCustomers = DB.Customers.OrderBy(c => c.Name);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var recs = filtererdCustomers.Select(c => c.Name).ToList();

                Assert.That(recs[0], Is.EqualTo("Customer A"));

                string sql = loggingContext.LogEntries.First().Sql;

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(1));

            }

            using (var loggingContext = DB.StartSqlLogging())
            {
                var recs = filtererdCustomers.Select(c => new { orders = c.Orders, age = c.Age * 2, name=c.Name }).ToList();

                string sql = loggingContext.LogEntries.Last().Sql;

                Assert.That(recs[0].orders.Count, Is.EqualTo(1));


                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(3));
            }

            using (var loggingContext = DB.StartSqlLogging())
            {
                var recs = filtererdCustomers.OrderBy(c => c.CustomerID).Select(c => new Wrapper<Customer>(c)).ToList();

                Assert.That(recs[0].Obj.CustomerID, Is.EqualTo(1));
                Assert.That(recs[0].Obj.Name, Is.EqualTo("Customer A"));
                Assert.That(recs[0].Obj.Age, Is.EqualTo(2));

                string sql = loggingContext.LogEntries.First().Sql;

                Assert.That(NumFieldsQueried(loggingContext), Is.EqualTo(3));
            }

        }

        private int NumFieldsQueried(SqlLoggingContext loggingContext)
        {
            string sql = loggingContext.LogEntries.First().Sql;

            int fromIndex = sql.IndexOf("from", StringComparison.OrdinalIgnoreCase);

            if (fromIndex < 0)
            {
                return 0;
            }

            return Regex.Matches(sql.Substring(0, fromIndex), "\\.").Count;
        }

        [Test]
        public void TestDistinct()
        {
            DB.PurgeAll();

            InsertRecord(new Customer() { Name = "Customer A", Age = 20 });
            InsertRecord(new Customer() { Name = "Customer B", Age = 21 });
            InsertRecord(new Customer() { Name = "Customer B", Age = 22 });
            InsertRecord(new Customer() { Name = "Customer C", Age = 23 });
            InsertRecord(new Customer() { Name = "Customer C", Age = 23 });

            var customers = DB.Customers;

            using (var loggingContext = DB.StartSqlLogging())
            {
                var names = customers.Select(c => c.Name).Distinct().ToList();

                Assert.That(names.Count, Is.EqualTo(3));

                var ages = customers.Select(c => c.Age).Distinct().ToList();

                Assert.That(ages.Count, Is.EqualTo(4));

                var combos = customers.Select(c => new {name=c.Name, age = c.Age}).Distinct().ToList();

                Assert.That(combos.Count, Is.EqualTo(4));

            }
        }

        [Test]
        public async Task TestDistinctAsync()
        {
            DB.PurgeAll();

            InsertRecord(new Customer() { Name = "Customer A", Age = 20 });
            InsertRecord(new Customer() { Name = "Customer B", Age = 21 });
            InsertRecord(new Customer() { Name = "Customer B", Age = 22 });
            InsertRecord(new Customer() { Name = "Customer C", Age = 23 });
            InsertRecord(new Customer() { Name = "Customer C", Age = 23 });

            var customers = DB.Customers;

            using (var loggingContext = DB.StartSqlLogging())
            {
                var names = await customers.Async().Select(c => c.Name).Distinct().ToList();

                Assert.That(names.Count, Is.EqualTo(3));

                var ages = await customers.Async().Select(c => c.Age).Distinct().ToList();

                Assert.That(ages.Count, Is.EqualTo(4));

                var combos = await customers.Async().Select(c => new { name = c.Name, age = c.Age }).Distinct().ToList();

                Assert.That(combos.Count, Is.EqualTo(4));
            }
        }




    }
}