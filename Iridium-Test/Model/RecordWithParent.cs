namespace Iridium.DB.Test
{
    public class RecordWithParent
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int Key;

        [Relation(LocalKey = nameof(ParentKey))]
        public RecordWithChildren Parent;

        public int? ParentKey;

        public string Name;
        public int? Value;
    }
}