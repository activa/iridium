using System.Collections.Generic;
using System.Linq;
using Velox.Core.Parser;

namespace Velox.DB
{
    public class TextQueryExpression : QueryExpression
    {
        public readonly Expression Expression;
        public readonly QueryParameterCollection Parameters;

        public TextQueryExpression(Expression textExpression, QueryParameterCollection parameters = null)
        {
            Expression = textExpression;
            Parameters = parameters;
        }

        public override object Evaluate(object target)
        {
            IParserContext context = (Parameters != null) ? new FlexContext(Parameters.ToDictionary(item => "@" + item.Key, item => item.Value)).CreateLocal(target) : new FlexContext(target);

            return Expression.EvaluateToObject(context);
        }

        public override HashSet<OrmSchema.Relation> FindRelations(OrmSchema schema)
        {
            return RelationFinder.FindRelationsInExpression(Expression, schema);
        }
    }
}