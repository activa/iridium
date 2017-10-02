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
}