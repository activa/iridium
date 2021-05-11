namespace Iridium.DB.Test
{
    public interface IRecordWithParent
    {
        RecordWithChildren Parent { get; set; }
        int? ParentKey { get; set; }
        string Name { get; set; }
        int? Value { get; set; }
    }

    public class RecordWithParent : IRecordWithParent
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int Key { get; set; }

        [Relation(LocalKey = nameof(ParentKey))]
        public RecordWithChildren Parent { get; set; }

        public int? ParentKey { get; set; }

        public string Name { get; set; }
        public int? Value { get; set; }
    }

    public class RecordWithPreloadParent
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int Key;

        [Relation(LocalKey = nameof(ParentKey))] [Relation.Preload]
        public RecordWithChildren Parent;

        public int? ParentKey;

        public string Name;
        public int? Value;
    }
}