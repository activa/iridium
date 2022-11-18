using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Iridium.Reflection;

namespace Iridium.DB
{
    internal sealed class DataSet<T,TImpl> : DataSetWithNewObjects, IDataSet<T> where TImpl:T
    {
        private readonly Repository<TImpl> _repository;
        private readonly FilterSpec _filter;
        private readonly SortOrderSpec _sortOrder;
        private readonly ProjectionSpec _projection;
        private readonly List<Expression<Func<T, object>>> _relationsToLoad;
        private readonly List<Action<object>> _actions;
        private int? _skip;
        private int? _take;
        private readonly TableSchema.Relation _parentRelation;
        private readonly object _parentObject;
        private List<T> _newObjects;

        public DataSet(Repository repository)
        {
            _repository = (Repository<TImpl>) repository;
        }

        public DataSet(StorageContext context)
        {
            _repository = context.GetRepository<TImpl>();
        }

        internal override IList NewObjects => _newObjects;

        [Preserve]
        public DataSet(Repository repository, FilterSpec filter)
        {
            _repository = (Repository<TImpl>)repository;
            _filter = filter;
        }
        
        [Preserve]
        public DataSet(Repository repository, FilterSpec filter, TableSchema.Relation parentRelation, object parentObject)
        {
            _repository = (Repository<TImpl>)repository;
            _filter = filter;
        
            _parentRelation = parentRelation;
            _parentObject = parentObject;
        }

        private DataSet(
            DataSet<T,TImpl> baseDataSet, 
            FilterSpec newFilterSpec = null, 
            SortOrderSpec newSortSpec = null, 
            IEnumerable<Expression<Func<T,object>>> additionalRelations = null, 
            IEnumerable<Action<object>> additionalActions = null, 
            ProjectionSpec projectionSpec = null
            )
        {
            if (baseDataSet._newObjects is { Count: > 0 })
                throw new Exception("DataSet with added objects can't be chained");

            _repository = baseDataSet._repository;

            _skip = baseDataSet._skip;
            _take = baseDataSet._take;

            _filter = newFilterSpec ?? baseDataSet._filter;
            _sortOrder = newSortSpec ?? baseDataSet._sortOrder;
            _projection = projectionSpec ?? baseDataSet._projection;

            if (baseDataSet._relationsToLoad != null)
                _relationsToLoad = new List<Expression<Func<T, object>>>(baseDataSet._relationsToLoad);

            if (additionalRelations != null)
            {
                if (_relationsToLoad == null)
                    _relationsToLoad = new List<Expression<Func<T, object>>>(additionalRelations);
                else
                    _relationsToLoad.AddRange(additionalRelations);
            }

            if (additionalActions != null)
            {
                if (_actions == null)
                    _actions = new List<Action<object>>(additionalActions);
                else
                    _actions.AddRange(additionalActions);
            }

            _parentRelation = baseDataSet._parentRelation;
            _parentObject = baseDataSet._parentObject;
        }

        public IAsyncDataSet<T> Async()
        {
            return new AsyncDataSet<T>(this);
        }

        public IDataSet<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            return new DataSet<T,TImpl>(this, newFilterSpec: new FilterSpec(whereExpression, _filter));
        }

        public IDataSet<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T,TImpl>(this, newSortSpec: new SortOrderSpec(keySelector,SortOrder.Ascending, _sortOrder));
        }

        public IDataSet<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T,TImpl>(this, newSortSpec: new SortOrderSpec(keySelector, SortOrder.Descending, _sortOrder));
        }

        public IDataSet<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T,TImpl>(this, newSortSpec: new SortOrderSpec(keySelector, SortOrder.Ascending, _sortOrder));
        }

        public IDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T,TImpl>(this, newSortSpec: new SortOrderSpec(keySelector, SortOrder.Descending, _sortOrder));
        }

        public IDataSet<T> OrderBy(QueryExpression expression, SortOrder sortOrder)
        {
            return new DataSet<T,TImpl>(this, newSortSpec: new SortOrderSpec(expression, sortOrder, _sortOrder));
        }

        private IEnumerable<T> Enumerate()
        {
            if (_newObjects is { Count: > 0 })
                throw new Exception("Trying to enumerate dataset with pending changes");

            return _repository.List(
                _repository.CreateQuerySpec(
                    _filter, 
                    sortSpec: _sortOrder,
                    projectionSpec: _projection,
                    skip: _skip, 
                    take: _take), 
                _relationsToLoad, 
                _parentRelation, 
                _parentObject, 
                _actions)
                .Cast<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }



        public void Purge()
        {
            _repository.Purge();
        }

        public T Read(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return _repository.Read(key,relationsToLoad);
        }

        public T Read(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return WithRelations(relationsToLoad).FirstOrDefault(condition);
        }

        public T Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return _repository.Load((TImpl) obj, key, relationsToLoad);
        }

        public bool Save(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return _repository.Save((TImpl) obj, create: null, relationsToSave: LambdaRelationFinder.FindRelations(relationsToSave, _repository.Schema));
        }

        public bool Save(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave)
        {
            return _repository.Context.RunTransaction(() => objects.All(o => Save(o, relationsToSave)));
        }

        public bool InsertOrUpdate(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return _repository.Save((TImpl) obj, create: null, relationsToSave: LambdaRelationFinder.FindRelations(relationsToSave, _repository.Schema));
        }

        public bool InsertOrUpdate(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave)
        {
            return _repository.Context.RunTransaction(() => objects.All(o => InsertOrUpdate(o, relationsToSave)));
        }

        public bool Insert(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Insert(obj, deferSave: null, relationsToSave: relationsToSave);
        }

        public bool Add(T obj)
        {
            return Insert(obj, deferSave: null);
        }

        public bool Insert(T obj, bool? deferSave, params Expression<Func<T,object>>[] relationsToSave)
        {
            var isOneToMany = _parentRelation?.RelationType == TableSchema.RelationType.OneToMany;

            if (isOneToMany)
            {
                var hasParentId = _parentRelation.LocalField.GetField(_parentObject) != _parentRelation.LocalField.FieldType.Inspector().DefaultValue();

                if (deferSave == null)
                {
                    deferSave = !hasParentId;
                }
                else
                {
                    if (!deferSave.Value && !hasParentId)
                        throw new ArgumentException($"{nameof(deferSave)} cannot be false when parent object is not saved yet");
                }

                if (!deferSave.Value)
                {
                    if (_parentRelation.ReverseRelation != null)
                        _parentRelation.ReverseRelation.SetField(obj, _parentObject);

                    _parentRelation.ForeignField.SetField(obj, _parentRelation.LocalField.GetField(_parentObject));
                }
                else
                {
                    if (_newObjects == null)
                        _newObjects = new List<T>();

                    _newObjects.Add(obj);

                    return true;
                }
            }
            else
            {
                if (deferSave == true)
                    throw new ArgumentException($"{nameof(deferSave)} is only applicable for one-to-many relations");
            }

            return _repository.Save((TImpl) obj, create: true, relationsToSave: LambdaRelationFinder.FindRelations(relationsToSave, _repository.Schema));
        }

        public bool Insert(IEnumerable<T> objects, bool? deferSave = null, params Expression<Func<T, object>>[] relationsToSave)
        {
            return _repository.Context.RunTransaction(() => objects.All(o => Insert(o, deferSave, relationsToSave)));
        }

        public bool Update(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return _repository.Save((TImpl) obj, create: false, relationsToSave: LambdaRelationFinder.FindRelations(relationsToSave, _repository.Schema));
        }

        public bool Update(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave)
        {
            return _repository.Context.RunTransaction(() => objects.All(o => Update(o, relationsToSave)));
        }

        public bool Delete(T obj)
        {
            return _repository.Delete((TImpl) obj);
        }

        public bool Delete(IEnumerable<T> objects)
        {
            return _repository.Context.RunTransaction(() => objects.All(Delete));
        }

        public bool DeleteAll()
        {
            return _repository.Delete(_repository.CreateQuerySpec(_filter));
        }

        public bool Delete(Expression<Func<T, bool>> filter)
        {
            return Where(filter).DeleteAll();
        }

        public bool Delete(QueryExpression filterExpression)
        {
            return _repository.Delete(_repository.CreateQuerySpec(new FilterSpec(filterExpression)));
        }

        public IDataSet<T> WithRelations(params Expression<Func<T, object>>[] relationsToLoad)
        {
            return new DataSet<T,TImpl>(this, additionalRelations: relationsToLoad);
        }

        public IDataSet<T> Where(QueryExpression filterExpression)
        {
            return new DataSet<T,TImpl>(this, newFilterSpec: new FilterSpec(filterExpression));
        }

        public IDataSet<T> WithAction(Action<T> action)
        {
            return WithActions(action);
        }

        public IDataSet<T> WithActions(params Action<T>[] actions)
        {
            return new DataSet<T,TImpl>(this, additionalActions: actions.Select(action => new Action<object>(obj => action((T) obj))));
        }

        public IDataSet<T> Skip(int n)
        {
            n += _skip ?? 0;

            return new DataSet<T,TImpl>(this) {_skip = n};
        }

        public IDataSet<T> Take(int n)
        {
            return new DataSet<T,TImpl>(this) { _take = Math.Min(n,_take ?? int.MaxValue) };
        }

        public IProjectedDataSet<TResult,T> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            return new ProjectedDataSet<TResult, T>(new DataSet<T, TImpl>(this, projectionSpec: new ProjectionSpec(selector)), selector);
        }

        public IDataSet<T> Distinct()
        {
            if (_projection == null)
                throw new Exception("Distinct() can only be called after Select()");

            return new DataSet<T, TImpl>(this, projectionSpec: new ProjectionSpec(_projection, true));
        }

        public T First()
        {
            return _repository.List(_repository.CreateQuerySpec(_filter, sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad, _parentRelation, _parentObject, _actions).First();
        }

        public T First(Expression<Func<T, bool>> filter)
        {
            return _repository.List(_repository.CreateQuerySpec(new FilterSpec(filter, _filter), sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad, _parentRelation, _parentObject, _actions).First();
        }

        public T FirstOrDefault()
        {
            return _repository.List(_repository.CreateQuerySpec(_filter, sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad, _parentRelation, _parentObject, _actions).FirstOrDefault();
        }

        public T FirstOrDefault(Expression<Func<T, bool>> filter)
        {
            return _repository.List(_repository.CreateQuerySpec(new FilterSpec(filter, _filter), sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad, _parentRelation, _parentObject, _actions).FirstOrDefault();
        }

        public long Count()
        {
            return _repository.GetAggregate<long>(Aggregate.Count, _repository.CreateQuerySpec(_filter, skip: _skip, take: _take));
        }

        public long Count(Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<long>(Aggregate.Count, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), skip: _skip, take: _take));
        }

        public TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Max, _repository.CreateQuerySpec(new FilterSpec(filter, _filter),new ScalarSpec(expression), skip: _skip, take: _take));
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Min, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), new ScalarSpec(expression), skip: _skip, take: _take));
        }

        public TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Sum, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), new ScalarSpec(expression), skip: _skip, take: _take));
        }

        public bool All(Expression<Func<T, bool>> filter)
        {
            return Count(Expression.Lambda<Func<T, bool>>(Expression.Not(filter.Body), filter.Parameters[0])) == 0;
        }

        public TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Max, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression), skip: _skip, take: _take));
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Min, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression), skip: _skip, take: _take));
        }

        public TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Sum, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression), skip: _skip, take: _take));
        }

        private TScalar Average<TScalar>(LambdaExpression expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Average, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression), skip: _skip, take: _take));
        }

        private TScalar Average<TScalar>(LambdaExpression expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Average, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), new ScalarSpec(expression), skip: _skip, take: _take));
        }

        public double Average(Expression<Func<T, int>> expression) => Average<double>(expression);
        public double? Average(Expression<Func<T, int?>> expression) => Average<double?>(expression);
        public double Average(Expression<Func<T, double>> expression) => Average<double>(expression);
        public double? Average(Expression<Func<T, double?>> expression) => Average<double?>(expression);
        public double Average(Expression<Func<T, long>> expression) => Average<double>(expression);
        public double? Average(Expression<Func<T, long?>> expression) => Average<double?>(expression);
        public decimal Average(Expression<Func<T, decimal>> expression) => Average<decimal>(expression);
        public decimal? Average(Expression<Func<T, decimal?>> expression) => Average<decimal?>(expression);

        public double Average(Expression<Func<T, int>> expression, Expression<Func<T, bool>> filter) => Average<double>(expression, filter);
        public double? Average(Expression<Func<T, int?>> expression, Expression<Func<T, bool>> filter) => Average<double?>(expression, filter);
        public double Average(Expression<Func<T, double>> expression, Expression<Func<T, bool>> filter) => Average<double>(expression, filter);
        public double? Average(Expression<Func<T, double?>> expression, Expression<Func<T, bool>> filter) => Average<double?>(expression, filter);
        public double Average(Expression<Func<T, long>> expression, Expression<Func<T, bool>> filter) => Average<double>(expression, filter);
        public double? Average(Expression<Func<T, long?>> expression, Expression<Func<T, bool>> filter) => Average<double?>(expression, filter);
        public decimal Average(Expression<Func<T, decimal>> expression, Expression<Func<T, bool>> filter) => Average<decimal>(expression, filter);
        public decimal? Average(Expression<Func<T, decimal?>> expression, Expression<Func<T, bool>> filter) => Average<decimal?>(expression, filter);

        public TScalar Average<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Average, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression), skip: _skip, take: _take));
        }

        public bool Any()
        {
            return _repository.GetAggregate<bool>(Aggregate.Any, _repository.CreateQuerySpec(_filter, skip: _skip, take: _take));
        }


        public bool Any(Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<bool>(Aggregate.Any, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), skip: _skip, take: _take));
        }

        public T ElementAt(int index)
        {
            return Skip(index).Take(1).FirstOrDefault();
        }

        public IObjectEvents<T> Events => _repository.Events<T>();

        public Task<List<T>> ToListAsync() => Task.Run(this.ToList);
        public Task<T[]> ToArrayAsync() => Task.Run(this.ToArray);

        public Task<T> FirstAsync() => Task.Run(First);
        public Task<T> FirstAsync(Expression<Func<T, bool>> filter) => Task.Run(() => First(filter));

        public Task<T> FirstOrDefaultAsync() => Task.Run(FirstOrDefault);
        public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> filter) => Task.Run(() => FirstOrDefault(filter));
        
        public Task<bool> AnyAsync(Expression<Func<T, bool>> filter) => Task.Run(() => Any(filter));
        public Task<bool> AllAsync(Expression<Func<T, bool>> filter) => Task.Run(() => All(filter));
        public Task<bool> AnyAsync() => Task.Run(Any);
        
        public Task<long> CountAsync() => Task.Run(Count);
        public Task<long> CountAsync(Expression<Func<T, bool>> filter) => Task.Run(() => Count(filter));
        
        public Task<TScalar> MaxAsync<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Max(expression, filter));
        public Task<TScalar> MinAsync<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Min(expression, filter));
        public Task<TScalar> SumAsync<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Sum(expression, filter));

        public Task<TScalar> MaxAsync<TScalar>(Expression<Func<T, TScalar>> expression) => Task.Run(() => Max(expression));
        public Task<TScalar> MinAsync<TScalar>(Expression<Func<T, TScalar>> expression) => Task.Run(() => Min(expression));
        public Task<TScalar> SumAsync<TScalar>(Expression<Func<T, TScalar>> expression) => Task.Run(() => Sum(expression));

        public Task<double> AverageAsync(Expression<Func<T, int>> expression) => Task.Run(() => Average(expression));
        public Task<double?> AverageAsync(Expression<Func<T, int?>> expression) => Task.Run(() => Average(expression));
        public Task<double> AverageAsync(Expression<Func<T, double>> expression) => Task.Run(() => Average(expression));
        public Task<double?> AverageAsync(Expression<Func<T, double?>> expression) => Task.Run(() => Average(expression));
        public Task<double> AverageAsync(Expression<Func<T, long>> expression) => Task.Run(() => Average(expression));
        public Task<double?> AverageAsync(Expression<Func<T, long?>> expression) => Task.Run(() => Average(expression));
        public Task<decimal> AverageAsync(Expression<Func<T, decimal>> expression) => Task.Run(() => Average(expression));
        public Task<decimal?> AverageAsync(Expression<Func<T, decimal?>> expression) => Task.Run(() => Average(expression));

        public Task<double> AverageAsync(Expression<Func<T, int>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));
        public Task<double?> AverageAsync(Expression<Func<T, int?>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));
        public Task<double> AverageAsync(Expression<Func<T, double>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));
        public Task<double?> AverageAsync(Expression<Func<T, double?>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));
        public Task<double> AverageAsync(Expression<Func<T, long>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));
        public Task<double?> AverageAsync(Expression<Func<T, long?>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));
        public Task<decimal> AverageAsync(Expression<Func<T, decimal>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));
        public Task<decimal?> AverageAsync(Expression<Func<T, decimal?>> expression, Expression<Func<T, bool>> filter) => Task.Run(() => Average(expression, filter));

        public Task PurgeAsync() => Task.Run(Purge);

        public Task<T> ReadAsync(object key, params Expression<Func<T, object>>[] relationsToLoad) => Task.Run(() => Read(key, relationsToLoad));
        public Task<T> ReadAsync(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad) => Task.Run(() => Read(condition, relationsToLoad));
        public Task<T> LoadAsync(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad) => Task.Run(() => Load(obj, key, relationsToLoad));

        public Task<bool> SaveAsync(T obj, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => Save(obj, relationsToSave));
        public Task<bool> UpdateAsync(T obj, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => Update(obj, relationsToSave));
        public Task<bool> InsertAsync(T obj, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => Insert(obj, relationsToSave));
        public Task<bool> InsertAsync(T obj, bool? deferSave, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => Insert(obj, deferSave, relationsToSave));
        public Task<bool> AddAsync(T obj) => Task.Run(() => Add(obj));
        public Task<bool> InsertOrUpdateAsync(T obj, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => InsertOrUpdate(obj, relationsToSave));
        public Task<bool> DeleteAsync(T obj) => Task.Run(() => Delete(obj));
        public Task<bool> SaveAsync(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => Save(objects, relationsToSave));
        public Task<bool> InsertOrUpdateAsync(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => InsertOrUpdate(objects, relationsToSave));
        public Task<bool> InsertAsync(IEnumerable<T> objects, bool? deferSave, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => Insert(objects, deferSave, relationsToSave));
        public Task<bool> UpdateAsync(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave) => Task.Run(() => Update(objects, relationsToSave));
        public Task<bool> DeleteAsync(IEnumerable<T> objects) => Task.Run(() => Delete(objects));
        public Task<bool> DeleteAllAsync() => Task.Run(DeleteAll);
        public Task<bool> DeleteAsync(Expression<Func<T, bool>> filter) => Task.Run(() => Delete(filter));
    }
}