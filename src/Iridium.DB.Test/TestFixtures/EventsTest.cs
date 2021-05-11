using System;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "memory")]
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class EventsTest : TestFixtureWithEmptyDB
    {
        public EventsTest(string driver) : base(driver)
        {
        }

        [Test]
        public void Events_ObjectCreated()
        {
            int counter1 = 0;
            int counter2 = 0;

            DB.RecordsWithAutonumKey.Events.Created.Add(_ => counter1++);
            DB.RecordsWithAutonumKey.Events.Created.Add(_ => counter2++);

            InsertRecord(new RecordWithAutonumKey() { Name = "A" });
            InsertRecord(new RecordWithAutonumKey() { Name = "A" });

            Assert.That(counter1, Is.EqualTo(2));
            Assert.That(counter2, Is.EqualTo(2));
        }

        [Test]
        public void Events_ObjectCreated_Interface()
        {
            int counter1 = 0;

            var dataSet = DB.DataSet<RecordWithInterface, IRecordWithInterface>();

            dataSet.Events.Created.Add(_ => counter1++);

            dataSet.Insert(new RecordWithInterface() {Name = "A"});

            Assert.That(counter1, Is.EqualTo(1));
        }

        [Test]
        public void Events_ObjectCreating()
        {
            int counter = 0;

            bool ev1(Customer _) { counter++; return true; }
            bool ev2(Customer _) { counter++; return true; }
            
            DB.Customers.Events.Creating.Add(ev1);
            DB.Customers.Events.Creating.Add(ev2);

            bool saveResult;

            try
            {
                saveResult = DB.Insert(new Customer { Name = "A" });

                Assert.That(saveResult, Is.True);
                Assert.That(DB.Customers.FirstOrDefault(c => c.Name == "A"), Is.Not.Null);
                Assert.That(counter, Is.EqualTo(2));
            }
            finally
            {
                DB.Customers.Events.Creating.Remove(ev1);
                DB.Customers.Events.Creating.Remove(ev2);
            }

            counter = 0;
            saveResult = DB.Insert(new Customer { Name = "B" });

            Assert.That(saveResult, Is.True);
            Assert.That(DB.Customers.FirstOrDefault(c => c.Name == "B"), Is.Not.Null);
            Assert.That(counter, Is.EqualTo(0));


        }

        [Test]
        public void Events_ObjectCreatingWithCancel1()
        {
            int counter = 0;

            bool ev(Customer _) { counter++; return true; }
            bool evWithCancel(Customer _) { counter++; return false; }

            DB.Customers.Events.Creating.Add(ev);
            DB.Customers.Events.Creating.Add(evWithCancel);

            try
            {
                bool saveResult = DB.Insert(new Customer() { Name = "A" });

                Assert.That(DB.Customers.FirstOrDefault(c => c.Name == "A"), Is.Null);
                Assert.That(saveResult, Is.False);
                Assert.That(counter, Is.EqualTo(2));
            }
            finally
            {
                DB.Customers.Events.Creating.Remove(ev);
                DB.Customers.Events.Creating.Remove(evWithCancel);
            }
        }

        [Test]
        public void Events_ObjectCreatingWithCancel2()
        {
            int counter = 0;

            bool ev(Customer _) { counter++; return true; }
            bool evWithCancel(Customer _) { counter++; return false; }

            DB.Customers.Events.Creating.Add(evWithCancel);
            DB.Customers.Events.Creating.Add(ev);

            try
            {
                bool saveResult = DB.Insert(new Customer() { Name = "A" });

                Assert.That(saveResult, Is.False);
                Assert.That(DB.Customers.FirstOrDefault(c => c.Name == "A"), Is.Null);
                Assert.That(counter, Is.EqualTo(1));
            }
            finally
            {
                DB.Customers.Events.Creating.Remove(ev);
                DB.Customers.Events.Creating.Remove(evWithCancel);
            }
        }

    }
}