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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Iridium.Reflection;

namespace Iridium.DB
{
    internal partial class Repository<T> : Repository
    {
        [Preserve]
        public Repository(StorageContext context) : base(typeof(T), context)
        {
        }

        private object GetFieldOrPropertyValue(object obj, string fieldName) // assume obj != null
        {
            var t = obj.GetType();

            var fieldInfo = t.GetRuntimeField(fieldName);

            if (fieldInfo != null)
                return fieldInfo.GetValue(obj);

            var propInfo = t.GetRuntimeProperty(fieldName);

            return propInfo?.GetValue(obj);
        }

        internal T Read(object key, params LambdaExpression[] relationsToLoad)
        {
            var o = Activator.CreateInstance<T>();

            return Load(o, key, relationsToLoad);
        }

        internal T Load(T obj, object key, params LambdaExpression[] relationsToLoad)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            Dictionary<string, object> primaryKey;

            var keyTypeInspector = key.GetType().Inspector();

            if (keyTypeInspector.Is(TypeFlags.Numeric | TypeFlags.Enum | TypeFlags.String | TypeFlags.Guid))
            {
                if (Schema.PrimaryKeys.Length != 1)
                    throw new Exception($"Invalid key for {typeof (T)}");

                primaryKey = new Dictionary<string, object>() {{Schema.PrimaryKeys[0].MappedName, key}};
            }
            else
            {
                primaryKey = Schema.PrimaryKeys.ToDictionary(pk => pk.MappedName, pk => GetFieldOrPropertyValue(key, pk.FieldName));
            }

            var serializedEntity = DataProvider.ReadObject(primaryKey, Schema);

            if (serializedEntity == null)
                return default(T);

            Schema.UpdateObject(obj, serializedEntity);

            var relations = LambdaRelationFinder.FindRelations(relationsToLoad, Schema);

            relations = Schema.BuildPreloadRelationSet(relations);

            Ir.LoadRelations(obj, relations);

            Fire_ObjectRead(obj);

            return obj;
        }

        internal bool Save(T obj, bool? create = null, HashSet<TableSchema.Relation> relationsToSave = null)
        {
            return base.Save(obj, create, relationsToSave);
        }

        internal bool Delete(T obj)
        {
            if (!Fire_ObjectDeleting(obj))
                return false;

            var serializedEntity = Schema.SerializeObject(obj);

            var deleteResult = DataProvider.DeleteObject(serializedEntity, Schema);

            if (deleteResult)
                Fire_ObjectDeleted(obj);

            return deleteResult;
        }

        internal TScalar GetAggregate<TScalar>(Aggregate aggregate, QuerySpec querySpec)
        {
            if (querySpec.Native != null && querySpec.Code == null)
            {
                var result = DataProvider.GetScalar(aggregate, querySpec.Native, Schema).Convert<TScalar>();

                // This awkward hack is needed because the .NET Sum() method will return 0 (zero) on a collection
                // of nullable numbers. Since Iridium tries to honor the .NET LINQ contract, any null value
                // returned from the database needs to be converted to zero.
                if (aggregate == Aggregate.Sum && typeof(TScalar).Inspector().IsNullable && result == null)
                    result = (TScalar) typeof(TScalar).Inspector().RealType.Inspector().DefaultValue();

                return result;
            }

            if (querySpec.Code == null)
                return default;

            var relations = Schema.BuildPreloadRelationSet();

            var objects = from o in DataProvider.GetObjects(querySpec.Native, Schema, null, null, out _)
                          let x = Ir.WithLoadedRelations(Schema.UpdateObject(Activator.CreateInstance<T>(), o), relations)
                          where querySpec.Code == null || querySpec.Code.IsFilterMatch(x)
                          select x;
            
            switch (aggregate)
            {
                case Aggregate.Count:
                    return objects.Count().Convert<TScalar>();
                case Aggregate.Any:
                    return objects.Any().Convert<TScalar>();
                default:
                    var projectedObjects = objects.Select(o => querySpec.Code.ExpressionValue(o).Convert<TScalar>());

                    return Aggregator.AggregateValues(aggregate, projectedObjects);
            }
        }


        internal IEnumerable<T> List(QuerySpec filter, IEnumerable<LambdaExpression> relationLambdas, TableSchema.Relation parentRelation, object parentObject, List<Action<object>> actions)
        {
            var relations = Schema.BuildPreloadRelationSet(LambdaRelationFinder.FindRelations(relationLambdas, Schema));

            var objects = DataProvider
                .GetObjects(filter.Native, Schema, filter.Projection, relations, out var relatedEntities)
                .Select(entity => Schema.UpdateObject(Activator.CreateInstance<T>(), entity));

            if (relatedEntities != null)
            {
                objects = objects.Zip(relatedEntities, (rootObj, relationEntities) =>
                {
                    Dictionary<TableSchema, object> objectsBySchema = null;

                    foreach (var entity in relationEntities)
                    {
                        var relation = entity.Key;
                        var record = entity.Value;

                        if (objectsBySchema == null || !objectsBySchema.TryGetValue(relation.LocalSchema, out var obj))
                            obj = rootObj;

                        if (record == null)
                        {
                            relation.SetField(obj, null);
                        }
                        else
                        {
                            var relationObject = relation.ForeignSchema.UpdateObject(Activator.CreateInstance(relation.FieldType), record);

                            relation.SetField(obj, relationObject);

                            objectsBySchema ??= new Dictionary<TableSchema, object>();

                            objectsBySchema[relation.ForeignSchema] = relationObject;
                        }
                    }

                    return rootObj;
                });
            }

            objects = objects.Select(item => Ir.WithLoadedRelations(item, relations));

            if (parentRelation?.ReverseRelation != null)
                objects = from o in objects select parentRelation.ReverseRelation.SetField(o, parentObject);

            if (filter.Code != null)
            {
                objects = filter.Code.Range((from o in objects where filter.Code.IsFilterMatch(o) select o).OrderBy(o => o, new CustomComparer(filter.Code)));
            }

            objects = objects.Select(o =>
            {
                Fire_ObjectRead(o);

                return o;
            });

            if (actions == null || !actions.Any())
                return objects;

            return objects.Select(o =>
            {
                foreach (var action in actions)
                    action(o);

                return o;
            });
        }

        private class CustomComparer : IComparer<T>
        {
            private readonly ICodeQuerySpec _codeQuerySpec;

            public CustomComparer(ICodeQuerySpec codeQuerySpec)
            {
                _codeQuerySpec = codeQuerySpec;
            }

            public int Compare(T x, T y)
            {
                return _codeQuerySpec.Compare(x, y);
            }
        }

        internal bool Delete(QuerySpec filter)
        {
            if (filter.Code != null)
                throw new Exception("Delete filter not supported");

            return DataProvider.DeleteObjects(filter.Native, Schema);
        }
    }


}
