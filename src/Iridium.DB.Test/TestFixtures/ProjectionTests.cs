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

        [Test]
        public void Test1()
        {
            var customers = InsertRecords<Customer>(10, (customer, i) =>
            {
                customer.Name = $"Customer {(char)('A' + (i-1))}";
                customer.Age = i + 1;
            });

            InsertRecords<Order>(10, (order, i) =>
            {
                order.CustomerID = customers[i-1].CustomerID;
                order.Remark = "xyz";
            });

            var orders = DB.Orders.OrderBy(c => c.Customer.Name);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var ids = orders.Select(c => new { age = c.Customer.Age * 2, remark = c.Remark }).ToList();

                Assert.That(ids[0].age, Is.EqualTo(4));

                string sql = loggingContext.LogEntries.First().Sql;

                Assert.That(NumFieldsQueried(sql), Is.EqualTo(3)); // one extra field for the primary key of the related record
            }

            using (var loggingContext = DB.StartSqlLogging())
            {
                var ids = orders.Select(c => new { age = c.Customer.Age * 2, remark = c.Remark, customer = c.Customer }).ToList();

                int id = ids[0].customer.CustomerID;
                string sql = loggingContext.LogEntries.First().Sql;

                Assert.That(NumFieldsQueried(sql), Is.EqualTo(4));
            }
        }

        [Test]
        public void Test2()
        {
            var customers = InsertRecords<Customer>(10, (customer, i) =>
            {
                customer.Name = $"Customer {(char)('A' + (i - 1))}";
                customer.Age = i + 1;
            });

            InsertRecords<Order>(10, (order, i) =>
            {
                order.CustomerID = customers[i - 1].CustomerID;
                order.Remark = "xyz";
            });

            var filtererdCustomers = DB.Customers.OrderBy(c => c.Name);

            using (var loggingContext = DB.StartSqlLogging())
            {
                var recs = filtererdCustomers.Select(c => c.Name).ToList();

                Assert.That(recs[0], Is.EqualTo("Customer A"));

                string sql = loggingContext.LogEntries.First().Sql;

                Assert.That(NumFieldsQueried(sql), Is.EqualTo(1));

            }

            using (var loggingContext = DB.StartSqlLogging())
            {
                var recs = filtererdCustomers.Select(c => new { orders = c.Orders, age = c.Age * 2, name=c.Name }).ToList();

                string sql = loggingContext.LogEntries.Last().Sql;

                Assert.That(recs[0].orders.Count, Is.EqualTo(1));


                Assert.That(NumFieldsQueried(sql), Is.EqualTo(3));
            }

            using (var loggingContext = DB.StartSqlLogging())
            {
                var recs = filtererdCustomers.OrderBy(c => c.CustomerID).Select(c => new Wrapper<Customer>(c)).ToList();

                Assert.That(recs[0].Obj.CustomerID, Is.EqualTo(1));
                Assert.That(recs[0].Obj.Name, Is.EqualTo("Customer A"));
                Assert.That(recs[0].Obj.Age, Is.EqualTo(2));

                string sql = loggingContext.LogEntries.First().Sql;

                Assert.That(NumFieldsQueried(sql), Is.EqualTo(3));
            }

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

        private int NumFieldsQueried(string sql)
        {
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