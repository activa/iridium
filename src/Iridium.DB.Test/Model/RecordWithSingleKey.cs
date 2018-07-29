namespace Iridium.DB.Test
{
    public class RecordWithSingleKey
    {
        [Column.PrimaryKey(AutoIncrement = false)]
        public int Key;

        public string Name;
        public int? Value;
    }

}