using System;
using System.Linq;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("memory", Category = "embedded")]
    [TestFixture("sqlitemem", Category = "embedded")]
    [TestFixture("sqlserver", Category = "server")]
    [TestFixture("sqlite", Category = "file")]
    [TestFixture("mysql", Category = "server")]
    [TestFixture("postgres", Category = "server")]
    public class LinqAggregateTests : TestFixture
    {
        public LinqAggregateTests(string driver) : base(driver)
        {
        }

        [SetUp]
        public void SetupTest()
        {
            DB.RecordsWithSingleKey.DeleteAll();
            DB.RecordsWithAllTypes.DeleteAll();
            DB.RecordsWithParent.DeleteAll();
            DB.RecordsWithChildren.DeleteAll();
        }


        [Test]
        public void Count()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            long n = DB.RecordsWithSingleKey.Count();

            Assert.That(n, Is.EqualTo(10));
        }

        [Test]
        public void Count_Filtered()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            long n = DB.RecordsWithSingleKey.Count(r => r.Key < 5);

            Assert.That(n, Is.EqualTo(4));
        }

        [Test]
        public void Count_Filtered_Where()
        {
            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            // ReSharper disable once ReplaceWithSingleCallToCount
            long n = DB.RecordsWithSingleKey.Where(r => r.Key < 5).Count();

            Assert.That(n, Is.EqualTo(4));
        }

        
        [Test]
        public void Count_Filtered_PartialNative()
        {
            Func<RecordWithSingleKey,bool> filter = rec => rec.Key > 2;

            InsertRecords<RecordWithSingleKey>(10, (r, i) => { r.Key = i; r.Name = i.ToString(); });

            long n = DB.RecordsWithSingleKey.Count(r => r.Key < 5 && filter(r));

            Assert.That(n, Is.EqualTo(2));
        }

        [Test]
        public void Max_Int()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntField), Is.EqualTo(10));
        }

        [Test]
        public void Max_Int_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntField, r => r.IntField < 5), Is.EqualTo(4));
        }

        [Test]
        public void Max_Int_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.IntField < 5).Max(r => r.IntField), Is.EqualTo(4));
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
        public void Max_IntNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_IntNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Max(r => r.IntFieldNullable), Is.Null);
        }

        [Test]
        public void Max_Long()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.LongField), Is.EqualTo(10));
        }

        [Test]
        public void Max_Long_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.LongField, r => r.LongField < 5), Is.EqualTo(4));
        }

        [Test]
        public void Max_Long_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.LongField < 5).Max(r => r.LongField), Is.EqualTo(4));
        }

        [Test]
        public void Max_LongNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.LongFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_LongNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.LongFieldNullable), Is.Null);
        }

        [Test]
        public void Max_LongNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.LongFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_LongNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Max(r => r.LongFieldNullable), Is.Null);
        }

        [Test]
        public void Max_Double()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DoubleField), Is.EqualTo(10));
        }

        [Test]
        public void Max_Double_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DoubleField, r => r.DoubleField < 5), Is.EqualTo(4));
        }

        [Test]
        public void Max_Double_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DoubleField < 5).Max(r => r.DoubleField), Is.EqualTo(4));
        }

        [Test]
        public void Max_DoubleNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DoubleFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_DoubleNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DoubleFieldNullable), Is.Null);
        }

        [Test]
        public void Max_DoubleNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DoubleFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_DoubleNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DoubleFieldNullable), Is.Null);
        }

        [Test]
        public void Max_Float()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.FloatField), Is.EqualTo(10));
        }

        [Test]
        public void Max_Float_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.FloatField, r => r.FloatField < 5), Is.EqualTo(4));
        }

        [Test]
        public void Max_Float_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.FloatField < 5).Max(r => r.FloatField), Is.EqualTo(4));
        }

        [Test]
        public void Max_FloatNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.FloatFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_FloatNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.FloatFieldNullable), Is.Null);
        }

        [Test]
        public void Max_FloatNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.FloatFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_FloatNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Max(r => r.FloatFieldNullable), Is.Null);
        }

        [Test]
        public void Max_Decimal()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DecimalField), Is.EqualTo(10));
        }

        [Test]
        public void Max_Decimal_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DecimalField, r => r.DecimalField < 5), Is.EqualTo(4));
        }

        [Test]
        public void Max_Decimal_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DecimalField < 5).Max(r => r.DecimalField), Is.EqualTo(4));
        }

        [Test]
        public void Max_DecimalNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DecimalFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_DecimalNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DecimalFieldNullable), Is.Null);
        }

        [Test]
        public void Max_DecimalNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DecimalFieldNullable), Is.EqualTo(10));
        }

        [Test]
        public void Max_DecimalNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DecimalFieldNullable), Is.Null);
        }

        [Test]
        public void Max_DateTime()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeField = new DateTime(2000,1,i));

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DateTimeField), Is.EqualTo(new DateTime(2000, 1, 10)));
        }

        [Test]
        public void Max_DateTime_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeField = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DateTimeField, r => r.DateTimeField < new DateTime(2000, 1, 5)), Is.EqualTo(new DateTime(2000, 1, 4)));
        }

        [Test]
        public void Max_DateTime_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeField = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DateTimeField < new DateTime(2000, 1, 5)).Max(r => r.DateTimeField), Is.EqualTo(new DateTime(2000, 1, 4)));
        }

        [Test]
        public void Max_DateTimeNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DateTimeFieldNullable), Is.EqualTo(new DateTime(2000, 1, 10)));
        }

        [Test]
        public void Max_DateTimeNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DateTimeFieldNullable), Is.Null);
        }

        [Test]
        public void Max_DateTimeNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DateTimeFieldNullable), Is.EqualTo(new DateTime(2000, 1, 10)));
        }

        [Test]
        public void Max_DateTimeNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Max(r => r.DateTimeFieldNullable), Is.Null);
        }

        [Test]
        public void Min_Int()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.IntField), Is.EqualTo(1));
        }

        [Test]
        public void Min_Int_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.IntField, r => r.IntField > 5), Is.EqualTo(6));
        }

        [Test]
        public void Min_Int_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.IntField > 5).Min(r => r.IntField), Is.EqualTo(6));
        }

        [Test]
        public void Min_IntNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.IntFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_IntNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.IntFieldNullable), Is.Null);
        }

        [Test]
        public void Min_IntNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.IntFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_IntNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Min(r => r.IntFieldNullable), Is.Null);
        }

        [Test]
        public void Min_Long()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.LongField), Is.EqualTo(1));
        }

        [Test]
        public void Min_Long_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.LongField, r => r.LongField > 5), Is.EqualTo(6));
        }

        [Test]
        public void Min_Long_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.LongField > 5).Min(r => r.LongField), Is.EqualTo(6));
        }

        [Test]
        public void Min_LongNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.LongFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_LongNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.LongFieldNullable), Is.Null);
        }

        [Test]
        public void Min_LongNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.LongFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_LongNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Min(r => r.LongFieldNullable), Is.Null);
        }

        [Test]
        public void Min_Float()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.FloatField), Is.EqualTo(1));
        }

        [Test]
        public void Min_Float_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.FloatField, r => r.FloatField > 5), Is.EqualTo(6));
        }

        [Test]
        public void Min_Float_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.FloatField > 5).Min(r => r.FloatField), Is.EqualTo(6));
        }

        [Test]
        public void Min_FloatNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.FloatFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_FloatNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.FloatFieldNullable), Is.Null);
        }

        [Test]
        public void Min_FloatNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.FloatFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_FloatNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Min(r => r.FloatFieldNullable), Is.Null);
        }


        [Test]
        public void Min_Double()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DoubleField), Is.EqualTo(1));
        }

        [Test]
        public void Min_Double_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DoubleField, r => r.DoubleField > 5), Is.EqualTo(6));
        }

        [Test]
        public void Min_Double_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DoubleField > 5).Min(r => r.DoubleField), Is.EqualTo(6));
        }

        [Test]
        public void Min_DoubleNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DoubleFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_DoubleNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DoubleFieldNullable), Is.Null);
        }

        [Test]
        public void Min_DoubleNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DoubleFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_DoubleNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DoubleFieldNullable), Is.Null);
        }

        [Test]
        public void Min_Decimal()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DecimalField), Is.EqualTo(1));
        }

        [Test]
        public void Min_Decimal_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DecimalField, r => r.DecimalField > 5), Is.EqualTo(6));
        }

        [Test]
        public void Min_Decimal_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DecimalField > 5).Min(r => r.DecimalField), Is.EqualTo(6));
        }

        [Test]
        public void Min_DecimalNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DecimalFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_DecimalNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DecimalFieldNullable), Is.Null);
        }

        [Test]
        public void Min_DecimalNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DecimalFieldNullable), Is.EqualTo(1));
        }

        [Test]
        public void Min_DecimalNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DecimalFieldNullable), Is.Null);
        }



        [Test]
        public void Min_DateTime()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeField = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DateTimeField), Is.EqualTo(new DateTime(2000, 1, 1)));
        }

        [Test]
        public void Min_DateTime_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeField = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DateTimeField, r => r.DateTimeField > new DateTime(2000, 1, 5)), Is.EqualTo(new DateTime(2000, 1, 6)));
        }

        [Test]
        public void Min_DateTime_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeField = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DateTimeField > new DateTime(2000, 1, 5)).Min(r => r.DateTimeField), Is.EqualTo(new DateTime(2000, 1, 6)));
        }

        [Test]
        public void Min_DateTimeNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DateTimeFieldNullable), Is.EqualTo(new DateTime(2000, 1, 1)));
        }

        [Test]
        public void Min_DateTimeNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DateTimeFieldNullable), Is.Null);
        }

        [Test]
        public void Min_DateTimeNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DateTimeFieldNullable = new DateTime(2000, 1, i));

            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DateTimeFieldNullable), Is.EqualTo(new DateTime(2000, 1, 1)));
        }

        [Test]
        public void Min_DateTimeNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Min(r => r.DateTimeFieldNullable), Is.Null);
        }

        [Test]
        public void Average_Int()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.IntField), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_Int_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.IntField, r => r.IntField < 5), Is.EqualTo(2.5));
        }

        [Test]
        public void Average_Int_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.IntField < 5).Average(r => r.IntField), Is.EqualTo(2.5));
        }

        [Test]
        public void Average_IntNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.IntFieldNullable), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_IntNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.IntFieldNullable), Is.Null);
        }

        [Test]
        public void Average_IntNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.IntFieldNullable), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_IntNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Average(r => r.IntFieldNullable), Is.Null);
        }

        [Test]
        public void Average_Long()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.LongField), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_Long_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.LongField, r => r.LongField < 5), Is.EqualTo(2.5));
        }

        [Test]
        public void Average_Long_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.LongField < 5).Average(r => r.LongField), Is.EqualTo(2.5));
        }

        [Test]
        public void Average_LongNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.LongFieldNullable), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_LongNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.LongFieldNullable), Is.Null);
        }

        [Test]
        public void Average_LongNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.LongFieldNullable), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_LongNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Average(r => r.LongFieldNullable), Is.Null);
        }

        [Test]
        public void Average_Double()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DoubleField), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_Double_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DoubleField, r => r.DoubleField < 5), Is.EqualTo(2.5));
        }

        [Test]
        public void Average_Double_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DoubleField < 5).Average(r => r.DoubleField), Is.EqualTo(2.5));
        }

        [Test]
        public void Average_DoubleNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DoubleFieldNullable), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_DoubleNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DoubleFieldNullable), Is.Null);
        }

        [Test]
        public void Average_DoubleNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DoubleFieldNullable), Is.EqualTo(5.5));
        }

        [Test]
        public void Average_DoubleNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DoubleFieldNullable), Is.Null);
        }

        [Test]
        public void Average_Decimal()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DecimalField), Is.EqualTo(5.5m));
        }

        [Test]
        public void Average_Decimal_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DecimalField, r => r.DecimalField < 5), Is.EqualTo(2.5m));
        }

        [Test]
        public void Average_Decimal_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DecimalField < 5).Average(r => r.DecimalField), Is.EqualTo(2.5m));
        }

        [Test]
        public void Average_DecimalNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DecimalFieldNullable), Is.EqualTo(5.5m));
        }

        [Test]
        public void Average_DecimalNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DecimalFieldNullable), Is.Null);
        }

        [Test]
        public void Average_DecimalNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DecimalFieldNullable), Is.EqualTo(5.5m));
        }

        [Test]
        public void Average_DecimalNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Average(r => r.DecimalFieldNullable), Is.Null);
        }

        [Test]
        public void Sum_Int()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.IntField), Is.EqualTo(55));
        }

        [Test]
        public void Sum_Int_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.IntField, r => r.IntField < 5), Is.EqualTo(10));
        }

        [Test]
        public void Sum_Int_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.IntField < 5).Sum(r => r.IntField), Is.EqualTo(10));
        }

        [Test]
        public void Sum_IntNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.IntFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_IntNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.IntFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_IntNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.IntFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.IntFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_IntNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.IntFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_Long()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.LongField), Is.EqualTo(55));
        }

        [Test]
        public void Sum_Long_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.LongField, r => r.LongField < 5), Is.EqualTo(10));
        }

        [Test]
        public void Sum_Long_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.LongField < 5).Sum(r => r.LongField), Is.EqualTo(10));
        }

        [Test]
        public void Sum_LongNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.LongFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_LongNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.LongFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_LongNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.LongFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.LongFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_LongNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.LongFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_Double()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DoubleField), Is.EqualTo(55));
        }

        [Test]
        public void Sum_Double_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DoubleField, r => r.DoubleField < 5), Is.EqualTo(10));
        }

        [Test]
        public void Sum_Double_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DoubleField < 5).Sum(r => r.DoubleField), Is.EqualTo(10));
        }

        [Test]
        public void Sum_DoubleNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DoubleFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_DoubleNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DoubleFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_DoubleNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DoubleFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DoubleFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_DoubleNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DoubleFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_Float()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.FloatField), Is.EqualTo(55));
        }

        [Test]
        public void Sum_Float_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.FloatField, r => r.FloatField < 5), Is.EqualTo(10));
        }

        [Test]
        public void Sum_Float_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.FloatField < 5).Sum(r => r.FloatField), Is.EqualTo(10));
        }

        [Test]
        public void Sum_FloatNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.FloatFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_FloatNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.FloatFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_FloatNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.FloatFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.FloatFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_FloatNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.FloatFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_Decimal()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DecimalField), Is.EqualTo(55));
        }

        [Test]
        public void Sum_Decimal_Filtered()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DecimalField, r => r.DecimalField < 5), Is.EqualTo(10));
        }

        [Test]
        public void Sum_Decimal_Filtered_Where()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalField = i);

            Assert.That(DB.RecordsWithAllTypes.Where(r => r.DecimalField < 5).Sum(r => r.DecimalField), Is.EqualTo(10));
        }

        [Test]
        public void Sum_DecimalNullable()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DecimalFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_DecimalNullable_AllNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DecimalFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_DecimalNullable_SomeNull()
        {
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = null);
            InsertRecords<RecordWithAllTypes>(10, (r, i) => r.DecimalFieldNullable = i);

            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DecimalFieldNullable), Is.EqualTo(55));
        }

        [Test]
        public void Sum_DecimalNullable_NoRecords()
        {
            Assert.That(DB.RecordsWithAllTypes.Sum(r => r.DecimalFieldNullable), Is.Zero);
        }

        [Test]
        public void Sum_FieldInRelation()
        {
            var parent = InsertRecord(new RecordWithChildren() {Value = 15});

            InsertRecords<RecordWithParent>(10, (rec, i) => { rec.ParentKey = parent.Key; });

            var sum = DB.RecordsWithParent.WithRelations(r => r.Parent).Sum(r => r.Parent.Value);

            Assert.That(sum, Is.EqualTo(10 * 15));
        }

        [Test]
        public void Sum_FieldInRelation_SomeNull()
        {
            var parent = InsertRecord(new RecordWithChildren() { Value = 15 });

            InsertRecords<RecordWithParent>(10, (rec, i) => { rec.ParentKey = (i%2 == 0) ? parent.Key : (int?)null; });

            var sum = DB.RecordsWithParent.WithRelations(r => r.Parent).Sum(r => r.Parent.Value);

            Assert.That(sum, Is.EqualTo(5 * 15));
        }

        [Test]
        public void Sum_FieldInRelation_Filtered()
        {
            var parent = InsertRecord(new RecordWithChildren() { Value = 15 });

            InsertRecords<RecordWithParent>(10, (rec, i) => { rec.Value = i; rec.ParentKey = parent.Key; });

            var sum = DB.RecordsWithParent.WithRelations(r => r.Parent).Sum(r => r.Parent.Value, r => r.Value < 5);

            Assert.That(sum, Is.EqualTo(4 * 15));
        }


    }
}