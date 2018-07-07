using System;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "embedded")]
    [TestFixture("sqlitemem", Category = "embedded")]
    [TestFixture("sqlserver", Category = "server")]
    [TestFixture("sqlite", Category = "file")]
    [TestFixture("mysql", Category = "server")]
    [TestFixture("postgres", Category = "server")]
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

            DB.RecordsWithAutonumKey.Events.ObjectCreated += (sender, args) => { counter1++; };
            DB.RecordsWithAutonumKey.Events.ObjectCreated += (sender, args) => { counter2++; };

            InsertRecord(new RecordWithAutonumKey() { Name = "A" });
            InsertRecord(new RecordWithAutonumKey() { Name = "A" });

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
                bool saveResult = DB.Insert(new Customer { Name = "A" });

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

    }
}