using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Velox.DB
{
    internal class LambdaRelationFinder : ExpressionVisitor
    {
        public LambdaRelationFinder(Vx.Context context)
        {
            _context = context;
        }

        public readonly HashSet<OrmSchema.Relation> Relations = new HashSet<OrmSchema.Relation>();
        private readonly Vx.Context _context;

        protected override Expression VisitMember(MemberExpression node)
        {
            Visit(node.Expression);

            var schema = _context.GetSchema(node.Expression.Type);

            if (schema == null) 
                return node;

            var relation = schema.Relations[node.Member.Name];

            if (relation == null) 
                return node;

            Relations.Add(relation);

            Debug.WriteLine("Found relation: {0}", node);

            return node;
        }

        public static HashSet<OrmSchema.Relation> FindRelations(LambdaExpression expression, OrmSchema schema)
        {
            var finder = new LambdaRelationFinder(schema.Repository.Context);

            finder.Visit(expression.Body);

            return finder.Relations;
        }

        public static HashSet<OrmSchema.Relation> FindRelations(IEnumerable<LambdaExpression> expressions, OrmSchema schema)
        {
            if (expressions == null)
                return null;

            HashSet<OrmSchema.Relation> relations = null;

            foreach (var lambdaExpression in expressions)
            {
                if (relations == null)
                    relations = new HashSet<OrmSchema.Relation>(FindRelations(lambdaExpression,schema));
                else
                    relations.UnionWith(FindRelations(lambdaExpression, schema));
            }

            return relations;
        }


    }


    
}