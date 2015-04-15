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
using System.Linq;
using Velox.DB.Core;

namespace Velox.DB.Sql.Sqlite
{
    public class SqliteDialect : SqlDialect
    {
        public override string QuoteField(string fieldName)
        {
            return "\"" + fieldName.Replace(".", "\".\"") + "\"";
        }

        public override string QuoteTable(string tableName)
        {
            return "\"" + tableName.Replace("\"", "\".\"") + "\"";
        }

        public override string CreateParameterExpression(string parameterName)
        {
            return "@" + parameterName;
        }

        public override string TruncateTableSql(string tableName)
        {
            return "DELETE FROM " + QuoteTable(tableName) + ";delete from sqlite_sequence where name='" + tableName + "'";
        }

        public override string GetLastAutoincrementIdSql(string columnName, string alias, string tableName)
        {
            return "select last_insert_rowid() as " + alias;
        }

        public override string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere)
        {
            if (tableName.Alias != null)
                sqlWhere = sqlWhere.Replace(QuoteTable(tableName.Alias) + ".", "");
            
            return "delete from " + QuoteTable(tableName.TableName) + " where " + sqlWhere;
        }

        public override void CreateOrUpdateTable(OrmSchema schema, bool recreateTable, bool recreateIndexes, Func<string, QueryParameterCollection, IEnumerable<Dictionary<string,object>>> fnExecuteReader, Action<string, QueryParameterCollection> fnExecuteSql)
        {
            const string longTextType = "TEXT";

            var columnMappings = new[]
            {
                new {Flags = TypeFlags.Integer, ColumnType = "INTEGER"},
                new {Flags = TypeFlags.Decimal, ColumnType = "DECIMAL({0},{1})"},
                new {Flags = TypeFlags.FloatingPoint, ColumnType = "REAL"},
                new {Flags = TypeFlags.String, ColumnType = "VARCHAR({0})"},
                new {Flags = TypeFlags.Array | TypeFlags.Byte, ColumnType = "LONGBLOB"},
                new {Flags = TypeFlags.DateTime, ColumnType = "DATETIME"}
            };

            var existingColumns = fnExecuteReader("pragma table_info(" + QuoteTable(schema.MappedName) + ")", null).ToLookup(rec => rec["name"].ToString());

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
                    columnMapping = new { columnMapping.Flags, ColumnType = longTextType };

                var part = string.Format("{0} {1}", QuoteField(field.MappedName), string.Format(columnMapping.ColumnType, field.ColumnSize, field.ColumnScale));

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
                return;

            if (createNew)
            {
                fnExecuteSql("CREATE TABLE " + QuoteTable(schema.MappedName) + " (" + string.Join(",", parts) + ")", null);
            }
            else
            {
                foreach (var part in parts)
                {
                    fnExecuteSql("ALTER TABLE " + QuoteTable(schema.MappedName) + " ADD COLUMN " + part + ";", null);
                }

            }
        }

    }
}
