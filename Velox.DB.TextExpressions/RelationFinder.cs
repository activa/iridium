using System.Collections.Generic;
using System.Diagnostics;
using Velox.Core.Parser;

namespace Velox.DB.TextExpressions
{
    internal sealed class RelationFinder : ExpressionVisitor
    {
        private readonly Stack<OrmSchema> _schemaStack = new Stack<OrmSchema>();

        public readonly HashSet<OrmSchema.Relation> Relations = new HashSet<OrmSchema.Relation>();

        public RelationFinder(Expression expression, OrmSchema schema)
        {
            _schemaStack.Push(schema);

            Visit(expression);
        }

        protected override Expression VisitVariable(VariableExpression expression)
        {
            var relation = _schemaStack.Peek().Relations[expression.VarName];

            Debug.WriteLine("[" + _schemaStack.Peek() + "] VisitVariable(" + expression + ")");

            if (relation == null)
                return expression;

            _schemaStack.Push(relation.ForeignSchema);

            Relations.Add(relation);

            Debug.WriteLine("Found relation for {0}: {1}", expression, relation);
            Debug.WriteLine("Pushed " + relation.ForeignSchema + " on schema stack");

            return expression;
        }

        protected override Velox.Core.Parser.Expression VisitField(FieldExpression expression)
        {
            Debug.WriteLine("[" + _schemaStack.Peek() + "] VisitMember( " + expression + " )");

            Debug.WriteLine("Calling Visit(e.Target) from VisitMember");

            Visit(expression.Target);

            var relation = _schemaStack.Peek().Relations[expression.Member];

            if (relation != null)
            {
                _schemaStack.Push(relation.ForeignSchema);
                Debug.WriteLine("Pushed " + relation.ForeignSchema + " on schema stack");
            }

            if (relation != null)
            {
                Relations.Add(relation);

                Debug.WriteLine("Found relation for {0}: {1}", expression, relation);
            }


            Debug.WriteLine("Popped " + _schemaStack.Peek() + " from schema stack");
            Debug.WriteLine("Exit calling Visit(e.Target) from VisitMember");

            _schemaStack.Pop();



            return expression;
        }

        protected override Velox.Core.Parser.Expression VisitCall(CallExpression node)
        {
            var fieldExpression = node.MethodExpression as FieldExpression;

            if (fieldExpression == null)
                return node;

            switch (fieldExpression.Member)
            {
                case "Any":
                case "Count":
                case "All":
                    break;
                case "Sum":
                case "Avg":
                    break;
            }

            return node;
        }

    }
}