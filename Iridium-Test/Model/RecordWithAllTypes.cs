using System;

namespace Iridium.DB.Test
{
    public class RecordWithAllTypes
    {
        [Column.PrimaryKeyAttribute(AutoIncrement = true)]
        public int PK;

        public bool BooleanField;
        public bool? BooleanFieldNullable;
        public byte ByteField;
        public byte? ByteFieldNullable;
        public short ShortField;
        public short? ShortFieldNullable;
        public int IntField;
        public int? IntFieldNullable;
        public long LongField;
        public long? LongFieldNullable;
        public decimal DecimalField;
        public decimal? DecimalFieldNullable;
        public float FloatField;
        public float? FloatFieldNullable;
        public double DoubleField;
        public double DoubleFieldNullable;
        public string StringField;
        [Column.LargeText]
        public string LongStringField;

        public byte[] BlobField;

        public DateTime DateTimeField = new DateTime(1970,1,1);
        public DateTime? DateTimeFieldNullable;

        public TestEnum EnumField;
        public TestEnum? EnumFieldNullable;

        public TestEnumWithZero EnumWithZeroField;
        public TestEnumWithZero? EnumWithZeroFieldNullable;

        public TestFlagsEnum FlagsEnumField;
        public TestFlagsEnum? FlagsEnumFieldNullable;
    }
}