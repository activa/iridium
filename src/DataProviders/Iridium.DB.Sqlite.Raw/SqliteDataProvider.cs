#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Iridium.Reflection;

namespace Iridium.DB
{
    public class SqliteDataProvider : SqlDataProvider<SqliteDialect>
    {
        private SQLitePCL.sqlite3 _db;

        private readonly ThreadLocal<long?> _lastRowId = new ThreadLocal<long?>();

        static SqliteDataProvider()
        {
            SQLitePCL.Batteries_V2.Init();
        }

        public SqliteDataProvider()
        {
        }

        public SqliteDataProvider(string fileName)
        {
            FileName = fileName;
        }

        public SqliteDataProvider(string fileName, SqliteDateFormat dateFormat)
        {
            FileName = fileName;
            DateFormat = dateFormat;
        }

        internal SQLitePCL.sqlite3 DbHandle
        {
            get
            {
                lock (this)
                {
                    if (_db != null)
                        return _db;

                    SQLitePCL.raw.sqlite3_open_v2(FileName, out var db, SQLitePCL.raw.SQLITE_OPEN_READWRITE | SQLitePCL.raw.SQLITE_OPEN_CREATE | SQLitePCL.raw.SQLITE_OPEN_FULLMUTEX, null);

                    _db = db;

                    _transactionStack.Clear();

                    ExecuteSql("pragma journal_mode = TRUNCATE", null);
                }

                return _db;
            }
        }

        public string FileName { get; set; }
        public SqliteDateFormat DateFormat { get; set; } = SqliteDateFormat.String;

        public override int ExecuteSql(string sql, QueryParameterCollection parameters = null)
        {
            lock (this) // although sqlite3 is thread-safe, we need to make sure that the last_insert_rowid is correct
            {
                var stopwatch = SqlLogger != null ? Stopwatch.StartNew() : null;

                var stmt = CreateCommand(sql, parameters);

                try
                {
                    int returnCode = SQLitePCL.raw.sqlite3_step(stmt);

                    if (returnCode == SQLitePCL.raw.SQLITE_ROW)
                        return 0; // quietly eat any rows being returned

                    if (returnCode != SQLitePCL.raw.SQLITE_DONE)
                        throw new SqliteException((SqliteReturnCode) returnCode, (SqliteExtendedErrorCode) SQLitePCL.raw.sqlite3_extended_errcode(DbHandle), SQLitePCL.raw.sqlite3_errmsg(DbHandle).utf8_to_string());

                    _lastRowId.Value = SQLitePCL.raw.sqlite3_last_insert_rowid(DbHandle);

                    return SQLitePCL.raw.sqlite3_changes(DbHandle);
                }
                finally
                {
                    SQLitePCL.raw.sqlite3_finalize(stmt);

                    SqlLogger?.LogSql(sql, parameters?.ToDictionary(p => SqlDialect.CreateParameterExpression(p.Name), p => p.Value), stopwatch?.Elapsed ?? TimeSpan.Zero);
                }
            }
        }

        public override int ExecuteProcedure(string procName, QueryParameterCollection parameters = null)
        {
            throw new NotSupportedException("Stored procedures are not supported in Sqlite");
        }

        private readonly Stack<string> _transactionStack = new Stack<string>();

        public override void BeginTransaction(IsolationLevel isolationLevel)
        {
            lock (this)
            {
                if (isolationLevel == IsolationLevel.None)
                    _transactionStack.Push(null);
                else
                {
                    if (_transactionStack.Any(s => s != null))
                    {
                        string savePoint = "SP" + _transactionStack.Count;

                        ExecuteSql("SAVEPOINT " + savePoint);

                        _transactionStack.Push(savePoint);
                    }
                    else
                    {
                        ExecuteSql("BEGIN TRANSACTION", null);

                        _transactionStack.Push(string.Empty);
                    }
                }
            }
        }

        public override void CommitTransaction()
        {
            lock (this)
            {
                string name = _transactionStack.Pop();

                if (name == null)
                    return;
                else if (name == "")
                    ExecuteSql("COMMIT");
                else
                    ExecuteSql("RELEASE " + name);
            }
        }

        public override void RollbackTransaction()
        {
            lock (this)
            {
                string name = _transactionStack.Pop();

                if (name == null)
                    return;
                else if (name == "")
                    ExecuteSql("ROLLBACK");
                else
                    ExecuteSql("ROLLBACK TO " + name);
            }
        }


        private SQLitePCL.sqlite3_stmt CreateCommand(string sql, QueryParameterCollection parameters)
        {
            var returnCode = SQLitePCL.raw.sqlite3_prepare_v2(DbHandle, sql, out SQLitePCL.sqlite3_stmt stmt);

            if (returnCode != SQLitePCL.raw.SQLITE_OK)
            {
                throw new SqliteException((SqliteReturnCode) returnCode, (SqliteExtendedErrorCode) SQLitePCL.raw.sqlite3_extended_errcode(DbHandle), SQLitePCL.raw.sqlite3_errmsg(DbHandle).utf8_to_string());
            }

            if (parameters != null)
                foreach (var parameter in parameters)
                {
                    int paramNumber = SQLitePCL.raw.sqlite3_bind_parameter_index(stmt, SqlDialect.CreateParameterExpression(parameter.Name));

                    var value = parameter.Value;

                    if (value == null)
                    {
                        SQLitePCL.raw.sqlite3_bind_null(stmt, paramNumber);
                    }
                    else
                    {
                        var parameterType = parameter.Type.Inspector();

                        if (parameterType.Is(TypeFlags.Boolean))
                            SQLitePCL.raw.sqlite3_bind_int(stmt, paramNumber, value.Convert<bool>() ? 1 : 0);
                        else if (parameterType.Is(TypeFlags.Array | TypeFlags.Byte))
                            SQLitePCL.raw.sqlite3_bind_blob(stmt, paramNumber, (byte[]) value);
                        else if (parameterType.Is(TypeFlags.Integer64))
                            SQLitePCL.raw.sqlite3_bind_int64(stmt, paramNumber, value.Convert<long>());
                        else if (parameterType.Is(TypeFlags.Integer))
                            SQLitePCL.raw.sqlite3_bind_int(stmt, paramNumber, value.Convert<int>());
                        else if (parameterType.Is(TypeFlags.FloatingPoint))
                            SQLitePCL.raw.sqlite3_bind_double(stmt, paramNumber, value.Convert<double>());
                        else if (parameterType.Is(TypeFlags.String))
                            SQLitePCL.raw.sqlite3_bind_text(stmt, paramNumber, value.Convert<string>());
                        else if (parameterType.Is(TypeFlags.Guid))
                            SQLitePCL.raw.sqlite3_bind_text(stmt, paramNumber, ((Guid) value).ToString("N"));
                        else if (parameterType.Is(TypeFlags.DateTime))
                        {
                            switch (DateFormat)
                            {
                                case SqliteDateFormat.String:
                                    SQLitePCL.raw.sqlite3_bind_text(stmt, paramNumber, ((DateTime) value).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    break;
                                case SqliteDateFormat.Unix:
                                    SQLitePCL.raw.sqlite3_bind_int(stmt, paramNumber, (int) (((DateTime) value) - new DateTime(1970, 1, 1)).TotalSeconds);
                                    break;
                                case SqliteDateFormat.Ticks:
                                    SQLitePCL.raw.sqlite3_bind_int64(stmt, paramNumber, ((DateTime) value).Ticks);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                            SQLitePCL.raw.sqlite3_bind_text(stmt, paramNumber, value.Convert<string>());
                    }
                }

            return stmt;
        }


        public override IEnumerable<Dictionary<string, object>> ExecuteSqlReader(string sql, QueryParameterCollection parameters)
        {
            var stopwatch = SqlLogger != null ? Stopwatch.StartNew() : null;

            var stmt = CreateCommand(sql, parameters);

            try
            {
                for (;;)
                {
                    var returnCode = SQLitePCL.raw.sqlite3_step(stmt);

                    if (returnCode == SQLitePCL.raw.SQLITE_BUSY)
                    {
                        Task.Delay(100).Wait();
                        continue;
                    }

                    if (returnCode == SQLitePCL.raw.SQLITE_DONE)
                        break;

                    if (returnCode != SQLitePCL.raw.SQLITE_ROW)
                        throw new SqliteException((SqliteReturnCode) returnCode, (SqliteExtendedErrorCode) SQLitePCL.raw.sqlite3_extended_errcode(DbHandle), SQLitePCL.raw.sqlite3_errmsg(DbHandle).utf8_to_string());

                    Dictionary<string, object> record = new Dictionary<string, object>();

                    for (int i = 0; i < SQLitePCL.raw.sqlite3_column_count(stmt); i++)
                    {
                        string fieldName = SQLitePCL.raw.sqlite3_column_name(stmt, i).utf8_to_string();

                        var columnType = SQLitePCL.raw.sqlite3_column_type(stmt, i);

                        switch (columnType)
                        {
                            case SQLitePCL.raw.SQLITE_BLOB:
                                record[fieldName] = SQLitePCL.raw.sqlite3_column_blob(stmt, i).ToArray();
                                break;
                            case SQLitePCL.raw.SQLITE_TEXT:
                                record[fieldName] = SQLitePCL.raw.sqlite3_column_text(stmt, i).utf8_to_string();
                                break;
                            case SQLitePCL.raw.SQLITE_FLOAT:
                                record[fieldName] = SQLitePCL.raw.sqlite3_column_double(stmt, i);
                                break;
                            case SQLitePCL.raw.SQLITE_INTEGER:
                                record[fieldName] = SQLitePCL.raw.sqlite3_column_int64(stmt, i);
                                break;
                            case SQLitePCL.raw.SQLITE_NULL:
                                record[fieldName] = null;
                                break;
                        }
                    }

                    yield return record;
                }
            }
            finally
            {
                SQLitePCL.raw.sqlite3_finalize(stmt);
            }

            SqlLogger?.LogSql(sql, parameters?.ToDictionary(p => SqlDialect.CreateParameterExpression(p.Name), p => p.Value), stopwatch?.Elapsed ?? TimeSpan.Zero);
        }

        public override void Purge(TableSchema schema)
        {
            var tableName = SqlDialect.QuoteTable(schema.MappedName);

            ExecuteSql("DELETE FROM " + tableName, null);

            if (SqlQueryScalar("select name from sqlite_master where name='sqlite_sequence'", null).Any())
                ExecuteSql("delete from sqlite_sequence where name=@name", QueryParameterCollection.FromObject(new {name = schema.MappedName}));
        }

        public override long GetLastAutoIncrementValue(TableSchema schema)
        {
            return _lastRowId.Value ?? 0;
        }

        public override void Dispose()
        {
            lock (this)
            {
                if (_db != null)
                    SQLitePCL.raw.sqlite3_close(_db);

                _db = null;

                _transactionStack.Clear();
            }
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
    public enum SqliteOpenFlags
    {
        ReadOnly = 0x00000001,
        ReadWrite = 0x00000002,
        Create = 0x00000004,
        Uri = 0x00000040,
        Memory = 0x00000080,
        NoMutex = 0x00008000,
        FullMutex = 0x00010000,
        SharedCache = 0x00020000,
        PrivateCache = 0x00040000
    }

}