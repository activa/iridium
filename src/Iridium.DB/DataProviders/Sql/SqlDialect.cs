#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
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
            Min,
            Max,
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

        public virtual string JoinSql(SqlJoinDefinition j)
        {
            return $"{(j.Type == SqlJoinType.Inner ? "inner" : "left outer")} join {QuoteTable(j.Right.Schema.MappedName)} {j.Right.Alias} on {QuoteField(j.Left.Alias + "." + j.Left.Field.MappedName)}={QuoteField(j.Right.Alias + "." + j.Right.Field.MappedName)}";
        }

        public virtual string InsertSql(string tableName, StringPair[] columns)
        {
            var parts = new []
            {
                "insert into", 
                QuoteTable(tableName), 
                '(' + string.Join(",", columns.Select(c => QuoteField(c.Key))) + ')', 
                "values", 
                "(" + string.Join(",", columns.Select(c => c.Value)) + ")"
            };

            return string.Join(" ", parts);
        }

        public virtual string UpdateSql(string table, StringPair[] setColumns, string[] keyColumns, string sqlWhere)
        {
            return $"update {QuoteTable(table)} set {string.Join(",", setColumns.Select(c => $"{QuoteField(c.Key)}={c.Value}"))} where {sqlWhere}";
        }

        public virtual string InsertOrUpdateSql(string tableName, StringPair[] columns, string[] keyColumns, string sqlWhere)
        {
            var parts = new []
            {
                "replace into",
                QuoteTable(tableName),
                '(' + string.Join(",", columns.Select(c => QuoteField(c.Key))) + ')',
                "values",
                "(" + string.Join(",", columns.Select(c => c.Value)) + ")"
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

        public virtual string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere, IEnumerable<SqlJoinDefinition> joins)
        {
            List<string> parts = new List<string>
            {
                "delete ",
                tableName.Alias ?? QuoteTable(tableName.TableName),
                "from",
                QuoteTable(tableName.TableName)
            };

            if (tableName.Alias != null)
                parts.Add(tableName.Alias);

            if (joins != null)
                parts.Add(string.Join(" ", joins.Select(j => j.ToSql(this))));

            if (!string.IsNullOrWhiteSpace(sqlWhere))
            {
                parts.Add("where");
                parts.Add(sqlWhere);
            }

            return string.Join(" ", parts);
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
                case Function.Min:
                    return $"min({parameters[0]})";
                case Function.Max:
                    return $"max({parameters[0]})";

                default:
                    return null;
            }
        }

        public virtual string StringLiteral(string s)
        {
            return '\'' + s + '\'';
        }

        public abstract bool SupportsInsertOrUpdate { get; }
        public abstract bool RequiresAutoIncrementGetInSameStatement { get; }
    }
}