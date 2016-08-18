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

        public SqliteExtendedErrorCode extended_errcode(IntPtr dbHandle)
        {
            return (SqliteExtendedErrorCode) sqlite3.extended_errcode(dbHandle);
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

    public enum SqliteExtendedErrorCode
    {
        IOERR_READ = (SqliteReturnCode.IOError | (1 << 8)),
        IOERR_SHORT_READ = (SqliteReturnCode.IOError | (2 << 8)),
        IOERR_WRITE = (SqliteReturnCode.IOError | (3 << 8)),
        IOERR_FSYNC = (SqliteReturnCode.IOError | (4 << 8)),
        IOERR_DIR_FSYNC = (SqliteReturnCode.IOError | (5 << 8)),
        IOERR_TRUNCATE = (SqliteReturnCode.IOError | (6 << 8)),
        IOERR_FSTAT = (SqliteReturnCode.IOError | (7 << 8)),
        IOERR_UNLOCK = (SqliteReturnCode.IOError | (8 << 8)),
        IOERR_RDLOCK = (SqliteReturnCode.IOError | (9 << 8)),
        IOERR_DELETE = (SqliteReturnCode.IOError | (10 << 8)),
        IOERR_BLOCKED = (SqliteReturnCode.IOError | (11 << 8)),
        IOERR_NOMEM = (SqliteReturnCode.IOError | (12 << 8)),
        IOERR_ACCESS = (SqliteReturnCode.IOError | (13 << 8)),
        IOERR_CHECKRESERVEDLOCK = (SqliteReturnCode.IOError | (14 << 8)),
        IOERR_LOCK = (SqliteReturnCode.IOError | (15 << 8)),
        IOERR_CLOSE = (SqliteReturnCode.IOError | (16 << 8)),
        IOERR_DIR_CLOSE = (SqliteReturnCode.IOError | (17 << 8)),
        IOERR_SHMOPEN = (SqliteReturnCode.IOError | (18 << 8)),
        IOERR_SHMSIZE = (SqliteReturnCode.IOError | (19 << 8)),
        IOERR_SHMLOCK = (SqliteReturnCode.IOError | (20 << 8)),
        IOERR_SHMMAP = (SqliteReturnCode.IOError | (21 << 8)),
        IOERR_SEEK = (SqliteReturnCode.IOError | (22 << 8)),
        IOERR_DELETE_NOENT = (SqliteReturnCode.IOError | (23 << 8)),
        IOERR_MMAP = (SqliteReturnCode.IOError | (24 << 8)),
        IOERR_GETTEMPPATH = (SqliteReturnCode.IOError | (25 << 8)),
        IOERR_CONVPATH = (SqliteReturnCode.IOError | (26 << 8)),
        IOERR_VNODE = (SqliteReturnCode.IOError | (27 << 8)),
        IOERR_AUTH = (SqliteReturnCode.IOError | (28 << 8)),
        LOCKED_SHAREDCACHE = (SqliteReturnCode.Locked | (1 << 8)),
        BUSY_RECOVERY = (SqliteReturnCode.Busy | (1 << 8)),
        BUSY_SNAPSHOT = (SqliteReturnCode.Busy | (2 << 8)),
        CANTOPEN_NOTEMPDIR = (SqliteReturnCode.CantOpen | (1 << 8)),
        CANTOPEN_ISDIR = (SqliteReturnCode.CantOpen | (2 << 8)),
        CANTOPEN_FULLPATH = (SqliteReturnCode.CantOpen | (3 << 8)),
        CANTOPEN_CONVPATH = (SqliteReturnCode.CantOpen | (4 << 8)),
        CORRUPT_VTAB = (SqliteReturnCode.Corrupt | (1 << 8)),
        READONLY_RECOVERY = (SqliteReturnCode.ReadOnly | (1 << 8)),
        READONLY_CANTLOCK = (SqliteReturnCode.ReadOnly | (2 << 8)),
        READONLY_ROLLBACK = (SqliteReturnCode.ReadOnly | (3 << 8)),
        READONLY_DBMOVED = (SqliteReturnCode.ReadOnly | (4 << 8)),
        ABORT_ROLLBACK = (SqliteReturnCode.Abort | (2 << 8)),
        CONSTRAINT_CHECK = (SqliteReturnCode.Constraint | (1 << 8)),
        CONSTRAINT_COMMITHOOK = (SqliteReturnCode.Constraint | (2 << 8)),
        CONSTRAINT_FOREIGNKEY = (SqliteReturnCode.Constraint | (3 << 8)),
        CONSTRAINT_FUNCTION = (SqliteReturnCode.Constraint | (4 << 8)),
        CONSTRAINT_NOTNULL = (SqliteReturnCode.Constraint | (5 << 8)),
        CONSTRAINT_PRIMARYKEY = (SqliteReturnCode.Constraint | (6 << 8)),
        CONSTRAINT_TRIGGER = (SqliteReturnCode.Constraint | (7 << 8)),
        CONSTRAINT_UNIQUE = (SqliteReturnCode.Constraint | (8 << 8)),
        CONSTRAINT_VTAB = (SqliteReturnCode.Constraint | (9 << 8)),
        CONSTRAINT_ROWID = (SqliteReturnCode.Constraint | (10 << 8)),
        NOTICE_RECOVER_WAL = (SqliteReturnCode.Notice | (1 << 8)),
        NOTICE_RECOVER_ROLLBACK = (SqliteReturnCode.Notice | (2 << 8)),
        WARNING_AUTOINDEX = (SqliteReturnCode.Warning | (1 << 8)),
        AUTH_USER = (SqliteReturnCode.Authorization | (1 << 8)),
        OK_LOAD_PERMANENTLY = (SqliteReturnCode.Ok | (1 << 8)),
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