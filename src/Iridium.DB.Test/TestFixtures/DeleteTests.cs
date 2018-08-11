using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "memory")]
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class DeleteTests : TestFixtureWithEmptyDB
    {
        public DeleteTests(string driver) : base(driver)
        {
        }

        [Test]
        public void DeleteSingleObject()
        {
            var records = InsertRecords<RecordWithAutonumKey>(10, (c, i) => c.Name = "Customer " + i);

            DB.RecordsWithAutonumKey.Delete(records[5]);

            Assert.IsNull(DB.Customers.Read(records[5].Key));

            Assert.AreEqual(9, DB.RecordsWithAutonumKey.Count());
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
                Order rec = new Order();

                rec.Customer = new Customer
                {
                    Name = "Customer " + (i + 1)
                };
                rec.Remark = "Remark" + (i + 1);

                DB.Orders.Insert(rec, deferSave: false, relationsToSave: o => o.Customer);

                orders.Add(rec);
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

            Assert.That(DB.Customers.Count(), Is.Zero);
        }


    }
}