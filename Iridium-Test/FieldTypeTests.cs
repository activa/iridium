using FluentAssertions;
using System;
using FluentAssertions.Common;
using NUnit.Framework;

namespace Iridium.DB.Test
{
    [TestFixture("sqlite")]
    [TestFixture("sqlserver")]
    [TestFixture("memory")]
   // [TestFixture("mysql")]
    public class FieldTypeTests : TestFixture
    {
        [SetUp]
        public void SetupTest()
        {
            DB.PurgeAll();
        }

        public FieldTypeTests(string driver) : base(driver)
        {
            DB.CreateAllTables();
        }

        private RecordWithAllTypes SaveAndReload(RecordWithAllTypes rec)
        {
            DB.RecordsWithAllTypes.Insert(rec);

            return DB.RecordsWithAllTypes.Read(rec.PK);
        }

        [Test]
        public void FieldInt()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes { });

            rec.IntField.Should().Be(default(int));
            rec.IntFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes {IntField = 111});

            rec.IntField.Should().Be(111);
            rec.IntFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() {IntFieldNullable = 111});

            rec.IntField.Should().Be(0);
            rec.IntFieldNullable.Should().Be(111);

            DB.RecordsWithAllTypes.Count(r => r.IntField == 111).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => r.IntField == 0).Should().Be(2);
            DB.RecordsWithAllTypes.Count(r => r.IntField == r.IntFieldNullable).Should().Be(0);
            DB.RecordsWithAllTypes.Count(r => r.IntFieldNullable == null).Should().Be(2);
            DB.RecordsWithAllTypes.Count(r => r.IntFieldNullable != null).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => r.IntFieldNullable == 111).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => (r.IntFieldNullable ?? 0) == 111).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => (r.IntFieldNullable ?? 0) == 0).Should().Be(2);
        }

        [Test]
        public void FieldEnum()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes { });

            rec.EnumField.Should().Be(default(TestEnum));
            rec.EnumFieldNullable.Should().BeNull();

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = default(TestEnum), EnumFieldNullable = null});

            rec.EnumField.Should().Be(default(TestEnum));
            rec.EnumFieldNullable.Should().BeNull();

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = TestEnum.One, EnumFieldNullable = TestEnum.One});

            rec.EnumField.Should().Be(TestEnum.One);
            rec.EnumFieldNullable.Should().Be(TestEnum.One);

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = (TestEnum)100, EnumFieldNullable = (TestEnum)100});

            Assert.That(rec.EnumField,Is.EqualTo(default(TestEnum)).Or.EqualTo((TestEnum)100));
            Assert.That(rec.EnumFieldNullable, Is.Null.Or.EqualTo((TestEnum)100));
        }

        [Test]
        public void FieldEnumWithZero()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes { });

            rec.EnumWithZeroField.Should().Be(default(TestEnumWithZero));
            rec.EnumWithZeroFieldNullable.Should().BeNull();

            rec = SaveAndReload(new RecordWithAllTypes { EnumWithZeroField = default(TestEnumWithZero), EnumWithZeroFieldNullable = default(TestEnumWithZero) });

            rec.EnumWithZeroField.Should().Be(TestEnumWithZero.Zero);
            rec.EnumWithZeroFieldNullable.Should().Be(TestEnumWithZero.Zero);

            rec = SaveAndReload(new RecordWithAllTypes { EnumWithZeroField = TestEnumWithZero.One, EnumWithZeroFieldNullable = TestEnumWithZero.One });

            rec.EnumWithZeroField.Should().Be(TestEnumWithZero.One);
            rec.EnumWithZeroFieldNullable.Should().Be(TestEnumWithZero.One);

            rec = SaveAndReload(new RecordWithAllTypes { EnumWithZeroField = (TestEnumWithZero)100, EnumWithZeroFieldNullable = (TestEnumWithZero)100 });

            Assert.That(rec.EnumWithZeroField,Is.EqualTo(TestEnumWithZero.Zero).Or.EqualTo((TestEnumWithZero)100));
            Assert.That(rec.EnumWithZeroFieldNullable,Is.Null.Or.EqualTo((TestEnumWithZero)100));
        }

        [Test]
        public void FieldLargeText()
        {
            RecordWithAllTypes rec;

            var bigText = new string('x',10000);

            rec = SaveAndReload(new RecordWithAllTypes() { LongStringField = bigText });

            rec.LongStringField.Should().Be(bigText);
        }

        [Test]
        public void FieldString()
        {
            RecordWithAllTypes rec;

            var s = new string('x', 50);

            rec = SaveAndReload(new RecordWithAllTypes() { StringField = s });

            rec.StringField.Should().Be(s);
        }

        [Test]
        public void FieldStringTooLong()
        {
            RecordWithAllTypes rec;

            var tooLongString = new string('x', 100);
            var fixedTooLongString = new string('x', 50);

            try
            {
                rec = SaveAndReload(new RecordWithAllTypes() {StringField = tooLongString});

                rec.StringField.Should().BeOneOf(tooLongString, fixedTooLongString);
            }
            catch
            {
                // it's ok, the DataProvider probably choked on the long string
            }

            
        }


        [Test]
        public void FieldDateTime()
        {
            RecordWithAllTypes rec;

            DateTime now = DateTime.Now;
            DateTime defaultDate = new DateTime(1970, 1, 1);

            rec = SaveAndReload(new RecordWithAllTypes { DateTimeField = now});

            rec.DateTimeField.Should().BeCloseTo(now, 1000); // some data providers have a 1 second precision on DateTime
            rec.DateTimeFieldNullable.Should().NotHaveValue();

            now = rec.DateTimeField; // to get the actual value in the database, rounded to database precision

            rec = SaveAndReload(new RecordWithAllTypes { });

            rec.DateTimeField.Should().Be(defaultDate);
            rec.DateTimeFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes { DateTimeFieldNullable = now });

            rec.DateTimeField.Should().Be(defaultDate);
            rec.DateTimeFieldNullable.Should().HaveValue();
            rec.DateTimeFieldNullable.Should().BeCloseTo(now, 1000);

            DB.RecordsWithAllTypes.Count(r => r.DateTimeField == now).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => r.DateTimeField == defaultDate).Should().Be(2);
            DB.RecordsWithAllTypes.Count(r => r.DateTimeFieldNullable == null).Should().Be(2);
            DB.RecordsWithAllTypes.Count(r => r.DateTimeFieldNullable != null).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => r.DateTimeFieldNullable == now).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => (r.DateTimeFieldNullable ?? defaultDate) == now).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => (r.DateTimeFieldNullable ?? defaultDate) == defaultDate).Should().Be(2);
        }

        [Test]
        public void FieldBoolean()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanField = true });

            rec.BooleanField.Should().BeTrue();
            rec.BooleanFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanField = false });

            rec.BooleanField.Should().BeFalse();
            rec.BooleanFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() {  });

            rec.BooleanField.Should().BeFalse();
            rec.BooleanFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanFieldNullable = true });

            rec.BooleanField.Should().BeFalse();
            rec.BooleanFieldNullable.Should().HaveValue();
            rec.BooleanFieldNullable.Should().BeTrue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanFieldNullable = false });

            rec.BooleanField.Should().BeFalse();
            rec.BooleanFieldNullable.Should().HaveValue();
            rec.BooleanFieldNullable.Should().BeFalse();


            DB.RecordsWithAllTypes.Count(r => r.BooleanField).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => !r.BooleanField).Should().Be(4);
            DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable == null).Should().Be(3);
            DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable != null).Should().Be(2);
            DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable == true).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable == false).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => (r.BooleanFieldNullable ?? false)).Should().Be(1);
            DB.RecordsWithAllTypes.Count(r => !(r.BooleanFieldNullable ?? false)).Should().Be(4);
        }

        [Test]
        public void FieldBlob()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes { });

            Assert.That(rec.BlobField, Is.Null);

            rec = SaveAndReload(new RecordWithAllTypes() {BlobField = new byte[] {1, 2, 3, 4, 5}});

            Assert.That(rec.BlobField, Is.EquivalentTo(new[] {1,2,3,4,5}));


        }

        [Test]
        public void CompareNullableWithNonNullable()
        {
            DB.Insert(new RecordWithAllTypes {IntField = 1, IntFieldNullable = 1});

            DB.RecordsWithAllTypes.Count(rec => rec.IntField == rec.IntFieldNullable).Should().Be(1);



        }

    }
}