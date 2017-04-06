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
using System.Linq;
using Iridium.Core;

namespace Iridium.DB
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
            throw new NotSupportedException(); // Is handled in DataProvider class
        }

        public override string GetLastAutoincrementIdSql(string columnName, string alias, string tableName)
        {
            throw new NotSupportedException(); // Is handled in DataProvider class
        }

        public override string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere, IEnumerable<SqlJoinDefinition> joins = null)
        {
            if (joins != null && joins.Any())
                throw new NotSupportedException("Sqlite does not support delete with joins");

            if (tableName.Alias != null)
                sqlWhere = sqlWhere?.Replace(QuoteTable(tableName.Alias) + ".", "");
            
            return "delete from " + QuoteTable(tableName.TableName) + (sqlWhere != null ? (" where " + sqlWhere) : "");
        }

        public override void CreateOrUpdateTable(TableSchema schema, bool recreateTable, bool recreateIndexes, SqlDataProvider dataProvider)
        {
            const string longTextType = "TEXT";

            var columnMappings = new[]
            {
                new {Flags = TypeFlags.Array | TypeFlags.Byte, ColumnType = "LONGBLOB"},
                new {Flags = TypeFlags.Boolean, ColumnType = "TINYINT"},
                new {Flags = TypeFlags.Integer, ColumnType = "INTEGER"},
                new {Flags = TypeFlags.Decimal, ColumnType = "DECIMAL({0},{1})"},
                new {Flags = TypeFlags.FloatingPoint, ColumnType = "REAL"},
                new {Flags = TypeFlags.String, ColumnType = "VARCHAR({0})"},
                new {Flags = TypeFlags.DateTime, ColumnType = "DATETIME"}
            };

            if (recreateTable)
                recreateIndexes = true;


            HashSet<string> tableNames = new HashSet<string>(dataProvider.ExecuteSqlReader("SELECT name FROM sqlite_master WHERE type='table'", null).Select(rec => rec["name"].ToString()),StringComparer.OrdinalIgnoreCase);

            if (tableNames.Contains(schema.MappedName) && recreateTable)
            {
                dataProvider.ExecuteSql("DROP TABLE " + QuoteTable(schema.MappedName), null);
            }

            var existingColumns = dataProvider.ExecuteSqlReader("pragma table_info(" + QuoteTable(schema.MappedName) + ")", null).ToLookup(rec => rec["name"].ToString());

            var parts = new List<string>();

            bool createNew = true;

            

            foreach (var field in schema.Fields)
            {
                var columnMapping = columnMappings.FirstOrDefault(mapping => field.FieldInfo.Inspector().Type.Inspector().Is(mapping.Flags));

                if (columnMapping == null)
                    continue;

                if (existingColumns.Contains(field.MappedName) && !recreateTable)
                {
                    createNew = false;
                    continue;
                }

                if (columnMapping.Flags == TypeFlags.String && field.ColumnSize == int.MaxValue)
                    columnMapping = new { columnMapping.Flags, ColumnType = longTextType };

                var part = $"{QuoteField(field.MappedName)} {string.Format(columnMapping.ColumnType, field.ColumnSize, field.ColumnScale)}";

                if (!field.ColumnNullable || field.PrimaryKey)
                    part += " NOT";

                part += " NULL";

                if (field.PrimaryKey && schema.PrimaryKeys.Length == 1)
                {
                    part += " PRIMARY KEY";

                    if (field.AutoIncrement)
                        part += " AUTOINCREMENT";
                }

                parts.Add(part);
            }

            if (parts.Any() && schema.PrimaryKeys.Length > 1)
            {
                parts.Add("PRIMARY KEY (" + string.Join(",", schema.PrimaryKeys.Select(pk => QuoteField(pk.MappedName))) + ")");
            }

            if (parts.Any())
            {
                if (createNew)
                {
                    dataProvider.ExecuteSql("CREATE TABLE " + QuoteTable(schema.MappedName) + " (" + string.Join(",", parts) + ")", null);
                }
                else
                {
                    foreach (var part in parts)
                    {
                        dataProvider.ExecuteSql("ALTER TABLE " + QuoteTable(schema.MappedName) + " ADD COLUMN " + part + ";", null);
                    }

                }
            }

            var existingIndexes = dataProvider.ExecuteSqlReader("PRAGMA INDEX_LIST(" + QuoteTable(schema.MappedName) + ")", null).ToLookup(rec => rec["name"].ToString());

            foreach (var index in schema.Indexes)
            {
                if (existingIndexes[index.Name].Any())
                {
                    if (recreateIndexes)
                        dataProvider.ExecuteSql("DROP INDEX " + QuoteTable(index.Name) + " ON " + QuoteTable(schema.MappedName), null);
                    else
                        continue;
                }

                string createIndexSql = "CREATE INDEX " + QuoteTable(index.Name) + " ON " + QuoteTable(schema.MappedName) + " (";

                createIndexSql += string.Join(",", index.FieldsWithOrder.Select(field => QuoteField(field.Item1.MappedName) + " " + (field.Item2 == SortOrder.Ascending ? "ASC" : "DESC")));

                createIndexSql += ")";

                dataProvider.ExecuteSql(createIndexSql, null);
            }

        }

    }
}
