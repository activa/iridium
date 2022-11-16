using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Iridium.DB
{
    public class ProjectionSpec
    {
        public readonly List<QueryExpression> Expressions = new List<QueryExpression>();

        public ProjectionSpec(LambdaExpression lambda)
        {
            if (lambda != null)
                Expressions.Add(new LambdaQueryExpression(lambda));
        }

        public ProjectionSpec(QueryExpression queryExpression)
        {
            if (queryExpression != null)
                Expressions.Add(queryExpression);
        }
    }
}