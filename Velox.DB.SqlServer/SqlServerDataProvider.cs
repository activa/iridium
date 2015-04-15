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
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Velox.DB.Core;

namespace Velox.DB.Sql.SqlServer
{
    public class SqlServerDataProvider : SqlAdoDataProvider<SqlConnection, SqlServerDialect>
    {
        private const string _longTextType = "TEXT";

        public SqlServerDataProvider(string connectionString) : base(connectionString)
        {
        }

        public override bool CreateOrUpdateTable(OrmSchema schema, bool recreateTable, bool recreateIndexes)
        {
            var columnMappings = new[]
            {
                new {Flags = TypeFlags.Integer8, ColumnType = "TINYINT"},
                new {Flags = TypeFlags.Integer16, ColumnType = "SMALLINT"},
                new {Flags = TypeFlags.Integer32, ColumnType = "INT"},
                new {Flags = TypeFlags.Integer64, ColumnType = "BIGINT"},
                new {Flags = TypeFlags.Decimal, ColumnType = "DECIMAL({0},{1})"},
                new {Flags = TypeFlags.Double, ColumnType = "FLOAT"},
                new {Flags = TypeFlags.Single, ColumnType = "REAL"},
                new {Flags = TypeFlags.String, ColumnType = "VARCHAR({0})"},
                new {Flags = TypeFlags.Array | TypeFlags.Byte, ColumnType = "IMAGE"},
                new {Flags = TypeFlags.DateTime, ColumnType = "DATETIME"}
            };


            string[] tableNameParts = schema.MappedName.Split('.');
            string tableSchemaName = tableNameParts.Length == 1 ? "dbo" : tableNameParts[0];
            string tableName = tableNameParts.Length == 1 ? tableNameParts[0] : tableNameParts[1];

            var existingColumns = ExecuteSqlReader("select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA=@schema and TABLE_NAME=@name", 
                new QueryParameterCollection(new {schema = tableSchemaName, name = tableName})).ToLookup(rec => rec["COLUMN_NAME"].ToString());

            var parts = new List<string>();

            bool createNew = true;

            foreach (var field in schema.FieldList)
            {
                var columnMapping = columnMappings.FirstOrDefault(mapping => field.FieldType.Inspector().Is(mapping.Flags));

                if (columnMapping == null)
                    continue;

                if (existingColumns.Contains(field.MappedName)  && !recreateTable)
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
                    part += " IDENTITY(1,1)";

                parts.Add(part);
            }

            if (recreateTable)
                ExecuteSql("DROP TABLE " + SqlDialect.QuoteTable(schema.MappedName), null);

            if (parts.Any())
            {
                string sql = (createNew ? "CREATE TABLE " : "ALTER TABLE ") + SqlDialect.QuoteTable(schema.MappedName);

                sql += createNew ? " (" : " ADD ";

                sql += string.Join(",", parts);

                if (createNew)
                    sql += ")";

                ExecuteSql(sql, null);
            }

            var existingIndexes = ExecuteSqlReader("SELECT ind.name as IndexName FROM sys.indexes ind INNER JOIN sys.tables t ON ind.object_id = t.object_id WHERE ind.name is not null and ind.is_primary_key = 0 AND t.is_ms_shipped = 0 AND t.name=@tableName",
                 new QueryParameterCollection(new { tableName } )).ToLookup(rec => rec["IndexName"].ToString());

            foreach (var index in schema.Indexes)
            {
                if (existingIndexes.Contains(index.Name))
                {
                    if (recreateIndexes)
                        ExecuteSql("DROP INDEX " + SqlDialect.QuoteTable("IX_" + index.Name) + " ON " + SqlDialect.QuoteTable(schema.MappedName), null);
                    else
                        continue;
                }

                string createIndexSql = "CREATE INDEX " + SqlDialect.QuoteTable("IX_" + index.Name) + " ON " + SqlDialect.QuoteTable(schema.MappedName) + " (";
                
                createIndexSql += string.Join(",", index.FieldsWithOrder.Select(field => SqlDialect.QuoteField(field.Item1.MappedName) + " " + (field.Item2 == SortOrder.Ascending ? "ASC" : "DESC")));

                createIndexSql += ")";

                try
                {
                    ExecuteSql(createIndexSql, null);
                }
                catch (Exception ex)
                {
                    return false;
                }

            }

            return true;

        }

        public override void ClearConnectionPool()
        {
            SqlConnection.ClearAllPools();
        }
    }
}
