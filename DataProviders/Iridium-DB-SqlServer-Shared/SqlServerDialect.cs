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
using Iridium.DB.CoreUtil;

namespace Iridium.DB.SqlServer
{
    public class SqlServerDialect : SqlDialect
    {
        public override string SelectSql(SqlTableNameWithAlias tableName, IEnumerable<SqlExpressionWithAlias> columns, string sqlWhere, IEnumerable<SqlJoinDefinition> joins = null, string sqlSortExpression = null, int? start = null, int? numRecords = null, string afterSelect = null)
        {
            if (start == null && numRecords == null)
                return base.SelectSql(tableName, columns, sqlWhere, joins, sqlSortExpression);

            if (start == null)
                return base.SelectSql(tableName, columns, sqlWhere, joins, sqlSortExpression, afterSelect: "TOP " + numRecords);

            string subQueryAlias = SqlNameGenerator.NextTableAlias();
            string rowNumAlias = SqlNameGenerator.NextFieldAlias();

            int end = (numRecords == null ? int.MaxValue : (numRecords.Value + start - 1)).Value;

            SqlExpressionWithAlias rowNumExpression = new SqlExpressionWithAlias("row_number() over (order by " + sqlSortExpression + ")", rowNumAlias);

            string[] parts =
            {
                "select",
                subQueryAlias + ".*",
                "from",
                "(",
                base.SelectSql(tableName, columns.Union(new[] {rowNumExpression}), sqlWhere, joins),
                ")",
                "as",
                subQueryAlias,
                "where",
                rowNumAlias,
                "between",
                start.ToString(),
                "and",
                end.ToString(),
                "order by",
                rowNumAlias
            };

            return string.Join(" ", parts);
        }

        public override string QuoteField(string fieldName)
        {
            int dotIdx = fieldName.IndexOf('.');

            if (dotIdx > 0)
                return fieldName.Substring(0, dotIdx + 1) + "[" + fieldName.Substring(dotIdx + 1) + "]";
            
            return "[" + fieldName + "]";
        }

        public override string QuoteTable(string tableName)
        {
            return "[" + tableName.Replace(".", "].[") + "]";
        }

        public override string CreateParameterExpression(string parameterName)
        {
            return "@" + parameterName;
        }

        public override string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere)
        {
            if (tableName.Alias != null)
                return "delete " + tableName.Alias + " from " + QuoteTable(tableName.TableName) + (tableName.Alias != null ? (" " + tableName.Alias + " ") : "") +  (sqlWhere != null ? (" where " + sqlWhere) : "");
            else
                return "delete from " + QuoteTable(tableName.TableName) + (sqlWhere != null ? (" where " + sqlWhere) : "");
        }

        public override string GetLastAutoincrementIdSql(string columnName, string alias, string tableName)
        {
            return "select SCOPE_IDENTITY() as " + alias;
        }

        public override string SqlFunction(Function function, params string[] parameters)
        {
            switch (function)
            {
                case Function.StringLength:
                    return $"len({parameters[0]})";
                case Function.BlobLength:
                    return $"datalength({parameters[0]})";

                default:
                    return base.SqlFunction(function, parameters);
            }
        }

        public override void CreateOrUpdateTable(TableSchema schema, bool recreateTable, bool recreateIndexes, SqlDataProvider dataProvider)
        {
            const string longTextType = "TEXT";

            var columnMappings = new[]
            {
                new {Flags = TypeFlags.Array | TypeFlags.Byte, ColumnType = "IMAGE"},
                new {Flags = TypeFlags.Boolean, ColumnType = "BIT"},
                new {Flags = TypeFlags.Integer8, ColumnType = "TINYINT"},
                new {Flags = TypeFlags.Integer16, ColumnType = "SMALLINT"},
                new {Flags = TypeFlags.Integer32, ColumnType = "INT"},
                new {Flags = TypeFlags.Integer64, ColumnType = "BIGINT"},
                new {Flags = TypeFlags.Decimal, ColumnType = "DECIMAL({0},{1})"},
                new {Flags = TypeFlags.Double, ColumnType = "FLOAT"},
                new {Flags = TypeFlags.Single, ColumnType = "REAL"},
                new {Flags = TypeFlags.String, ColumnType = "VARCHAR({0})"},
                new {Flags = TypeFlags.DateTime, ColumnType = "DATETIME"}
            };


            string[] tableNameParts = schema.MappedName.Split('.');
            string tableSchemaName = tableNameParts.Length == 1 ? "dbo" : tableNameParts[0];
            string tableName = tableNameParts.Length == 1 ? tableNameParts[0] : tableNameParts[1];

            var existingColumns = dataProvider.ExecuteSqlReader("select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA=@schema and TABLE_NAME=@name",
                QueryParameterCollection.FromObject(new { schema = tableSchemaName, name = tableName })).ToLookup(rec => rec["COLUMN_NAME"].ToString());

            var tableExists = dataProvider.ExecuteSqlReader("select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA=@schema and TABLE_NAME=@name",
                QueryParameterCollection.FromObject(new {schema = tableSchemaName, name = tableName})).Select(rec => rec.First().Value.Convert<int>()).First() == 1;

            var parts = new List<string>();

            bool createNew = true;

            foreach (var field in schema.Fields)
            {
                var columnMapping = columnMappings.FirstOrDefault(mapping => field.FieldInfo.TypeInspector.Is(mapping.Flags));

                if (columnMapping == null)
                    continue;

                if (existingColumns.Contains(field.MappedName) && !recreateTable)
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

                if (field.AutoIncrement)
                    part += " IDENTITY(1,1)";

                parts.Add(part);
            }

            if (parts.Any() && schema.PrimaryKeys.Length > 0)
            {
                parts.Add("PRIMARY KEY (" + string.Join(",", schema.PrimaryKeys.Select(pk => QuoteField(pk.MappedName))) + ")");
            }

            if (recreateTable && tableExists)
                dataProvider.ExecuteSql("DROP TABLE " + QuoteTable(schema.MappedName), null);

            if (parts.Any())
            {
                string sql = (createNew ? "CREATE TABLE " : "ALTER TABLE ") + QuoteTable(schema.MappedName);

                sql += createNew ? " (" : " ADD ";

                sql += string.Join(",", parts);

                if (createNew)
                    sql += ")";

                dataProvider.ExecuteSql(sql, null);
            }

            var existingIndexes = dataProvider.ExecuteSqlReader("SELECT ind.name as IndexName FROM sys.indexes ind INNER JOIN sys.tables t ON ind.object_id = t.object_id WHERE ind.name is not null and ind.is_primary_key = 0 AND t.is_ms_shipped = 0 AND t.name=@tableName",
                 QueryParameterCollection.FromObject(new { tableName })).ToLookup(rec => rec["IndexName"].ToString());

            foreach (var index in schema.Indexes)
            {
                if (existingIndexes["IX_" + index.Name].Any())
                {
                    if (recreateIndexes || recreateTable)
                        dataProvider.ExecuteSql($"DROP INDEX {QuoteTable("IX_" + index.Name)} ON {QuoteTable(schema.MappedName)}", null);
                    else
                        continue;
                }

                string createIndexSql = $"CREATE INDEX {QuoteTable("IX_" + index.Name)} ON {QuoteTable(schema.MappedName)} (";

                createIndexSql += string.Join(",", index.FieldsWithOrder.Select(field => QuoteField(field.Item1.MappedName) + " " + (field.Item2 == SortOrder.Ascending ? "ASC" : "DESC")));

                createIndexSql += ")";

                dataProvider.ExecuteSql(createIndexSql, null);
            }
        }
    }
}