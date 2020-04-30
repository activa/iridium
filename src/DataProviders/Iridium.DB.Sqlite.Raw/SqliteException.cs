using System;

namespace Iridium.DB
{
    public class SqliteException : Exception
    {
        public SqliteReturnCode ReturnCode { get; }
        public SqliteExtendedErrorCode ExtendedErrorCode { get; }

        public SqliteException(SqliteReturnCode returnCode, SqliteExtendedErrorCode extendedErrorCode, string message) : base(message)
        {
            ReturnCode = returnCode;
            ExtendedErrorCode = extendedErrorCode;
        }
    }
}