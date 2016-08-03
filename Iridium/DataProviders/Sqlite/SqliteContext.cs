namespace Iridium.DB
{
    public class SqliteContext : Vx.Context
    {
        public static void Use(string dbFileName)
        {
            Vx.DB = new SqliteContext(dbFileName);
        }

        public SqliteContext() : base(new SqliteDataProvider())
        {
        }

        public SqliteContext(string dbFileName) : base(new SqliteDataProvider(dbFileName))
        {
        }

        public string FileName
        {
            get { return ((SqliteDataProvider) DataProvider).FileName; }
            set { ((SqliteDataProvider) DataProvider).FileName = value; }
        }
    }
}