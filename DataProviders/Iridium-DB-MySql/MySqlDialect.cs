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

namespace Iridium.DB.MySql
{
    public class MySqlDialect : SqlDialect
    {
        public override string QuoteField(string fieldName)
        {
            return "`" + fieldName.Replace(".", "`.`") + "`";
        }

        public override string QuoteTable(string tableName)
        {
            return "`" + tableName.Replace(".", "`.`") + "`";
        }

        public override string CreateParameterExpression(string parameterName)
        {
            return "?" + parameterName;
        }

        public override string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere)
        {
            if (tableName.Alias != null)
                return "delete " + tableName.Alias + " from " + QuoteTable(tableName.TableName) + (tableName.Alias != null ? (" " + tableName.Alias + " ") : "") + (sqlWhere != null ? (" where " + sqlWhere) : "");
            else
                return "delete from " + QuoteTable(tableName.TableName) + (sqlWhere != null ? (" where " + sqlWhere) : "");
        }

        public override string GetLastAutoincrementIdSql(string columnName, string alias, string tableName)
        {
            return "select last_insert_id() as " + alias;
        }

        public override void CreateOrUpdateTable(TableSchema schema, bool recreateTable, bool recreateIndexes, SqlDataProvider dataProvider)
        {
            const string longTextType = "LONGTEXT";

            var columnMappings = new[]
            {
                new {Flags = TypeFlags.Array | TypeFlags.Byte, ColumnType = "LONGBLOB"},
                new {Flags = TypeFlags.Boolean, ColumnType = "BOOLEAN"},
                new {Flags = TypeFlags.Byte, ColumnType = "TINYINT UNSIGNED"},
                new {Flags = TypeFlags.SByte, ColumnType = "TINYINT"},
                new {Flags = TypeFlags.Int16, ColumnType = "SMALLINT"},
                new {Flags = TypeFlags.UInt16, ColumnType = "SMALLINT UNSIGNED"},
                new {Flags = TypeFlags.Int32, ColumnType = "INT"},
                new {Flags = TypeFlags.UInt32, ColumnType = "INT UNSIGNED"},
                new {Flags = TypeFlags.Int64, ColumnType = "BIGINT"},
                new {Flags = TypeFlags.UInt64, ColumnType = "BIGINT UNSIGNED"},
                new {Flags = TypeFlags.Decimal, ColumnType = "DECIMAL({0},{1})"},
                new {Flags = TypeFlags.Double, ColumnType = "DOUBLE"},
                new {Flags = TypeFlags.Single, ColumnType = "FLOAT"},
                new {Flags = TypeFlags.String, ColumnType = "VARCHAR({0})"},
                new {Flags = TypeFlags.DateTime, ColumnType = "DATETIME"}
            };

            if (recreateTable)
                recreateIndexes = true;

            var existingColumns = dataProvider.ExecuteSqlReader("select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA=DATABASE() and TABLE_NAME=@name", QueryParameterCollection.FromObject(new { name = schema.MappedName })).ToLookup(rec => rec["COLUMN_NAME"].ToString());

            if (existingColumns.Any() && recreateTable)
            {
                dataProvider.ExecuteSql("DROP TABLE " + QuoteTable(schema.MappedName));
            }

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

                if (!field.ColumnNullable)
                    part += " NOT";

                part += " NULL";

//                if (field.PrimaryKey)
//                    part += " PRIMARY KEY";

                if (field.AutoIncrement)
                    part += " AUTO_INCREMENT";

                parts.Add(part);
            }

            if (parts.Any() && schema.PrimaryKeys.Length > 0 && createNew)
            {
                parts.Add("PRIMARY KEY (" + string.Join(",", schema.PrimaryKeys.Select(pk => QuoteField(pk.MappedName))) + ")");
            }

            if (!parts.Any())
                return;

            string sql = (createNew ? "CREATE TABLE " : "ALTER TABLE ") + QuoteTable(schema.MappedName);

            if (createNew)
                sql += " (";

            if (createNew)
                sql += string.Join(",", parts);
            else
                sql += string.Join(",", parts.Select(s => "ADD COLUMN " + s));

            if (createNew)
                sql += ")";

            dataProvider.ExecuteSql(sql);
        }

        public override string SqlFunction(Function function, params string[] parameters)
        {
            switch (function)
            {
                case Function.Coalesce:
                    return $"ifnull({parameters[0]},{parameters[1]})";

                default:
                    return base.SqlFunction(function,parameters);
            }
        }

    }
}