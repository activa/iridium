using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Velox.DB.Core;

namespace Velox.DB
{
    internal partial class Repository<T> : Repository
    {
        [Preserve]
        public Repository(Vx.Context context)
            : base(typeof(T), context)
        {
        }

        private object GetFieldOrPropertyValue(object obj, string fieldName) // assume obj != null
        {
            var t = obj.GetType();

            var fieldInfo = t.GetRuntimeField(fieldName);

            if (fieldInfo != null)
                return fieldInfo.GetValue(obj);

            var propInfo = t.GetRuntimeProperty(fieldName);

            if (propInfo != null)
                return propInfo.GetValue(obj);

            return null;
        }

        internal T Read(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            var o = Activator.CreateInstance<T>();

            return Load(o, key, relationsToLoad);
        }

        internal T Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            Dictionary<string, object> primaryKey = null;

            var keyTypeInspector = key.GetType().Inspector();

            if (keyTypeInspector.Is(TypeFlags.Numeric | TypeFlags.Enum | TypeFlags.String))
            {
                if (Schema.PrimaryKeys.Length != 1)
                    throw new Exception(string.Format("Invalid key for {0}", typeof (T)));

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

            if (Schema.DatasetRelations != null)
            {
                if (relations == null)
                    relations = new HashSet<OrmSchema.Relation>(Schema.DatasetRelations);
                else
                    relations.UnionWith(Schema.DatasetRelations);
            }

            return Vx.WithLoadedRelations(obj, relations);
        }

        internal bool Save(T obj, bool saveRelations = false, bool? create = null)
        {
            return base.Save(obj, saveRelations, create);
        }

        internal bool Delete(T obj)
        {
            bool cancelDelete = false;

            Fire_ObjectDeleting(obj, ref cancelDelete);

            if (cancelDelete)
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
                return DataProvider.GetScalar(aggregate, querySpec.Native, Schema).Convert<TScalar>();

            var objects = from o in DataProvider.GetObjects(querySpec.Native, Schema)
                          let x = Vx.WithLoadedRelations(Schema.UpdateObject(Activator.CreateInstance<T>(), o), Schema.DatasetRelations)
                          where querySpec.Code == null || querySpec.Code.IsFilterMatch(x)
                          select x;

            double? zero = querySpec.Code == null ? 0.0 : (double?)null;

            switch (aggregate)
            {
                case Aggregate.Sum:
                    return objects.Sum(o => (zero ?? querySpec.Code.ExpressionValue(o).Convert<double>())).Convert<TScalar>();
                case Aggregate.Average:
                    return objects.Average(o => zero ?? querySpec.Code.ExpressionValue(o).Convert<double>()).Convert<TScalar>();
                case Aggregate.Max:
                    return objects.Max(o => zero ?? querySpec.Code.ExpressionValue(o).Convert<double>()).Convert<TScalar>();
                case Aggregate.Min:
                    return objects.Min(o => zero ?? querySpec.Code.ExpressionValue(o).Convert<double>()).Convert<TScalar>();
                case Aggregate.Count:
                    return objects.Count().Convert<TScalar>();
                case Aggregate.Any:
                    return objects.Any().Convert<TScalar>();
                default:
                    throw new ArgumentOutOfRangeException("aggregate");
            }
        }

        internal IEnumerable<T> List(QuerySpec filter, IEnumerable<Expression<Func<T, object>>> relationLambdas = null)
        {
            IEnumerable<T> objects;

            var relations = LambdaRelationFinder.FindRelations(relationLambdas, Schema);

            var prefetchRelations = relations == null ? null : relations.Where(r => r.RelationType == OrmSchema.RelationType.ManyToOne && r.LocalSchema == Schema).ToList();

            if (Schema.DatasetRelations != null)
            {
                if (relations == null)
                    relations = new HashSet<OrmSchema.Relation>(Schema.DatasetRelations);
                else
                    relations.UnionWith(Schema.DatasetRelations);
            }

            if (prefetchRelations != null && prefetchRelations.Count > 0 && DataProvider.SupportsRelationPrefetch)
            {
                IEnumerable<Dictionary<OrmSchema.Relation, SerializedEntity>> relatedEntities;

                objects = DataProvider
                    .GetObjectsWithPrefetch(filter.Native, Schema, prefetchRelations, out relatedEntities)
                    .Select(entity => Schema.UpdateObject(Activator.CreateInstance<T>(), entity))
                    .Zip(relatedEntities, (obj, relationEntities) =>
                    {
                        foreach (var entity in relationEntities)
                        {
                            var relation = entity.Key;

                            obj = relation.SetField(obj, entity.Value == null ? null : relation.ForeignSchema.UpdateObject(Activator.CreateInstance(relation.FieldType), entity.Value));
                        }

                        return obj;
                    })
                    .Select(item => Vx.WithLoadedRelations(item, relations));

            }
            else
            {
                objects = from o in DataProvider.GetObjects(filter.Native, Schema) select Vx.WithLoadedRelations(Schema.UpdateObject(Activator.CreateInstance<T>(), o), relations);
            }

            if (filter.Code != null)
            {
                return filter.Code.Range((from o in objects where filter.Code.IsFilterMatch(o) select o).OrderBy(o => o, new CustomComparer(filter.Code)));
            }

            return objects;
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
            if (filter.Native != null)
                return DataProvider.DeleteObjects(filter.Native, Schema);

            var objects = from o in DataProvider.GetObjects(null, Schema)
                          let x = Schema.UpdateObject(Activator.CreateInstance<T>(), o)
                          where filter.Code.IsFilterMatch(x)
                          select x;

            foreach (var o in objects.ToArray())
                DataProvider.DeleteObject(Schema.SerializeObject(o), Schema);

            return true;
        }
    }
}
