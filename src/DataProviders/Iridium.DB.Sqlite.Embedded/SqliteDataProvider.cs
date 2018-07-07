namespace Iridium.DB
{
    public class SqliteDataProvider : SqliteDataProviderCommon
    {
        public SqliteDataProvider() : base(null)
        {
        }

        public SqliteDataProvider(string fileName = null, SqliteDateFormat dateFormat = SqliteDateFormat.String) : base(null, fileName, dateFormat)
        {
        }
    }
}