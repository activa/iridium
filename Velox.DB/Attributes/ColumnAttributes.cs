using System;

namespace Velox.DB
{
    public sealed class Column
    {
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class NotMappedAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class IndexedAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class NotNullAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class NullAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class PrimaryKeyAttribute : Attribute
        {
            public bool AutoIncrement { get; set; }
            public int Pos { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class SizeAttribute : Attribute
        {
            public int Size { get; set; }
            public int Scale { get; set; }

            public SizeAttribute(int size)
            {
                Size = size;
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class LargeTextAttribute : Attribute
        {
        }
    }


}