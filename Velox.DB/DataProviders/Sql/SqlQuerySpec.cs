using System;
using System.Collections.Generic;

namespace Velox.DB.Sql
{
    internal class SqlQuerySpec : INativeQuerySpec
    {
        public string TableAlias { get; set; }
        public string FilterSql { get; set; }
        public string ExpressionSql { get; set; }
        public string SortExpressionSql { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }

        public QueryParameterCollection SqlParameters { get; set; }
        public HashSet<SqlJoinDefinition> Joins { get; set; }

        
    }
}