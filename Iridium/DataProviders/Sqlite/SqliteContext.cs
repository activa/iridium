namespace Iridium.DB
{
    public class SqliteContext : DbContext
    {
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