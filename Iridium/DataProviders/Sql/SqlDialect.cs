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

namespace Iridium.DB
{
    public abstract class SqlDialect
    {
        public enum Function
        {
            StringLength,
            BlobLength,
            Coalesce,
            Trim,
            Sum,
            Average,
            Count
        }

        public virtual string SelectSql(SqlTableNameWithAlias tableName, IEnumerable<SqlExpressionWithAlias> columns, string sqlWhere, IEnumerable<SqlJoinDefinition> joins = null, string sqlSortExpression = null, int? start = null, int? numRecords = null, string afterSelect = null)
        {
            var parts = new List<string>
            {
                "select", 
                string.Join(",", columns.Select(c => $"{(c.ShouldQuote ? QuoteField(c.Expression) : c.Expression)} as {c.Alias}")), 
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

        public virtual string JoinSql(SqlJoinDefinition join)
        {
            return string.Format("{0} join {1} {2} on {3}={4}",
                            join.Type == SqlJoinType.Inner ? "inner" : "left outer",
                            QuoteTable(@join.Right.Schema.MappedName),
                            join.Right.Alias,
                            QuoteField(join.Left.Alias + "." + @join.Left.Field.MappedName),
                            QuoteField(join.Right.Alias + "." + @join.Right.Field.MappedName)
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
            return $"truncate table {QuoteTable(tableName)}";
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
            return string.Format("update {0} set {1} where {2}",
                            QuoteTable(table.TableName),
                            string.Join(",", setColumns.Select(c => $"{QuoteField(c.Item1)}={c.Item2}")),
                            sqlWhere
                            );

        }

        public abstract string GetLastAutoincrementIdSql(string columnName, string alias, string tableName);

        public abstract void CreateOrUpdateTable(TableSchema schema, bool recreateTable, bool recreateIndexes, SqlDataProvider dataProvider);

        public virtual string SqlFunction(Function function, params string[] parameters)
        {
            switch (function)
            {
                case Function.StringLength:
                    return $"length({parameters[0]})";
                case Function.BlobLength:
                    return $"length({parameters[0]})";
                case Function.Coalesce:
                    return $"coalesce({parameters[0]},{parameters[1]})";
                case Function.Trim:
                    return $"trim({parameters[0]}";
                case Function.Sum:
                    return $"sum({parameters[0]})";
                case Function.Average:
                    return $"avg({parameters[0]})";
                case Function.Count:
                    return $"count({parameters[0]})";

                default:
                    return null;
            }
        }

        public virtual string StringLiteral(string s)
        {
            return '\'' + s + '\'';
        }
    }
}