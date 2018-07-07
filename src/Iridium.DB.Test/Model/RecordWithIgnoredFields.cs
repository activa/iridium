namespace Iridium.DB.Test
{
    public class RecordWithIgnoredFields
    {
        [Column.PrimaryKey(AutoIncrement = true)]
        public int RecordID;

        public string FirstName;
        public string LastName;

        [Column.Ignore]
        public string FullName
        {
            get { return FirstName + " " + LastName; }
            set { FirstName = value; }
        }
    }
}