namespace Iridium.DB
{
    public class SqliteContext : StorageContext
    {
        public SqliteContext() : base(new SqliteDataProvider())
        {
        }

        public SqliteContext(string dbFileName) : base(new SqliteDataProvider(dbFileName))
        {
        }

        public string FileName
        {
            get => ((SqliteDataProvider) DataProvider).FileName;
            set => ((SqliteDataProvider) DataProvider).FileName = value;
        }
    }
}