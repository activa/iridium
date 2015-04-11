using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Velox.DB.Sql
{
    public abstract class SqlDialect
    {
        public virtual string SelectSql(SqlTableNameWithAlias tableName, IEnumerable<SqlExpressionWithAlias> columns, string sqlWhere, IEnumerable<SqlJoinDefinition> joins = null, string sqlSortExpression = null, int? start = null, int? numRecords = null, string afterSelect = null)
        {
            var parts = new List<string>
            {
                "select", 
                string.Join(",", columns.Select(c => string.Format("{0} as {1}", c.ShouldQuote ? QuoteField(c.Expression) : c.Expression, c.Alias))), 
                "from", 
                QuoteTable(tableName.TableName)
            };

            if (afterSelect != null)
                parts.Insert(1,afterSelect);

            if (tableName.Alias != null)
                parts.Add(tableName.Alias);

            if (joins != null)
                parts.Add(string.Join(" ", joins.Select(j => j.ToSql(this))));

            if (!string.IsNullOrWhiteSpace(sqlWhere))
            {
                parts.Add("where");
                parts.Add(sqlWhere);
            }

            if (!string.IsNullOrWhiteSpace(sqlSortExpression))
            {
                parts.Add("order by");
                parts.Add(sqlSortExpression);
            }

            if (start != null || numRecords != null)
            {
                parts.Add("limit");

                if (start != null && numRecords == null)
                    numRecords = int.MaxValue;

                if (start != null)
                    parts.Add((start.Value - 1).ToString());

                if (start != null)
                    parts.Add(",");

                parts.Add(numRecords.ToString());
            }

            return string.Join(" ", parts);
        }

        public virtual string InnerJoinSql(SqlJoinDefinition join)
        {
            return string.Format("{0} join {1} {2} on {3}={4}",
                            join.Type == SqlJoinType.Inner ? "inner" : "left outer",
                            QuoteTable(join.Right.Schema.MappedName),
                            join.Right.Alias,
                            QuoteField(join.Left.Alias + "." + join.Left.Field.MappedName),
                            QuoteField(join.Right.Alias + "." + join.Right.Field.MappedName)
                            );
        }

        public virtual string InsertSql(string tableName, IEnumerable<string> columns, IEnumerable<string> values)
        {
            var parts = new List<string>
            {
                "insert into", 
                QuoteTable(tableName), 
                '(' + string.Join(",", columns.Select(QuoteField)) + ')', 
                "values", 
                "(" + string.Join(",", values) + ")"
            };

            return string.Join(" ", parts);
        }

        public virtual string TruncateTableSql(string tableName)
        {
            return string.Format("truncate table {0}", QuoteTable(tableName));
        }

        public abstract string QuoteField(string fieldName);
        public abstract string QuoteTable(string tableName);
        public abstract string CreateParameterExpression(string parameterName);

        public virtual string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere)
        {
            if (tableName.Alias != null)
                return "delete from " + QuoteTable(tableName.TableName) + (tableName.Alias != null ? (" " + tableName.Alias + " ") : "") + " where " + sqlWhere;
            else
                return "delete from " + QuoteTable(tableName.TableName) + " where " + sqlWhere;
        }

        public virtual string UpdateSql(SqlTableNameWithAlias table, IEnumerable<Tuple<string, string>> setColumns, string sqlWhere)
        {
            return String.Format("update {0} set {1} where {2}",
                            QuoteTable(table.TableName),
                            String.Join(",", setColumns.Select(c => QuoteField(c.Item1) + "=" + c.Item2)),
                            sqlWhere
                            );

        }

        public abstract string GetLastAutoincrementIdSql(string columnName, string alias, string tableName);
    }
}