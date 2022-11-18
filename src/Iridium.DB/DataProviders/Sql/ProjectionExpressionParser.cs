using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Iridium.DB
{
    public class ProjectionInfo
    {
        public ProjectionInfo(HashSet<TableSchema.Field> projectedFields, HashSet<TableSchema.Relation> relationsReferenced, IEnumerable<TableSchema> fullSchemasReferenced)
        {
            ProjectedFields = projectedFields;
            RelationsReferenced = relationsReferenced;
            FullSchemasReferenced = new HashSet<TableSchema>(fullSchemasReferenced);
        }

        public HashSet<TableSchema.Field> ProjectedFields { get; }
        public HashSet<TableSchema.Relation> RelationsReferenced { get; }
        public HashSet<TableSchema> FullSchemasReferenced { get; }

    }

    internal static class ProjectionExtensions
    {
        internal static IEnumerable<TableSchema.Field> Fields(this ProjectionInfo projectionInfo, TableSchema Schema)
        {
            var fields = new HashSet<TableSchema.Field>(Schema.Fields);

            if (projectionInfo == null)
                return fields;

            if (projectionInfo.ProjectedFields != null)
                fields = new HashSet<TableSchema.Field>(projectionInfo.ProjectedFields);

            if (projectionInfo.FullSchemasReferenced != null)
            {
                foreach (var referencedSchema in projectionInfo.FullSchemasReferenced)
                {
                    fields.UnionWith(referencedSchema.Fields);
                }
            }

            return fields;
        }
    }

    internal class ProjectionExpressionParser : ExpressionVisitor
    {
        private readonly TableSchema _schema;

        private readonly HashSet<TableSchema.Field> _fields = new();
        private readonly HashSet<TableSchema.Relation> _relations = new();
        private readonly Dictionary<TableSchema,int> _seenSchemas = new();

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
            Debug.WriteLine($"Parameter: {node}");

            if (node.Type == _schema.ObjectType)
            {
                UpdateSeenSchemas(_schema, 1);
            }

            return base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            Debug.WriteLine($"Member: {memberExpression}");

            var schema = _schema;

            var leftExpression = memberExpression.Expression;
            var memberName = memberExpression.Member.Name;

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
            Debug.WriteLine($"UpdateSeen {schema}: {n}");

            if (_seenSchemas.TryGetValue(schema, out var total))
            {
                _seenSchemas[schema] = total + n;
            }
            else
            {
                _seenSchemas.Add(schema, n);
            }
        }

        private Dictionary<Expression, object> _annotations = new();

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