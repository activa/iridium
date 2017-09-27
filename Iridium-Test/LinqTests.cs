using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("sqlite")]
    [TestFixture("sqlitemem")]
    [TestFixture("sqlserver")]
    [TestFixture("memory")]
    [TestFixture("mysql")]
    [TestFixture("postgres")]
    public class LinqTests : TestFixture
    {
        public LinqTests(string driver) : base(driver)
        {
            DB.CreateAllTables();
        }

        [SetUp]
        public void SetupTest()
        {
            DB.PurgeAll();
        }

        [Test]
        public void Linq_Count()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            long n = DB.RecordsWithSingleKey.Count();

            Assert.That(n, Is.EqualTo(10));
        }

        [Test]
        public void Linq_Count_Predicate()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            long n = DB.RecordsWithSingleKey.Count(r => r.Key < 5);

            Assert.That(n, Is.EqualTo(4));
        }

        [Test]
        public void Linq_Count_WherePredicate()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            // ReSharper disable once ReplaceWithSingleCallToCount
            long n = DB.RecordsWithSingleKey.Where(r => r.Key < 5).Count();

            Assert.That(n, Is.EqualTo(4));
        }

        [Test]
        public void Max_Int()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntField), Is.EqualTo(10));
        }

        [Test]
        public void Max_IntNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_IntNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntFieldNullable), Is.Null);
        }

        [Test]
        public void Max_IntNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntFieldNullable), Is.Null);
        }


    }
}