using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace Velox.DB
{
    public class SortOrderSpec
    {
        public class ExpressionWithSortOrder
        {
            public ExpressionWithSortOrder(QueryExpression expression, SortOrder sortOrder)
            {
                Expression = expression;
                SortOrder = sortOrder;
            }

            public QueryExpression Expression;
            public SortOrder SortOrder;
        }

        public readonly List<ExpressionWithSortOrder> Expressions = new List<ExpressionWithSortOrder>();

        public SortOrderSpec(SortOrderSpec parent = null)
        {
            if (parent != null)
            {
                Expressions.AddRange(parent.Expressions);
            }
        }

        public SortOrderSpec(LambdaExpression lambda, SortOrder sortOrder, SortOrderSpec parent = null) : this(parent)
        {
            if (lambda != null)
                Expressions.Add(new ExpressionWithSortOrder(new LambdaQueryExpression(lambda), sortOrder));
        }

        public SortOrderSpec(QueryExpression queryExpression, SortOrder sortOrder, SortOrderSpec parent = null) : this(parent)
        {
            if (queryExpression != null)
                Expressions.Add(new ExpressionWithSortOrder(queryExpression, sortOrder));
        }
    }
}