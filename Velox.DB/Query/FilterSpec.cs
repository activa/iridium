using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Velox.DB
{
    public class FilterSpec
    {
        public readonly List<QueryExpression> Expressions = new List<QueryExpression>();

        public FilterSpec(FilterSpec parentFilter = null)
        {
            if (parentFilter != null)
            {
                Expressions.AddRange(parentFilter.Expressions);
            }
        }

        public FilterSpec(LambdaExpression lambda, FilterSpec parentFilter = null) : this(parentFilter)
        {
            if (lambda != null)
                Add(lambda);
        }

        public FilterSpec(QueryExpression queryExpression , FilterSpec parentFilter = null) : this(parentFilter)
        {
            if (queryExpression == null) 
                return;

            Expressions.Add(queryExpression);
        }

        private void Add(LambdaExpression lambda)
        {
            var body = lambda.Body;

            if (body.NodeType == ExpressionType.Quote)
                body = ((UnaryExpression)body).Operand;

            if (body.NodeType == ExpressionType.AndAlso)
            {
                var andAlsoExpression = (BinaryExpression) body;

                Add(System.Linq.Expressions.Expression.Lambda(andAlsoExpression.Left, lambda.Parameters[0]));
                Add(System.Linq.Expressions.Expression.Lambda(andAlsoExpression.Right, lambda.Parameters[0]));
            }
            else
            {
                Expressions.Add(new LambdaQueryExpression(lambda));
            }
        }
    }
}