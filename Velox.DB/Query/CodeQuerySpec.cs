using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace Velox.DB
{
    internal class CodeQuerySpec : ICodeQuerySpec
    {
        private class ExpressionWithReferencedRelations
        {
            public QueryExpression Expression;
            public HashSet<OrmSchema.Relation> Relations;

            public object ObjectWithRelations(object o)
            {
                return Vx.WithLoadedRelations(o, Relations);
            }
        }

        private class SortExpressionWithReferencedRelations : ExpressionWithReferencedRelations
        {
            public SortOrder SortOrder;
        }

        private readonly List<ExpressionWithReferencedRelations> _filterLambdas = new List<ExpressionWithReferencedRelations>();
        private  ExpressionWithReferencedRelations _scalarLambda;

        private readonly List<SortExpressionWithReferencedRelations> _sortExpressions = new List<SortExpressionWithReferencedRelations>();

        public int? Skip { get; set; }
        public int? Take { get; set; }

        public void AddFilter(OrmSchema schema, QueryExpression filter)
        {
            if (filter == null) 
                return;

            _filterLambdas.Add(new ExpressionWithReferencedRelations
            {
                Expression = filter, 
                Relations = filter.FindRelations(schema)
            });
        }

        public void AddScalar(OrmSchema schema, QueryExpression scalarExpression)
        {
            if (scalarExpression == null) 
                return;

            _scalarLambda = new ExpressionWithReferencedRelations()
            {
                Expression = scalarExpression,
                Relations = scalarExpression.FindRelations(schema)
            };
        }

        public void AddSort(OrmSchema schema, QueryExpression expression, SortOrder sortOrder)
        {
            _sortExpressions.Add(new SortExpressionWithReferencedRelations()
            {
                Expression = expression,
                Relations = expression.FindRelations(schema),
                SortOrder = sortOrder
            });
        }

        public bool IsFilterMatch(object o)
        {
            return _filterLambdas.All(exp => (bool)exp.Expression.Evaluate(exp.ObjectWithRelations(o)));
        }

        public object ExpressionValue(object o)
        {
            return _scalarLambda != null ? 
                _scalarLambda.Expression.Evaluate(_scalarLambda.ObjectWithRelations(o)) 
                : 
                null;
        }

        public int Compare(object o1, object o2)
        {
            return (
                from sortExpression in _sortExpressions 
                let value1 = sortExpression.Expression.Evaluate(sortExpression.ObjectWithRelations(o1)) 
                let value2 = sortExpression.Expression.Evaluate(sortExpression.ObjectWithRelations(o2)) 
                where value1 is IComparable && value2 is IComparable 
                let result = ((IComparable) value1).CompareTo(value2) 
                where result != 0
                select sortExpression.SortOrder == SortOrder.Ascending ? result : -result
                )
                .FirstOrDefault();
        }

        public IEnumerable<T> Range<T>(IEnumerable<T> objects)
        {
            if (Skip != null)
                objects = objects.Skip(Skip.Value);

            if (Take != null)
                objects = objects.Take(Take.Value);

            return objects;

        }
    }
}