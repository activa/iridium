using System.Linq.Expressions;

namespace Velox.DB
{
    public class ScalarSpec
    {
        internal readonly QueryExpression Expression;

        internal ScalarSpec(LambdaExpression scalarLambda)
        {
            Expression = new LambdaQueryExpression(scalarLambda);
        }

        internal ScalarSpec(QueryExpression expression)
        {
            Expression = expression;
        }
    }

}