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
using System.ComponentModel;
using System.Linq;
using Velox.DB.Core;
using Velox.DB.Sqlite.WinPhone;


namespace Velox.DB.Sql.Sqlite
{
    public class SqliteDataProvider : SqlDataProvider<SqliteDialect>
    {
        private IntPtr? _db;
        private readonly string _fileName;

        private const string _longTextType = "TEXT";

        public SqliteDataProvider(string fileName)
        {
            _fileName = fileName;
        }

        public IntPtr DbHandle
        {
            get
            {
                if (_db != null)
                    return _db.Value;

                IntPtr db;

                sqlite3.open_v2(_fileName, out db, (int) (sqlite3.OpenFlags.ReadWrite | sqlite3.OpenFlags.FullMutex), IntPtr.Zero);

                _db = db;

                return _db.Value;
            }
        }


        public override bool CreateOrUpdateTable(OrmSchema schema, bool recreateTable, bool recreateIndexes)
        {
            var columnMappings = new[]
            {
                new {Flags = TypeFlags.Integer, ColumnType = "INTEGER"},
                new {Flags = TypeFlags.Decimal, ColumnType = "DECIMAL({0},{1})"},
                new {Flags = TypeFlags.FloatingPoint, ColumnType = "REAL"},
                new {Flags = TypeFlags.String, ColumnType = "VARCHAR({0})"},
                new {Flags = TypeFlags.Array | TypeFlags.Byte, ColumnType = "LONGBLOB"},
                new {Flags = TypeFlags.DateTime, ColumnType = "DATETIME"}
            };

            var existingColumns = ExecuteSqlReader("pragma table_info(" + SqlDialect.QuoteTable(schema.MappedName) + ")", null).ToLookup(rec => rec["name"].ToString());

            var parts = new List<string>();

            bool createNew = true;

            foreach (var field in schema.FieldList)
            {
                var columnMapping = columnMappings.FirstOrDefault(mapping => field.FieldType.Inspector().Is(mapping.Flags));

                if (columnMapping == null)
                    continue;

                if (existingColumns.Contains(field.MappedName))
                {
                    createNew = false;
                    continue;
                }

                if (columnMapping.Flags == TypeFlags.String && field.ColumnSize == int.MaxValue)
                    columnMapping = new { columnMapping.Flags, ColumnType = _longTextType };

                var part = string.Format("{0} {1}", SqlDialect.QuoteField(field.MappedName), string.Format(columnMapping.ColumnType, field.ColumnSize, field.ColumnScale));

                if (!field.ColumnNullable || field.PrimaryKey)
                    part += " NOT";

                part += " NULL";

                if (field.PrimaryKey)
                    part += " PRIMARY KEY";

                if (field.AutoIncrement)
                    part += " AUTOINCREMENT";

                parts.Add(part);
            }

            if (!parts.Any())
                return true;

            if (createNew)
            {
                ExecuteSql("CREATE TABLE " + SqlDialect.QuoteTable(schema.MappedName) + " (" + string.Join(",", parts) + ")", null);
            }
            else
            {
                foreach (var part in parts)
                {
                    ExecuteSql("ALTER TABLE " + SqlDialect.QuoteTable(schema.MappedName) + " ADD COLUMN " + part + ";", null);
                }

            }

            return true;
        }

        public override int ExecuteSql(string sql, QueryParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<SerializedEntity> Query(string sql, QueryParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        public override object QueryScalar(string sql, QueryParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Dictionary<string, object>> ExecuteSqlReader(string sql, QueryParameterCollection parameters)
        {
            IntPtr stmt;

            sqlite3.ReturnCode returnCode = sqlite3.prepare_v2(DbHandle, sql, -1, out stmt, IntPtr.Zero);

            if (returnCode != sqlite3.ReturnCode.Ok)
            {
                throw new Exception();
            }

            foreach (var varName in parameters.Keys)
            {
                int paramNumber = sqlite3.bind_parameter_index(stmt, varName);

                var value = parameters[varName];

                var parameterType = value.GetType().Inspector();

                if (parameterType.Is(TypeFlags.Integer64))
                    sqlite3.bind_int64(stmt, paramNumber, value.Convert<long>());
                else if (parameterType.Is(TypeFlags.Integer))
                    sqlite3.bind_int(stmt, paramNumber, value.Convert<int>());
                if (parameterType.Is(TypeFlags.FloatingPoint))
                    sqlite3.bind_double(stmt, paramNumber, value.Convert<double>());
                if (parameterType.Is(TypeFlags.String))
                    sqlite3.bind_text16(stmt, paramNumber, value.Convert<string>(), -1, new IntPtr(-1));
                if (parameterType.Is(TypeFlags.Array | TypeFlags.Byte))
                    sqlite3.bind_blob(stmt, paramNumber, (byte[]) value, ((byte[]) value).Length, new IntPtr(-1));
                if (parameterType.Is(TypeFlags.DateTime))
                    sqlite3.bind_int64(stmt, paramNumber, ((DateTime) value).Ticks);
                else
                    sqlite3.bind_text16(stmt, paramNumber, value.Convert<string>(), -1, new IntPtr(-1));
            }

            returnCode = sqlite3.ReturnCode.Row;

            while (returnCode == sqlite3.ReturnCode.Row)
            {
                returnCode = sqlite3.ReturnCode.Busy;

                while (returnCode == sqlite3.ReturnCode.Busy)
                {
                    returnCode = sqlite3.step(stmt);
                }

                if (returnCode != sqlite3.ReturnCode.Row)
                {
                    if (returnCode != sqlite3.ReturnCode.Done)
                    {
                        // TODO: handle error
                    }

                    break;
                }

                Dictionary<string, object> record = new Dictionary<string, object>();

                for (int i = 0; i < sqlite3.column_count(stmt); i++)
                {
                    string fieldName = sqlite3.column_name(stmt, i);

                    sqlite3.ColumnType columnType = sqlite3.column_type(stmt, i);

                    switch (columnType)
                    {
                        case sqlite3.ColumnType.Blob:
                            record[fieldName] = sqlite3.column_blob(stmt, i);
                            break;
                        case sqlite3.ColumnType.Text:
                            record[fieldName] = sqlite3.column_text(stmt, i);
                            break;
                        case sqlite3.ColumnType.Float:
                            record[fieldName] = sqlite3.column_double(stmt, i);
                            break;
                        case sqlite3.ColumnType.Integer:
                            record[fieldName] = sqlite3.column_int64(stmt, i);
                            break;
                        case sqlite3.ColumnType.Null:
                            record[fieldName] = null;
                            break;

                    }

                }

                yield return record;

            }
            sqlite3.finalize(stmt);

        }
    }
}