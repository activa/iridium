#region License
//=============================================================================
// Velox.DB - Portable .NET ORM 
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
using System.Threading;
using System.Threading.Tasks;
using Velox.DB.Core;
using Velox.DB.Sql;
using Velox.DB.Sqlite.API;
using Velox.DB.Sqlite.win32;

namespace Velox.DB.Sqlite
{
    public class SqliteDataProvider : SqlDataProvider<SqliteDialect>
    {
        private IntPtr? _db;
        private string _fileName;
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

        public SqliteDataProvider(string fileName)
        {
            _sqlite3 = new SqliteAPI();
            _fileName = fileName;
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

                    _sqlite3.open_v2(_fileName, out db, (SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create | SqliteOpenFlags.FullMutex));

                    _db = db;
                }

                return _db.Value;
            }
        }

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public override bool RequiresAutoIncrementGetInSameStatement
        {
            get { return false; }
        }

        public override int ExecuteSql(string sql, QueryParameterCollection parameters)
        {
            lock (this) // although sqlite3 is thread-safe, we need to make sure that the last_insert_rowid is correct
            {
                var stmt = CreateCommand(sql, parameters);

                try
                {
                    SqliteReturnCode returnCode = _sqlite3.step(stmt);

                    if (returnCode != SqliteReturnCode.Done)
                        throw new Exception(_sqlite3.errmsg(DbHandle));

                    _lastRowId.Value = _sqlite3.last_insert_rowid(DbHandle);

                    return _sqlite3.changes(DbHandle);
                }
                finally
                {
                    _sqlite3.finalize(stmt);
                }
            }
        }

        private readonly ThreadLocal<Stack<bool>> _transactionStack = new ThreadLocal<Stack<bool>>(() => new Stack<bool>());

        public override void BeginTransaction(Vx.IsolationLevel isolationLevel)
        {
            if (isolationLevel == Vx.IsolationLevel.None)
                _transactionStack.Value.Push(false);
            else
            {
                ExecuteSql("BEGIN TRANSACTION", null);

                _transactionStack.Value.Push(true);
            }

        }

        public override void CommitTransaction()
        {
            bool realTransaction = _transactionStack.Value.Pop();

            if (realTransaction)
                ExecuteSql("COMMIT", null);

        }

        public override void RollbackTransaction()
        {
            bool realTransaction = _transactionStack.Value.Pop();

            if (realTransaction)
                ExecuteSql("ROLLBACK", null);
        }


        private IntPtr CreateCommand(string sql, QueryParameterCollection parameters)
        {
            IntPtr stmt;

            Debug.WriteLine("{0}",sql);

            SqliteReturnCode returnCode = _sqlite3.prepare_v2(DbHandle, sql, out stmt);

            if (returnCode != SqliteReturnCode.Ok)
            {
                throw new Exception(_sqlite3.errmsg(DbHandle));
            }

            if (parameters != null)
                foreach (var varName in parameters.Keys)
                {
                    int paramNumber = _sqlite3.bind_parameter_index(stmt, SqlDialect.CreateParameterExpression(varName));

                    var value = parameters[varName];

                    if (value == null)
                    {
                        _sqlite3.bind_null(stmt, paramNumber);
                    }
                    else
                    {
                        var parameterType = value.GetType().Inspector();

                        if (parameterType.Is(TypeFlags.Boolean))
                            _sqlite3.bind_int(stmt, paramNumber, value.Convert<bool>() ? 1 : 0);
                        else if (parameterType.Is(TypeFlags.Integer64))
                            _sqlite3.bind_int64(stmt, paramNumber, value.Convert<long>());
                        else if (parameterType.Is(TypeFlags.Integer))
                            _sqlite3.bind_int(stmt, paramNumber, value.Convert<int>());
                        else if (parameterType.Is(TypeFlags.FloatingPoint))
                            _sqlite3.bind_double(stmt, paramNumber, value.Convert<double>());
                        else if (parameterType.Is(TypeFlags.String))
                            _sqlite3.bind_text(stmt, paramNumber, value.Convert<string>());
                        else if (parameterType.Is(TypeFlags.Array | TypeFlags.Byte))
                            _sqlite3.bind_blob(stmt, paramNumber, (byte[])value);
                        else if (parameterType.Is(TypeFlags.DateTime))
                            _sqlite3.bind_int64(stmt, paramNumber, ((DateTime)value).Ticks);
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
                        throw new Exception(_sqlite3.errmsg(DbHandle));

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

        public override void Purge(OrmSchema schema)
        {
            var tableName = SqlDialect.QuoteTable(schema.MappedName);

            ExecuteSql("DELETE FROM " + tableName, null);
            ExecuteSql("delete from sqlite_sequence where name=@name", new QueryParameterCollection(new { name = schema.MappedName }));
        }

        public override long GetLastAutoIncrementValue(OrmSchema schema)
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
            }
        }
    }

}