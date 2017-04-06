#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015 Philippe Leybaert
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
using System.Threading;
using System.Threading.Tasks;
using Iridium.Core;

namespace Iridium.DB
{
    public class SqliteDataProvider : SqlDataProvider<SqliteDialect>
    {
        private IntPtr? _db;
        private readonly ThreadLocal<long?> _lastRowId = new ThreadLocal<long?>();
        private readonly ISqliteAPI _sqlite3;

        static SqliteDataProvider()
        {
            Win32Loader.CheckAndLoadSqliteLibrary();
        }

        public SqliteDataProvider()
        {
            _sqlite3 = new SqliteAPI();
        }

        public SqliteDataProvider(string fileName = null, SqliteDateFormat dateFormat = SqliteDateFormat.String)
        {
            _sqlite3 = new SqliteAPI();

            FileName = fileName;
            DateFormat = dateFormat;
        }

        public IntPtr DbHandle
        {
            get
            {
                lock (this)
                {
                    if (_db != null)
                        return _db.Value;

                    IntPtr db;

                    _sqlite3.open_v2(FileName, out db, (SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create | SqliteOpenFlags.FullMutex));

                    _db = db;

                    _transactionStack.Clear();

                    ExecuteSql("pragma journal_mode = TRUNCATE",null);
                }

                return _db.Value;
            }
        }

        public string FileName { get; set; }
        public SqliteDateFormat DateFormat { get; set; } = SqliteDateFormat.String;

        public override bool RequiresAutoIncrementGetInSameStatement => false;

        public override int ExecuteSql(string sql, QueryParameterCollection parameters = null)
        {
            lock (this) // although sqlite3 is thread-safe, we need to make sure that the last_insert_rowid is correct
            {
                var stmt = CreateCommand(sql, parameters);

                try
                {
                    SqliteReturnCode returnCode = _sqlite3.step(stmt);

                    if (returnCode == SqliteReturnCode.Row)
                        return 0; // quietly eat any rows being returned

                    if (returnCode != SqliteReturnCode.Done)
                        throw new SqliteException(returnCode, _sqlite3.extended_errcode(DbHandle), _sqlite3.errmsg(DbHandle));

                    _lastRowId.Value = _sqlite3.last_insert_rowid(DbHandle);

                    return _sqlite3.changes(DbHandle);
                }
                finally
                {
                    _sqlite3.finalize(stmt);
                }
            }
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


        private IntPtr CreateCommand(string sql, QueryParameterCollection parameters)
        {
            IntPtr stmt;

            SqliteReturnCode returnCode = _sqlite3.prepare_v2(DbHandle, sql, out stmt);

            if (returnCode != SqliteReturnCode.Ok)
            {
                throw new SqliteException(returnCode, _sqlite3.extended_errcode(DbHandle), _sqlite3.errmsg(DbHandle));
            }

            if (parameters != null)
                foreach (var parameter in parameters)
                {
                    int paramNumber = _sqlite3.bind_parameter_index(stmt, SqlDialect.CreateParameterExpression(parameter.Name));

                    var value = parameter.Value;

                    if (value == null)
                    {
                        _sqlite3.bind_null(stmt, paramNumber);
                    }
                    else
                    {
                        var parameterType = parameter.Type.Inspector();

                        if (parameterType.Is(TypeFlags.Boolean))
                            _sqlite3.bind_int(stmt, paramNumber, value.Convert<bool>() ? 1 : 0);
                        else if (parameterType.Is(TypeFlags.Array | TypeFlags.Byte))
                            _sqlite3.bind_blob(stmt, paramNumber, (byte[]) value);
                        else if (parameterType.Is(TypeFlags.Integer64))
                            _sqlite3.bind_int64(stmt, paramNumber, value.Convert<long>());
                        else if (parameterType.Is(TypeFlags.Integer))
                            _sqlite3.bind_int(stmt, paramNumber, value.Convert<int>());
                        else if (parameterType.Is(TypeFlags.FloatingPoint))
                            _sqlite3.bind_double(stmt, paramNumber, value.Convert<double>());
                        else if (parameterType.Is(TypeFlags.String))
                            _sqlite3.bind_text(stmt, paramNumber, value.Convert<string>());
                        else if (parameterType.Is(TypeFlags.DateTime))
                        {
                            switch (DateFormat)
                            {
                                case SqliteDateFormat.String:
                                    _sqlite3.bind_text(stmt, paramNumber, ((DateTime) value).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    break;
//                                case SqliteDateFormat.Julian:
//                                    _sqlite3.bind_int64(stmt, paramNumber, ((DateTime)value).Ticks);
//                                    break;
                                case SqliteDateFormat.Unix:
                                    _sqlite3.bind_int(stmt, paramNumber, (int) (((DateTime) value) - new DateTime(1970, 1, 1)).TotalSeconds);
                                    break;
                                case SqliteDateFormat.Ticks:
                                    _sqlite3.bind_int64(stmt, paramNumber, ((DateTime) value).Ticks);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                            _sqlite3.bind_text(stmt, paramNumber, value.Convert<string>());
                    }
                }

            return stmt;
        }


        public override IEnumerable<Dictionary<string, object>> ExecuteSqlReader(string sql, QueryParameterCollection parameters)
        {
            var stmt = CreateCommand(sql, parameters);

            try
            {
                for (;;)
                {
                    var returnCode = _sqlite3.step(stmt);

                    if (returnCode == SqliteReturnCode.Busy)
                    {
                        Task.Delay(100).Wait();
                        continue;
                    }

                    if (returnCode == SqliteReturnCode.Done)
                        break;

                    if (returnCode != SqliteReturnCode.Row)
                        throw new SqliteException(returnCode, _sqlite3.extended_errcode(DbHandle), _sqlite3.errmsg(DbHandle));

                    Dictionary<string, object> record = new Dictionary<string, object>();

                    for (int i = 0; i < _sqlite3.column_count(stmt); i++)
                    {
                        string fieldName = _sqlite3.column_name(stmt, i);

                        SqliteColumnType columnType = _sqlite3.column_type(stmt, i);

                        switch (columnType)
                        {
                            case SqliteColumnType.Blob:
                                record[fieldName] = _sqlite3.column_blob(stmt, i);
                                break;
                            case SqliteColumnType.Text:
                                record[fieldName] = _sqlite3.column_text(stmt, i);
                                break;
                            case SqliteColumnType.Float:
                                record[fieldName] = _sqlite3.column_double(stmt, i);
                                break;
                            case SqliteColumnType.Integer:
                                record[fieldName] = _sqlite3.column_int64(stmt, i);
                                break;
                            case SqliteColumnType.Null:
                                record[fieldName] = null;
                                break;
                        }
                    }

                    yield return record;
                }
            }
            finally
            {
                _sqlite3.finalize(stmt);
            }
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
                    _sqlite3.close(_db.Value);

                _db = null;

                _transactionStack.Clear();
            }
        }
    }
}