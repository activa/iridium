using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Velox.DB.Sql;

namespace Velox.DB.Sql.SqlServer
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
            else
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
                return "delete " + tableName.Alias + " from " + QuoteTable(tableName.TableName) + (tableName.Alias != null ? (" " + tableName.Alias + " ") : "") + " where " + sqlWhere;
            else
                return "delete from " + QuoteTable(tableName.TableName) + " where " + sqlWhere;
        }

        public override string GetLastAutoincrementIdSql(string columnName, string alias, string tableName)
        {
            return "select SCOPE_IDENTITY() as " + alias;
        }
    }
}