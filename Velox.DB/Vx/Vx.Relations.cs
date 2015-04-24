#region License
//=============================================================================
// Velox.DB - Portable .NET ORM 
//
// Copyright (c) 2015 Philippe Leybaert
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Velox.DB
{
    public partial class Vx
    {
        public static void LoadRelations<T>(T obj, params Expression<Func<T, object>>[] relationsToLoad)
        {
            _LoadRelations(obj, relationsToLoad, null);
        }

        public static void LoadRelations<T>(T obj, IEnumerable<Expression<Func<T, object>>> relationsToLoad)
        {
            _LoadRelations(obj, relationsToLoad, null);
        }

        public static void LoadRelations(params Expression<Func<object>>[] relationsToLoad)
        {
            foreach (var relation in relationsToLoad)
            {
                var memberExpression = relation.Body as MemberExpression;

                if (memberExpression != null)
                {
                    _LoadRelations(Expression.Lambda(memberExpression.Expression).Compile().DynamicInvoke(), new[] { relation }, null);
                }
            }
        }

        // Internal & private methods

        internal static T WithLoadedRelations<T>(T obj, IEnumerable<OrmSchema.Relation> relations)
        {
            if (relations != null)
                _LoadRelations(obj, relations);

            return obj;
        }

        private static void _LoadRelations(object obj, IEnumerable<LambdaExpression> relationsToLoad, OrmSchema parentSchema)
        {
            if (parentSchema == null)
                parentSchema = SchemaForObject(obj);

            if (parentSchema == null)
                throw new Exception("LoadRelations() not supported for multiple contexts");

            _LoadRelations(obj, LambdaRelationFinder.FindRelations(relationsToLoad, parentSchema));
        }

        private static void _LoadRelations(object obj, IEnumerable<OrmSchema.Relation> relationsToLoad)
        {
            var objectType = obj.GetType();

            var splitRelations = relationsToLoad.ToLookup(r => r.LocalSchema.ObjectType == objectType);

            var localRelations = splitRelations[true];

            foreach (var relation in localRelations)
            {
                object value = relation.GetField(obj);

                if (value == null)
                {
                    value = relation.LoadRelation(obj);

                    relation.SetField(obj, value);

                    if (value == null)
                        continue;
                }

                var deepRelations = splitRelations[false].ToList(); // avoid multiple enumerations

                if (deepRelations.Count == 0)
                    continue;

                if (relation.RelationType == OrmSchema.RelationType.OneToMany)
                {
                    foreach (var item in (IEnumerable)value)
                    {
                        _LoadRelations(item, deepRelations);
                    }
                }
                else
                {
                    _LoadRelations(value, deepRelations);
                }
            }
        }

        private static OrmSchema SchemaForObject(object obj)
        {
            return DB.GetSchema(obj.GetType());
        }

    }
}
