using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Velox.DB.TextExpressions;

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

            var tooLongString = new string('x', 100);
            var fixedTooLongString = new string('x', 50);

            rec = SaveAndReload(new RecordWithAllTypes() { StringField = tooLongString });

            rec.StringField.Should().BeOneOf(tooLongString, fixedTooLongString);
        }


        [Test]
        public void FieldDateTime()
        {
            RecordWithAllTypes rec;

            DateTime now = DateTime.Now;

            rec = SaveAndReload(new RecordWithAllTypes { DateTimeField = now});

            rec.DateTimeField.Should().Be(now);
            rec.DateTimeFieldNullable.Should().NotHaveValue();

        }


    }
}