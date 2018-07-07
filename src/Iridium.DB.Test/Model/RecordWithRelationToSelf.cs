namespace Iridium.DB.Test
{
    public class RecordWithRelationToSelf
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int Key;

        [Relation(LocalKey=nameof(ParentKey))]
        public RecordWithRelationToSelf Parent;

        public int? ParentKey;

        public string Name;
        public int? Value;
    }

    public class RecordWithUniqueIndex
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int RecordID;

        [Column.Indexed(Unique = true)]
        public string Name;
    }
}