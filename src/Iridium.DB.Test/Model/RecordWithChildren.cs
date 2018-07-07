namespace Iridium.DB.Test
{
    public class RecordWithChildren
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int Key;

        [Relation(ForeignKey = nameof(RecordWithParent.ParentKey))]
        public IDataSet<RecordWithParent> Children;

        public string Name;
        public int? Value;
    }
}