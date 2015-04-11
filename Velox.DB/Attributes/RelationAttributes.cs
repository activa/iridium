using System;

namespace Velox.DB
{
    public sealed class Relation
    {
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public abstract class RelationAttribute : Attribute
        {
            public string LocalKey { get; set; }
            public string ForeignKey { get; set; }
            public bool ReadOnly { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class OneToManyAttribute : RelationAttribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class ManyToOneAttribute : RelationAttribute
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public sealed class IgnoreAttribute : Attribute
        {
        }
    }

    /*
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ManyToManyAttribute : RelationAttribute
    {
        public bool Pure { get; set; }
        public string LocalLinkKey { get; set; }
        public string ForeignLinkKey { get; set; }
        public string LinkTable { get; set; }

        public ManyToManyAttribute(string linkTable)
        {
            LinkTable = linkTable;
        }
    }
    */
}