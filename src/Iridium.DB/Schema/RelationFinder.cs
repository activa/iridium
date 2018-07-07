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
using Iridium.Reflection;

namespace Iridium.DB
{
    internal class LambdaRelationFinder : ExpressionVisitor
    {
        private readonly TableSchema _schema;
        private readonly HashSet<TableSchema.Relation> _relations = new HashSet<TableSchema.Relation>();

        private LambdaRelationFinder(TableSchema schema)
        {
            _schema = schema;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Visit(node.Expression);

            var schema = _schema.Repository.Context.GetSchema(node.Expression.Type, autoCreate: false);

            if (schema == null && _schema.ObjectType.Inspector().ImplementsOrInherits(node.Expression.Type))
                schema = _schema;

            var relation = schema?.Relations[node.Member.Name];

            if (relation == null) 
                return node;

            _relations.Add(relation);

            return node;
        }

        public static HashSet<TableSchema.Relation> FindRelations(LambdaExpression expression, TableSchema schema)
        {
            var finder = new LambdaRelationFinder(schema);

            finder.Visit(expression.Body);

            return finder._relations;
        }

        public static HashSet<TableSchema.Relation> FindRelations(IEnumerable<LambdaExpression> expressions, TableSchema schema)
        {
            if (expressions == null)
                return null;

            HashSet<TableSchema.Relation> relations = null;

            foreach (var lambdaExpression in expressions)
            {
                if (relations == null)
                    relations = new HashSet<TableSchema.Relation>(FindRelations(lambdaExpression,schema));
                else
                    relations.UnionWith(FindRelations(lambdaExpression, schema));
            }

            return relations;
        }
    }
}