using System.Collections.Generic;
using System.Linq;
using Velox.Core.Parser;
using Velox.DB.Sql;

namespace Velox.DB.TextExpressions
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

        public TextQueryExpression(string textExpression, object parameters = null)
        {
            Expression = new CSharpParser().Parse(textExpression);
            Parameters = new QueryParameterCollection(parameters);
        }


        public override object Evaluate(object target)
        {
            IParserContext context = (Parameters != null) ? new FlexContext(Parameters.ToDictionary(item => "@" + item.Key, item => item.Value)).CreateLocal(target) : new FlexContext(target);

            return Expression.EvaluateToObject(context);
        }

        public override HashSet<OrmSchema.Relation> FindRelations(OrmSchema schema)
        {
            var finder = new RelationFinder(Expression, schema);

            return finder.Relations;
        }

        static TextQueryExpression()
        {
            RegisterPlugin();
        }

        public static void RegisterPlugin()
        {
            SqlExpressionTranslator.RegisterSqlTranslator<TextQueryExpression>(() => new SqlTextExpressionTranslator());
        }
    }
}