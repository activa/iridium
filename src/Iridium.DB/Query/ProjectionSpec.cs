using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Iridium.DB
{
    public class ProjectionSpec
    {
        public readonly QueryExpression Expression;

        public ProjectionSpec(LambdaExpression lambda)
        {
            Expression = new LambdaQueryExpression(lambda);
        }

        public ProjectionSpec(QueryExpression queryExpression)
        {
            Expression = queryExpression;
        }
    }
}