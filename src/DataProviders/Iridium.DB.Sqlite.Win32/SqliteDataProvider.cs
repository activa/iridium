using Iridium.DB.Sqlite;

namespace Iridium.DB
{
    public class SqliteDataProvider : SqliteDataProviderCommon
    {
        public SqliteDataProvider() : base(new NativeLibraryLoader())
        {
        }

        public SqliteDataProvider(string fileName = null, SqliteDateFormat dateFormat = SqliteDateFormat.String) : base(new NativeLibraryLoader(), fileName, dateFormat)
        {
        }
    }
}