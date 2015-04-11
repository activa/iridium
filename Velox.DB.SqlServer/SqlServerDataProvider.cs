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

        public override bool CreateOrUpdateTable(OrmSchema schema)
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

            var existingColumns = ExecuteSqlReader("select * from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA=@schema and TABLE_NAME=@name", new Dictionary<string, object>() {{"schema", tableSchemaName}, {"name", tableName}}).ToLookup(rec => rec["COLUMN_NAME"].ToString());

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
                    part += " IDENTITY(1,1)";

                parts.Add(part);
            }

            if (!parts.Any())
                return true;

            string sql = (createNew ? "CREATE TABLE " : "ALTER TABLE ") + SqlDialect.QuoteTable(schema.MappedName);

            sql += createNew ? " (" : " ADD ";

            sql += string.Join(",", parts);

            if (createNew)
                sql += ")";

            try
            {
                ExecuteSql(sql);

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public override void ClearConnectionPool()
        {
            SqlConnection.ClearAllPools();
        }
    }
}
