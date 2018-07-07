namespace Iridium.DB.Test
{
    public interface IRecordWithInterface
    {
        string Name { get; set; }
    }

    public class RecordWithInterface : IRecordWithInterface
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int RecordID;

        public string Name { get; set; }
        public int? Value { get; set; }
    }
}