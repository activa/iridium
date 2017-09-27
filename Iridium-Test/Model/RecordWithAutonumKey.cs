namespace Iridium.DB.Test
{
    public class RecordWithAutonumKey
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int Key;

        public string Name;
    }
}