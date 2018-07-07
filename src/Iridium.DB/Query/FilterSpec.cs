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

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Iridium.DB
{
    public class FilterSpec
    {
        public readonly List<QueryExpression> Expressions = new List<QueryExpression>();

        private FilterSpec(FilterSpec parentFilter = null)
        {
            if (parentFilter != null)
            {
                Expressions.AddRange(parentFilter.Expressions);
            }
        }

        public FilterSpec(LambdaExpression lambda, FilterSpec parentFilter = null) : this(parentFilter)
        {
            if (lambda != null)
                Add(lambda);
        }

        public FilterSpec(QueryExpression queryExpression , FilterSpec parentFilter = null) : this(parentFilter)
        {
            if (queryExpression == null) 
                return;

            Expressions.Add(queryExpression);
        }

        private void Add(LambdaExpression lambda)
        {
            var body = lambda.Body;

            if (body.NodeType == ExpressionType.Quote)
                body = ((UnaryExpression)body).Operand;

            if (body.NodeType == ExpressionType.AndAlso)
            {
                var andAlsoExpression = (BinaryExpression) body;

                Add(Expression.Lambda(andAlsoExpression.Left, lambda.Parameters[0]));
                Add(Expression.Lambda(andAlsoExpression.Right, lambda.Parameters[0]));
            }
            else
            {
                Expressions.Add(new LambdaQueryExpression(lambda));
            }
        }
    }
}