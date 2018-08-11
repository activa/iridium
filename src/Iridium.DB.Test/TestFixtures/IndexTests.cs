using System;
using NUnit.Framework;

namespace Iridium.DB.Test
{
//    [TestFixture("memory", Category = "memory")]
    [TestFixture("sqlitemem", Category = "sqlite-mem")]
    [TestFixture("sqlserver", Category = "sqlserver")]
    [TestFixture("sqlite", Category = "sqlite")]
//    [TestFixture("mysql", Category = "mysql")]
    [TestFixture("postgres", Category = "postgres")]
    public class IndexTests : TestFixtureWithEmptyDB
    {
        public IndexTests(string driver) : base(driver)
        {
        }

        [Test]
        public void UniqueIndex()
        {
            var records = DB.DataSet<RecordWithUniqueIndex>();

            Assert.That(records.Count(), Is.Zero);

            records.Insert(new RecordWithUniqueIndex() {Name = "ABC"});
            Assert.That(records.Count(), Is.EqualTo(1));

            records.Insert(new RecordWithUniqueIndex() {Name = "XYZ"});
            Assert.That(records.Count(), Is.EqualTo(2));

            try
            {
                records.Insert(new RecordWithUniqueIndex() {Name = "ABC"});

                Assert.Fail("Adding duplicate record succeeded");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch
            {
            }

            Assert.That(records.Count(), Is.EqualTo(2));
        }

    }
}