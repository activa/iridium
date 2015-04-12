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
using MySql.Data.MySqlClient;
using Velox.DB.Core;

namespace Velox.DB.Sql.MySql
{
    public class MySqlDataProvider : SqlAdoDataProvider<MySqlConnection, MySqlDialect>
    {
        private readonly string _longTextType = "LONGTEXT";

        public MySqlDataProvider(string connectionString) : base(connectionString)
        {
        }


        public override bool CreateOrUpdateTable(OrmSchema schema)
        {
            var columnMappings = new[]
            {
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
                new {Flags = TypeFlags.Array | TypeFlags.Byte, ColumnType = "LONGBLOB"},
                new {Flags = TypeFlags.DateTime, ColumnType = "DATETIME"}
            };

            var existingColumns = ExecuteSqlReader("select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA=DATABASE() and TABLE_NAME=@name", new Dictionary<string, object>() { { "name", schema.MappedName } }).ToLookup(rec => rec["COLUMN_NAME"].ToString());

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

                if (!field.ColumnNullable)
                    part += " NOT";

                part += " NULL";

                if (field.PrimaryKey)
                    part += " PRIMARY KEY";

                if (field.AutoIncrement)
                    part += " AUTO_INCREMENT";

                parts.Add(part);
            }

            if (!parts.Any())
                return true;

            string sql = (createNew ? "CREATE TABLE " : "ALTER TABLE ") + SqlDialect.QuoteTable(schema.MappedName);

            sql += createNew ? " (" : " ADD COLUMN ";

            sql += string.Join(",", parts);

            if (createNew)
                sql += ")";


            try
            {
                ExecuteSql(sql);

                return true;
            }
            catch
            {
                return false;
            }


        }

        public override void ClearConnectionPool()
        {
            MySqlConnection.ClearAllPools();
        }
    }
}
