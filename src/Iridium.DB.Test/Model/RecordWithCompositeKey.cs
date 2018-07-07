namespace Iridium.DB.Test
{
    public class RecordWithCompositeKey
    {
        [Column.PrimaryKey] public int Key1;
        [Column.PrimaryKey] public int Key2;

        public string Name;
    }
}