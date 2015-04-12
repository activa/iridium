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
using Velox.DB.Core;
using Velox.DB.Sql;

namespace Velox.DB
{
    internal abstract class Repository
    {
        private static readonly SafeDictionary<Type, Repository> _activeRepositories = new SafeDictionary<Type, Repository>();

        internal Vx.Context Context { get; private set; }
        internal OrmSchema Schema { get; private set; }
        internal IDataProvider DataProvider { get { return Context.DataProvider; } }

        public int QueryCount
        {
            get
            {
                lock (_statsLock)
                    return _statsQueryCount;
            }
            set 
            {
                lock (_statsLock)
                {
                    _statsQueryCount = value;
                }
            }
        }

        public void IncrementQueryCount()
        {
            lock (_statsLock)
                _statsQueryCount++;
        }

        protected int _statsQueryCount = 0;
        protected long _statsQqueryTime = 0;

        protected object _statsLock = new object();

        protected Repository(Type type, Vx.Context context)
        {
            lock (_activeRepositories)
            {
                if (_activeRepositories.ContainsKey(type) && _activeRepositories[type] != this)
                    _activeRepositories[type] = null; // Prevents usage of static repositories list when more than one context is used
                else
                    _activeRepositories[type] = this;
            }

            Context = context;

            Schema = new OrmSchema(type, this);
        }

        internal static DataSet<T> CreateDataSet<T>()
        {
            lock (_activeRepositories)
            {
                return new DataSet<T>(_activeRepositories[typeof(T)]);
            }
        }

        public static Repository GetRepository(Type t)
        {
            lock (_activeRepositories)
            {
                return _activeRepositories[t];
            }
        }

        protected internal IEnumerable<object> GetRelationObjects(QuerySpec filter)
        {
            lock (_statsLock)
                _statsQueryCount++;

            var objects = from o in DataProvider.GetObjects(filter.Native,Schema) 
                          let x = Vx.WithLoadedRelations(Schema.UpdateObject(Activator.CreateInstance(Schema.ObjectType), o),Schema.DatasetRelations) 
                          select x;

            if (filter.Code != null)
            {
                objects = from o in objects 
                          where filter.Code.IsFilterMatch(o) 
                          select o;
            }

            return objects;
        }

        protected bool Save(object obj, bool saveRelations = false, bool? create = null)
        {
            var toOneRelations = Schema.Relations.Values.Where(r => r.RelationType == OrmSchema.RelationType.ManyToOne && !r.ReadOnly);
            var toManyRelations = Schema.Relations.Values.Where(r => r.RelationType == OrmSchema.RelationType.OneToMany && !r.ReadOnly);

            // Update and save ManyToOne relations
            foreach (var relation in toOneRelations)
            {
                var foreignObject = relation.GetField(obj);

                if (foreignObject == null)
                    continue;

                if (saveRelations)
                    relation.ForeignSchema.Repository.Save(foreignObject);

                var foreignKeyValue = relation.ForeignField.GetField(foreignObject);

                if (!Equals(relation.LocalField.GetField(obj), foreignKeyValue))
                    relation.LocalField.SetField(obj, foreignKeyValue);
            }

            if (create == null)
                create = Schema.IncrementKeys.Length > 0 && Equals(Schema.IncrementKeys[0].GetField(obj), Schema.IncrementKeys[0].FieldType.Inspector().DefaultValue());

            var serializedEntity = Schema.SerializeObject(obj);

            lock (_statsLock)
                _statsQueryCount++;

            var result = DataProvider.WriteObject(serializedEntity, create.Value, Schema);

            if (result.OriginalUpdated)
                Schema.UpdateObject(obj, serializedEntity);


            // Update and save OneToMany relations
            foreach (var relation in toManyRelations)
            {
                var foreignCollection = (IEnumerable) relation.GetField(obj);

                if (foreignCollection == null)
                    continue;

                foreach (var foreignObject in foreignCollection)
                {
                    var foreignKeyValue = relation.ForeignField.GetField(foreignObject);
                    var localKeyValue = relation.LocalField.GetField(obj);

                    if (!Equals(localKeyValue, foreignKeyValue))
                        relation.ForeignField.SetField(foreignObject, localKeyValue);

                    if (saveRelations)
                        relation.ForeignSchema.Repository.Save(foreignObject);
                }
            }


            return result.Success;
        }

        internal void Purge()
        {
            DataProvider.Purge(Schema);
        }

        internal QuerySpec CreateQuerySpec(FilterSpec filter, ScalarSpec scalarSpec = null, int? skip = null, int? take = null, SortOrderSpec sortSpec = null)
        {
            if (DataProvider.SupportsQueryTranslation(null))
                return DataProvider.CreateQuerySpec(filter, scalarSpec, sortSpec, skip, take, Schema); // TODO: implement support for hybrid providers

            var querySpec = new QuerySpec(new CodeQuerySpec(), null);

            var codeQuerySpec = (CodeQuerySpec) querySpec.Code;

            if (filter != null)
                foreach (var expression in filter.Expressions)
                    codeQuerySpec.AddFilter(Schema, expression);

            if (sortSpec != null)
                foreach (var expression in sortSpec.Expressions)
                    codeQuerySpec.AddSort(Schema, expression.Expression, expression.SortOrder);

            if (scalarSpec != null)
                codeQuerySpec.AddScalar(Schema, scalarSpec.Expression);

            codeQuerySpec.Skip = skip;
            codeQuerySpec.Take = take;

            return querySpec;
        }
    }


    internal class Repository<T> : Repository
    {
        [Preserve]
        public Repository(Vx.Context context) : base(typeof(T), context)
        {
        }

        internal T Read(object key, params Expression<Func<T,object>>[] relationsToLoad)
        {
            var o = Activator.CreateInstance<T>();

            return Load(o, key, relationsToLoad);
        }

        internal T Load(T obj, object key, params Expression<Func<T,object>>[] relationsToLoad)
        {
            if (Schema.PrimaryKeys.Length != 1)
                throw new Exception(string.Format("{0} primary keys defined for {1}", Schema.PrimaryKeys.Length, typeof(T)));

            lock (_statsLock)
                _statsQueryCount++;

            var serializedEntity = DataProvider.ReadObject(new []{key}, Schema);

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
            var serializedEntity = Schema.SerializeObject(obj);

            lock (_statsLock)
                _statsQueryCount++;

            return DataProvider.DeleteObject(serializedEntity, Schema);
        }

        internal TScalar GetAggregate<TScalar>(Aggregate aggregate, QuerySpec querySpec) 
        {
            lock (_statsLock)
                _statsQueryCount++;

            if (querySpec.Native != null && querySpec.Code == null)
                return DataProvider.GetScalar(aggregate, querySpec.Native, Schema).Convert<TScalar>();

            var objects = from o in DataProvider.GetObjects(querySpec.Native, Schema) 
                          let x = Vx.WithLoadedRelations(Schema.UpdateObject(Activator.CreateInstance<T>(), o) ,Schema.DatasetRelations) 
                          where querySpec.Code == null || querySpec.Code.IsFilterMatch(x) 
                          select x;

            double? zero = querySpec.Code == null ? 0.0 : (double?) null;

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
                IEnumerable<Dictionary<OrmSchema.Relation,SerializedEntity>> relatedEntities;

                lock (_statsLock)
                    _statsQueryCount++;

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
                lock (_statsLock)
                    _statsQueryCount++;

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
            lock (_statsLock)
                _statsQueryCount++;

            if (filter.Native != null)
                return DataProvider.DeleteObjects(filter.Native, Schema);

            var objects = from o in DataProvider.GetObjects(null, Schema) 
                          let x = Schema.UpdateObject(Activator.CreateInstance<T>(), o) 
                          where filter.Code.IsFilterMatch(x) 
                          select x;

            foreach (var o in objects.ToArray())
            {
                lock (_statsLock)
                    _statsQueryCount++;

                DataProvider.DeleteObject(Schema.SerializeObject(o), Schema);
            }

            return true;
        }


    }
}