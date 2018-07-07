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
    public class SortOrderSpec
    {
        public class ExpressionWithSortOrder
        {
            public ExpressionWithSortOrder(QueryExpression expression, SortOrder sortOrder)
            {
                Expression = expression;
                SortOrder = sortOrder;
            }

            public readonly QueryExpression Expression;
            public readonly SortOrder SortOrder;
        }

        public readonly List<ExpressionWithSortOrder> Expressions = new List<ExpressionWithSortOrder>();

        public SortOrderSpec(SortOrderSpec parent = null)
        {
            if (parent != null)
            {
                Expressions.AddRange(parent.Expressions);
            }
        }

        public SortOrderSpec(LambdaExpression lambda, SortOrder sortOrder, SortOrderSpec parent = null) : this(parent)
        {
            if (lambda != null)
                Expressions.Add(new ExpressionWithSortOrder(new LambdaQueryExpression(lambda), sortOrder));
        }

        public SortOrderSpec(QueryExpression queryExpression, SortOrder sortOrder, SortOrderSpec parent = null) : this(parent)
        {
            if (queryExpression != null)
                Expressions.Add(new ExpressionWithSortOrder(queryExpression, sortOrder));
        }
    }
}