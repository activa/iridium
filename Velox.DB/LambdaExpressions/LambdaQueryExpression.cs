using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Velox.DB
{
    public class LambdaQueryExpression : QueryExpression
    {
        public readonly LambdaExpression Expression;

        public LambdaQueryExpression(LambdaExpression lambdaExpression)
        {
            Expression = lambdaExpression;
        }

        public override object Evaluate(object target)
        {
            switch (Expression.Parameters.Count)
            {
                case 1:
                    return Expression.Compile().DynamicInvoke(target);
                case 0:
                    return Expression.Compile().DynamicInvoke();
                default:
                    throw new Exception("Expression " + Expression + " has too many parameters");
            }
        }

        public override HashSet<OrmSchema.Relation> FindRelations(OrmSchema schema)
        {
            return LambdaRelationFinder.FindRelations(Expression, schema);
        }
    }
}