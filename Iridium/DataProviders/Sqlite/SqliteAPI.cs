using System;

namespace Iridium.DB
{
    internal class SqliteAPI : ISqliteAPI
    {
        public void open_v2(string fileName, out IntPtr db, SqliteOpenFlags openFlags)
        {
            sqlite3.open_v2(fileName, out db, (int) openFlags, IntPtr.Zero);
        }

        public SqliteReturnCode close(IntPtr db)
        {
            return (SqliteReturnCode) sqlite3.close(db);
        }

        public SqliteReturnCode step(IntPtr stmt)
        {
            return (SqliteReturnCode) sqlite3.step(stmt);
        }

        public string errmsg(IntPtr dbHandle)
        {
            return sqlite3.errmsg(dbHandle);
        }

        public int changes(IntPtr dbHandle)
        {
            return sqlite3.changes(dbHandle);
        }

        public long last_insert_rowid(IntPtr dbHandle)
        {
            return sqlite3.last_insert_rowid(dbHandle);
        }

        public void finalize(IntPtr stmt)
        {
            sqlite3.finalize(stmt);
        }

        public SqliteReturnCode prepare_v2(IntPtr dbHandle, string sql, out IntPtr stmt)
        {
            return (SqliteReturnCode) sqlite3.prepare_v2(dbHandle, sql, -1, out stmt, IntPtr.Zero);
        }

        public int bind_parameter_index(IntPtr stmt, string paramName)
        {
            return sqlite3.bind_parameter_index(stmt, paramName);
        }

        public void bind_int64(IntPtr stmt, int paramNumber, long value)
        {
            sqlite3.bind_int64(stmt, paramNumber, value);
        }

        public void bind_int(IntPtr stmt, int paramNumber, int value)
        {
            sqlite3.bind_int(stmt, paramNumber, value);
        }

        public void bind_null(IntPtr stmt, int paramNumber)
        {
            sqlite3.bind_null(stmt, paramNumber);
        }

        public void bind_double(IntPtr stmt, int paramNumber, double value)
        {
            sqlite3.bind_double(stmt, paramNumber, value);
        }

        public void bind_text(IntPtr stmt, int paramNumber, string value)
        {
            sqlite3.bind_text16(stmt, paramNumber, value, -1, new IntPtr(-1));
        }

        public void bind_blob(IntPtr stmt, int paramNumber, byte[] value)
        {
            sqlite3.bind_blob(stmt, paramNumber, value, value.Length, new IntPtr(-1));
        }

        public int column_count(IntPtr stmt)
        {
            return sqlite3.column_count(stmt);
        }

        public string column_name(IntPtr stmt, int i)
        {
            return sqlite3.column_name(stmt, i);
        }

        public long column_int64(IntPtr stmt, int i)
        {
            return sqlite3.column_int64(stmt, i);
        }

        public double column_double(IntPtr stmt, int i)
        {
            return sqlite3.column_double(stmt, i);
        }

        public string column_text(IntPtr stmt, int i)
        {
            return sqlite3.column_text(stmt, i);
        }

        public byte[] column_blob(IntPtr stmt, int i)
        {
            return sqlite3.column_blob(stmt, i);
        }

        public SqliteColumnType column_type(IntPtr stmt, int i)
        {
            return (SqliteColumnType) sqlite3.column_type(stmt, i);
        }
    }

    public enum SqliteColumnType
    {
        Integer = 1,
        Float = 2,
        Text = 3,
        Blob = 4,
        Null = 5
    }

    public enum SqliteConfigOption
    {
        SingleThread = 1,
        MultiThread = 2,
        Serialized = 3
    }

    public enum SqliteReturnCode
    {
        Ok = 0,
        Error = 1,
        Internal = 2,
        Perm = 3,
        Abort = 4,
        Busy = 5,
        Locked = 6,
        NoMem = 7,
        ReadOnly = 8,
        Interrupt = 9,
        IOError = 10,
        Corrupt = 11,
        NotFound = 12,
        Full = 13,
        CantOpen = 14,
        ProtocolLock = 15,
        Empty = 16,
        SchemaChanged = 17,
        TooBig = 18,
        Constraint = 19,
        Mismatch = 20,
        Misuse = 21,
        NotSupportedOnHost = 22,
        Authorization = 23,
        Format = 24,
        Range = 25,
        NoDatabasse = 26,
        Notice = 27,
        Warning = 28,
        Row = 100,
        Done = 101
    }


    [Flags]
    internal enum SqliteOpenFlags
    {
        ReadOnly     = 0x00000001, 
        ReadWrite    = 0x00000002, 
        Create       = 0x00000004,
        Uri          = 0x00000040,
        Memory       = 0x00000080,
        NoMutex      = 0x00008000, 
        FullMutex    = 0x00010000,
        SharedCache  = 0x00020000, 
        PrivateCache = 0x00040000
    }
}