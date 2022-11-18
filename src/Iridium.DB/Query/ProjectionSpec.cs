using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Iridium.DB
{
    public class ProjectionSpec
    {
        public readonly QueryExpression Expression;
        public bool Distinct;

        public ProjectionSpec(LambdaExpression lambda, bool distinct = false)
        {
            Expression = new LambdaQueryExpression(lambda);
            Distinct = distinct;
        }

        public ProjectionSpec(ProjectionSpec projection, bool distinct)
        {
            Expression = projection.Expression;
            Distinct = distinct;
        }

        public ProjectionSpec(QueryExpression queryExpression)
        {
            Expression = queryExpression;
        }
    }
}