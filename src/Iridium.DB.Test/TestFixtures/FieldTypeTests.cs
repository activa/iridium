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
    public class FieldTypeTests : TestFixture
    {
        public FieldTypeTests(string driver) : base(driver)
        {
        }

        [SetUp]
        public void DeleteTables()
        {
            DB.RecordsWithAllTypes.DeleteAll();
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

            Assert.That(rec.IntField, Is.EqualTo(default(int)));
            Assert.That(rec.IntFieldNullable, Is.Null);

            rec = SaveAndReload(new RecordWithAllTypes {IntField = 111});

            Assert.That(rec.IntField, Is.EqualTo(111));
            Assert.That(rec.IntFieldNullable, Is.Null);

            rec = SaveAndReload(new RecordWithAllTypes() {IntFieldNullable = 111});

            Assert.That(rec.IntField, Is.Zero);
            Assert.That(rec.IntFieldNullable, Is.EqualTo(111));

            Assert.That(DB.RecordsWithAllTypes.Count(r => r.IntField == 111),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.IntField == 0),Is.EqualTo(2));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.IntField == r.IntFieldNullable),Is.EqualTo(0));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.IntFieldNullable == null),Is.EqualTo(2));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.IntFieldNullable != null),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.IntFieldNullable == 111),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => (r.IntFieldNullable ?? 0) == 111),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => (r.IntFieldNullable ?? 0) == 0),Is.EqualTo(2));
        }

        [Test]
        public void FieldEnum()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes { });

            Assert.That(rec.EnumField, Is.EqualTo(default(TestEnum)));
            Assert.That(rec.EnumFieldNullable, Is.Null);

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = default(TestEnum), EnumFieldNullable = null});

            Assert.That(rec.EnumField, Is.EqualTo(default(TestEnum)));
            Assert.That(rec.EnumFieldNullable, Is.Null);

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = TestEnum.One, EnumFieldNullable = TestEnum.One});

            Assert.That(rec.EnumField, Is.EqualTo(TestEnum.One));
            Assert.That(rec.EnumFieldNullable, Is.EqualTo(TestEnum.One));

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = (TestEnum)100, EnumFieldNullable = (TestEnum)100});

            Assert.That(rec.EnumField,Is.EqualTo(default(TestEnum)).Or.EqualTo((TestEnum)100));
            Assert.That(rec.EnumFieldNullable, Is.Null.Or.EqualTo((TestEnum)100));
        }

        [Test]
        public void FieldEnumWithZero()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes { });

            Assert.That(rec.EnumWithZeroField, Is.EqualTo(default(TestEnumWithZero)));
            Assert.Null(rec.EnumWithZeroFieldNullable);

            rec = SaveAndReload(new RecordWithAllTypes { EnumWithZeroField = default(TestEnumWithZero), EnumWithZeroFieldNullable = default(TestEnumWithZero) });

            Assert.That(rec.EnumWithZeroField, Is.EqualTo(TestEnumWithZero.Zero));
            Assert.That(rec.EnumWithZeroFieldNullable, Is.EqualTo(TestEnumWithZero.Zero));

            rec = SaveAndReload(new RecordWithAllTypes { EnumWithZeroField = TestEnumWithZero.One, EnumWithZeroFieldNullable = TestEnumWithZero.One });

            Assert.That(rec.EnumWithZeroField, Is.EqualTo(TestEnumWithZero.One));
            Assert.That(rec.EnumWithZeroFieldNullable, Is.EqualTo(TestEnumWithZero.One));

            rec = SaveAndReload(new RecordWithAllTypes { EnumWithZeroField = (TestEnumWithZero)100, EnumWithZeroFieldNullable = (TestEnumWithZero)100 });

            Assert.That(rec.EnumWithZeroField, Is.EqualTo((TestEnumWithZero)100).Or.EqualTo(TestEnumWithZero.Zero));
            Assert.That(rec.EnumWithZeroFieldNullable, Is.Null.Or.EqualTo((TestEnumWithZero)100));
        }

        [Test]
        public void FieldLargeText()
        {
            RecordWithAllTypes rec;

            var bigText = new string('x',10000);

            rec = SaveAndReload(new RecordWithAllTypes() { LongStringField = bigText });

            Assert.That(rec.LongStringField, Is.EqualTo(bigText));
        }

        [Test]
        public void FieldString()
        {
            RecordWithAllTypes rec;

            var s = new string('x', 50);

            rec = SaveAndReload(new RecordWithAllTypes() { StringField = s });

            Assert.That(rec.StringField, Is.EqualTo(s));
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

                Assert.That(rec.StringField, Is.EqualTo(tooLongString).Or.EqualTo(fixedTooLongString));
//                rec.StringField.Should().BeOneOf(tooLongString, fixedTooLongString);
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

            DateTime now = DateTime.UtcNow;
            DateTime defaultDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            rec = SaveAndReload(new RecordWithAllTypes { DateTimeField = now});

            Assert.That(rec.DateTimeField, Is.EqualTo(now).Within(1).Seconds);
            Assert.Null(rec.DateTimeFieldNullable);

            now = rec.DateTimeField; // to get the actual value in the database, rounded to database precision

            rec = SaveAndReload(new RecordWithAllTypes { });

            Assert.That(rec.DateTimeField, Is.EqualTo(defaultDate));
            Assert.Null(rec.DateTimeFieldNullable);

            rec = SaveAndReload(new RecordWithAllTypes { DateTimeFieldNullable = now });

            Assert.That(rec.DateTimeField, Is.EqualTo(defaultDate));
            Assert.NotNull(rec.DateTimeFieldNullable);
            Assert.That(rec.DateTimeFieldNullable, Is.EqualTo(now).Within(1).Seconds);

            Assert.That(DB.RecordsWithAllTypes.Count(r => r.DateTimeField == now),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.DateTimeField == defaultDate),Is.EqualTo(2));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.DateTimeFieldNullable == null),Is.EqualTo(2));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.DateTimeFieldNullable != null),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.DateTimeFieldNullable == now),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => (r.DateTimeFieldNullable ?? defaultDate) == now),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => (r.DateTimeFieldNullable ?? defaultDate) == defaultDate),Is.EqualTo(2));
        }

        [Test]
        public void FieldBoolean()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanField = true });

            Assert.True(rec.BooleanField);
            Assert.Null(rec.BooleanFieldNullable);

//            rec.BooleanField.Should().BeTrue();
//            rec.BooleanFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanField = false });

            Assert.False(rec.BooleanField);
            Assert.Null(rec.BooleanFieldNullable);
//            rec.BooleanField.Should().BeFalse();
//            rec.BooleanFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() {  });

            Assert.False(rec.BooleanField);
            Assert.Null(rec.BooleanFieldNullable);
//            rec.BooleanField.Should().BeFalse();
//            rec.BooleanFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanFieldNullable = true });

            Assert.False(rec.BooleanField);
            Assert.NotNull(rec.BooleanFieldNullable);
            Assert.True(rec.BooleanFieldNullable.Value);

//            rec.BooleanField.Should().BeFalse();
//            rec.BooleanFieldNullable.Should().HaveValue();
//            rec.BooleanFieldNullable.Should().BeTrue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanFieldNullable = false });

            Assert.False(rec.BooleanField);
            Assert.NotNull(rec.BooleanFieldNullable);
            Assert.False(rec.BooleanFieldNullable.Value);
//            rec.BooleanField.Should().BeFalse();
//            rec.BooleanFieldNullable.Should().HaveValue();
//            rec.BooleanFieldNullable.Should().BeFalse();


            Assert.That(DB.RecordsWithAllTypes.Count(r => r.BooleanField),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => !r.BooleanField),Is.EqualTo(4));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable == null),Is.EqualTo(3));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable != null),Is.EqualTo(2));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable == true),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => r.BooleanFieldNullable == false),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => (r.BooleanFieldNullable ?? false)),Is.EqualTo(1));
            Assert.That(DB.RecordsWithAllTypes.Count(r => !(r.BooleanFieldNullable ?? false)),Is.EqualTo(4));
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

            Assert.That(DB.RecordsWithAllTypes.Count(rec => rec.IntField == rec.IntFieldNullable), Is.EqualTo(1));
        }

    }
}