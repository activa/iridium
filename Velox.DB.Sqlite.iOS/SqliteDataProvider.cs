using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;
using Velox.DB.Core;

namespace Velox.DB.Sql.Sqlite
{
    public class SqliteDataProvider : SqlAdoDataProvider<SqliteConnection, SqliteDialect>
    {
        private readonly string _longTextType = "TEXT";

        public SqliteDataProvider(string connectionString) : base(connectionString)
        {
        }

        public SqliteDataProvider(string fileName, bool useDateTimeTicks)
            : this("Data Source=" + fileName + ";DateTimeFormat=" + (useDateTimeTicks ? "Ticks" : "ISO8601"))
        {
        }

        public override bool CreateOrUpdateTable(OrmSchema schema)
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

            var existingColumns = ExecuteSqlReader("pragma table_info(" + SqlDialect.QuoteTable(schema.MappedName) + ")").ToLookup(rec => rec["name"].ToString());

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
                ExecuteSql("CREATE TABLE " + SqlDialect.QuoteTable(schema.MappedName) + " (" + string.Join(",", parts) + ")");
            }
            else
            {
                foreach (var part in parts)
                {
                    ExecuteSql("ALTER TABLE " + SqlDialect.QuoteTable(schema.MappedName) + " ADD COLUMN " + part + ";");
                }

            }

            return true;
        }


        public override void ClearConnectionPool()
        {
            SqliteConnection.ClearAllPools();
        }
    }
}