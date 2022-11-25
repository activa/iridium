using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Iridium.DB
{
    internal class ProjectionExpressionParser : ExpressionVisitor
    {
        private readonly TableSchema _schema;

        private readonly HashSet<TableSchema.Field> _fields = new();
        private readonly HashSet<TableSchema.Relation> _relations = new();
        private readonly Dictionary<TableSchema,int> _seenSchemas = new();
        private readonly Dictionary<Expression, object> _annotations = new();

        public ProjectionExpressionParser(TableSchema schema)
        {
            _schema = schema;
        }

        public ProjectionInfo FindFields(Expression expression)
        {
            if (expression is LambdaExpression lambdaExpression)
                Visit(lambdaExpression.Body);
            else
                Visit(expression);


            return new ProjectionInfo(_fields, _relations, _seenSchemas.Where(kv => kv.Value > 0).Select(kv => kv.Key));
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type == _schema.ObjectType)
            {
                UpdateSeenSchemas(_schema, 1);
            }

            return base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var schema = _schema;

            var leftExpression = memberExpression.Expression;
            var memberName = memberExpression.Member.Name;

            if (leftExpression == null) // static member
                return memberExpression;

            if (leftExpression.NodeType == ExpressionType.Convert)
                leftExpression = ((UnaryExpression)leftExpression).Operand;

            if (leftExpression is ParameterExpression paramExpression && paramExpression.Type == _schema.ObjectType)
            {
                UpdateSeenSchemas(_schema, -1);
            }

            leftExpression = Visit(leftExpression);

            if (Annotation(leftExpression) is TableSchema.Relation relation)
            {
                schema = relation.ForeignSchema;

                UpdateSeenSchemas(relation.ForeignSchema, -1);
            }

            if (schema.FieldsByFieldName.ContainsKey(memberName))
            {
                _fields.Add(schema.FieldsByFieldName[memberName]);
            }
            else if (schema.Relations.TryGetValue(memberName, out var rel))
            {
                if (rel.RelationType == TableSchema.RelationType.OneToMany)
                {
                    _fields.Add(rel.LocalField);
                }
                else
                {

                    UpdateSeenSchemas(rel.ForeignSchema, 1);

                    _relations.Add(rel);

                    Annotate(memberExpression, rel);
                }
            }

            return memberExpression;
        }

        private void UpdateSeenSchemas(TableSchema schema, int n)
        {
            if (_seenSchemas.TryGetValue(schema, out var total))
            {
                _seenSchemas[schema] = total + n;
            }
            else
            {
                _seenSchemas.Add(schema, n);
            }
        }


        public void Annotate(Expression expression, object annotation)
        {
            _annotations[expression] = annotation;
        }

        public object Annotation(Expression expression)
        {
            return _annotations.TryGetValue(expression, out var value) ? value : null;
        }

    }
}