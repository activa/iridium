using System;
using System.Linq;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "memory")]
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class RelationTests : TestFixtureWithEmptyDB
    {
        public RelationTests(string driver) : base(driver)
        {
        }

        [Test]
        public void ManyToOne()
        {
            var customer = InsertRecord(new Customer { Name = "x" });
            var salesPerson = InsertRecord(new SalesPerson { Name = "Test" });

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
                Customer = new Customer { Name = "A" },
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
            Customer customer = new Customer() { Name = "A" };

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

            rec1 = DB.Read<OneToOneRec1>(rec1.OneToOneRec1ID, r => r.Rec2);

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
        public void ManyToOne_NoPreload()
        {
            var parent1 = InsertRecord(new RecordWithChildren() {Name = "A"});
            var parent2 = InsertRecord(new RecordWithChildren() {Name = "B"});

            InsertRecords<RecordWithParent>(10, (child, i) => { child.Name = "X"; child.ParentKey = parent1.Key; });
            InsertRecords<RecordWithParent>(10, (child, i) => { child.Name = "Y"; child.ParentKey = parent2.Key; });

            var children1 = DB.RecordsWithParent.ToArray();

            Assert.That(children1[0].Parent, Is.Null);

        }

        [Test]
        public void ManyToOne_ManualPreload()
        {
            var parent1 = InsertRecord(new RecordWithChildren() {Name = "A"});
            var parent2 = InsertRecord(new RecordWithChildren() {Name = "B"});

            InsertRecords<RecordWithParent>(10, (child, i) => { child.Name = "X"; child.ParentKey = parent1.Key; });
            InsertRecords<RecordWithParent>(10, (child, i) => { child.Name = "Y"; child.ParentKey = parent2.Key; });

            var children1 = DB.RecordsWithParent.Where(c => c.Name == "X").WithRelations(r => r.Parent).ToArray();

            Assert.That(children1[0].Parent, Is.Not.Null);
            Assert.That(children1[0].Parent.Name, Is.EqualTo("A"));

        }

        [Test]
        public void ManyToOne_AttributePreload()
        {
            var parent1 = InsertRecord(new RecordWithChildren() {Name = "A"});
            var parent2 = InsertRecord(new RecordWithChildren() {Name = "B"});

            InsertRecords<RecordWithPreloadParent>(10, (child, i) => { child.Name = "X"; child.ParentKey = parent1.Key; });
            InsertRecords<RecordWithPreloadParent>(10, (child, i) => { child.Name = "Y"; child.ParentKey = parent2.Key; });

            var children1 = DB.RecordsWithPreloadParent.Where(c => c.Name == "X").ToArray();

            Assert.That(children1[0].Parent, Is.Not.Null);
            Assert.That(children1[0].Parent.Name, Is.EqualTo("A"));

        }


    }
}