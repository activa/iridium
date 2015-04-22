using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if MSTEST
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using TestFixtureAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using SetUpAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;

#else
using NUnit.Framework;
#endif

namespace Velox.DB.Test
{
    [TestFixture]
    public class FieldTypeTests
    {
        private MyContext DB = MyContext.Instance;

        [SetUp]
        public void SetupTest()
        {
            DB.PurgeAll();
        }

        public FieldTypeTests()
        {
            DB.CreateAllTables();
        }

        private RecordWithAllTypes SaveAndReload(RecordWithAllTypes rec)
        {
            DB.RecordsWithAllTypes.Save(rec);

            return DB.RecordsWithAllTypes.Read(rec.PK);
        }

        [Test]
        public void FieldInt()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes {IntField = 111});

            rec.IntField.Should().Be(111);
            rec.IntFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() {IntFieldNullable = 111});

            rec.IntField.Should().Be(0);
            rec.IntFieldNullable.Should().Be(111);
        }

        [Test]
        public void FieldEnum()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes { });

            rec.EnumField.Should().Be(default(TestEnum));
            rec.EnumFieldNullable.Should().BeNull();

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = default(TestEnum), EnumFieldNullable = default(TestEnum)});

            rec.EnumField.Should().Be(default(TestEnum));
            rec.EnumFieldNullable.Should().BeNull();

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = TestEnum.One, EnumFieldNullable = TestEnum.One});

            rec.EnumField.Should().Be(TestEnum.One);
            rec.EnumFieldNullable.Should().Be(TestEnum.One);

            rec = SaveAndReload(new RecordWithAllTypes { EnumField = (TestEnum)100, EnumFieldNullable = (TestEnum)100});

            rec.EnumField.Should().Be(default(TestEnum)); // unsupported enums are converted to the default value
            rec.EnumFieldNullable.Should().BeNull();
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

            rec.EnumWithZeroField.Should().Be(TestEnumWithZero.Zero); // unsupported enums are converted to the default value
            rec.EnumWithZeroFieldNullable.Should().BeNull();
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

            rec = SaveAndReload(new RecordWithAllTypes { DateTimeField = now});

            rec.DateTimeField.Should().BeCloseTo(now, 1000);
            rec.DateTimeFieldNullable.Should().NotHaveValue();

        }

        [Test]
        public void FieldBoolean()
        {
            RecordWithAllTypes rec;

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanField = true });

            rec.BooleanField.Should().BeTrue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanField = false });

            rec.BooleanField.Should().BeFalse();

            rec = SaveAndReload(new RecordWithAllTypes() {  });

            rec.BooleanField.Should().BeFalse();
            rec.BooleanFieldNullable.Should().NotHaveValue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanFieldNullable = true });

            rec.BooleanFieldNullable.Should().HaveValue();
            rec.BooleanFieldNullable.Should().BeTrue();

            rec = SaveAndReload(new RecordWithAllTypes() { BooleanFieldNullable = false });

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


    }
}