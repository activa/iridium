#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.DB
{
    internal class CodeQuerySpec : ICodeQuerySpec
    {
        private class ExpressionWithReferencedRelations
        {
            public QueryExpression Expression;
            public HashSet<TableSchema.Relation> Relations;

            public object ObjectWithRelations(object o)
            {
                return Ir.WithLoadedRelations(o, Relations);
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

        public void AddFilter(TableSchema schema, QueryExpression filter)
        {
            if (filter == null) 
                return;

            _filterLambdas.Add(new ExpressionWithReferencedRelations
            {
                Expression = filter, 
                Relations = filter.FindRelations(schema)
            });
        }

        public void AddScalar(TableSchema schema, QueryExpression scalarExpression)
        {
            if (scalarExpression == null) 
                return;

            _scalarLambda = new ExpressionWithReferencedRelations()
            {
                Expression = scalarExpression,
                Relations = scalarExpression.FindRelations(schema)
            };
        }

        public void AddSort(TableSchema schema, QueryExpression expression, SortOrder sortOrder)
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
            return _scalarLambda?.Expression.Evaluate(_scalarLambda.ObjectWithRelations(o));
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