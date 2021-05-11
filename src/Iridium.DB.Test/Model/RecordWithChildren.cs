namespace Iridium.DB.Test
{
    public interface IRecordWithChildren
    {
        IDataSet<RecordWithParent> Children { get; set; }

        string Name { get; set; }
        int? Value { get; set; }
    }

    public class RecordWithChildren : IRecordWithChildren
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int Key;

        [Relation(ForeignKey = nameof(RecordWithParent.ParentKey))]
        public IDataSet<RecordWithParent> Children { get; set; }

        public string Name { get; set; }
        public int? Value { get; set; }
    }
}