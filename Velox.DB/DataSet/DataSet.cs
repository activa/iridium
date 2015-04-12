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
    internal sealed class DataSet<T> : IDataSet<T>
    {
        private readonly Repository<T> _repository;
        private readonly FilterSpec _filter;
        private readonly SortOrderSpec _sortOrder;
        private readonly List<Expression<Func<T, object>>> _relationsToLoad;
        private int? _skip;
        private int? _take;

        public DataSet(Repository repository)
        {
            _repository = (Repository<T>) repository;
        }
        
        [Preserve]
        public DataSet(Repository repository, FilterSpec filter)
        {
            _repository = (Repository<T>)repository;
            _filter = filter;
        }

        private DataSet(DataSet<T> parent, FilterSpec newFilterSpec = null, SortOrderSpec newSortSpec = null, IEnumerable<Expression<Func<T,object>>> additionalRelations = null)
        {
            _repository = parent._repository;

            _skip = parent._skip;
            _take = parent._take;

            _filter = newFilterSpec ?? parent._filter;
            _sortOrder = newSortSpec ?? parent._sortOrder;

            if (parent._relationsToLoad != null)
                _relationsToLoad = new List<Expression<Func<T, object>>>(parent._relationsToLoad);

            if (additionalRelations != null)
            {
                if (_relationsToLoad == null)
                    _relationsToLoad = new List<Expression<Func<T, object>>>(additionalRelations);
                else
                    _relationsToLoad.AddRange(additionalRelations);
            }
        }

        public Repository<T> Repository
        {
            get { return _repository; }
        }

        public IDataSet<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            return new DataSet<T>(this, newFilterSpec: new FilterSpec(whereExpression, _filter));
        }

        public IDataSet<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T>(this, newSortSpec: new SortOrderSpec(keySelector,SortOrder.Ascending, _sortOrder));
        }

        public IDataSet<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T>(this, newSortSpec: new SortOrderSpec(keySelector, SortOrder.Descending, _sortOrder));
        }

        public IDataSet<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T>(this, newSortSpec: new SortOrderSpec(keySelector, SortOrder.Ascending, _sortOrder));
        }

        public IDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new DataSet<T>(this, newSortSpec: new SortOrderSpec(keySelector, SortOrder.Descending, _sortOrder));
        }

        public IDataSet<T> OrderBy(QueryExpression expression, SortOrder sortOrder)
        {
            return new DataSet<T>(this, newSortSpec: new SortOrderSpec(expression, sortOrder, _sortOrder));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _repository.List(_repository.CreateQuerySpec(_filter, sortSpec: _sortOrder, skip:_skip, take:_take), _relationsToLoad).GetEnumerator();
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

        public T Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return _repository.Load(obj, key, relationsToLoad);
        }

        public bool Save(T obj, bool saveRelations = false, bool? create = null)
        {
            return _repository.Save(obj, saveRelations, create);
        }

        public bool Create(T obj, bool saveRelations = false)
        {
            return _repository.Save(obj, saveRelations, true);
        }

        public bool Delete(T obj)
        {
            return _repository.Delete(obj);
        }

        public bool Delete(Expression<Func<T, bool>> filter)
        {
            return _repository.Delete(_repository.CreateQuerySpec(new FilterSpec(filter)));
        }

        public bool Delete(QueryExpression filterExpression)
        {
            return _repository.Delete(_repository.CreateQuerySpec(new FilterSpec(filterExpression)));
        }

        public IDataSet<T> WithRelations(params Expression<Func<T, object>>[] relationsToLoad)
        {
            return new DataSet<T>(this, additionalRelations: relationsToLoad);
        }

        public IDataSet<T> Where(QueryExpression filterExpression)
        {
            return new DataSet<T>(this, newFilterSpec: new FilterSpec(filterExpression));
        }

        public IDataSet<T> Skip(int n)
        {
            n += _skip ?? 0;

            return new DataSet<T>(this) {_skip = n};
        }

        public IDataSet<T> Take(int n)
        {
            return new DataSet<T>(this) { _take = Math.Min(n,_take ?? int.MaxValue) };
        }

        public T First()
        {
            return _repository.List(_repository.CreateQuerySpec(_filter, sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad).First();
        }

        public T First(Expression<Func<T, bool>> filter)
        {
            return _repository.List(_repository.CreateQuerySpec(new FilterSpec(filter, _filter), sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad).First();
        }

        public T FirstOrDefault()
        {
            return _repository.List(_repository.CreateQuerySpec(_filter, sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad).FirstOrDefault();
        }

        public T FirstOrDefault(Expression<Func<T, bool>> filter)
        {
            return _repository.List(_repository.CreateQuerySpec(new FilterSpec(filter, _filter), sortSpec: _sortOrder, skip: _skip, take: 1), _relationsToLoad).FirstOrDefault();
        }

        public long Count()
        {
            return _repository.GetAggregate<long>(Aggregate.Count, _repository.CreateQuerySpec(_filter));
        }

        public long Count(Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<long>(Aggregate.Count, _repository.CreateQuerySpec(new FilterSpec(filter, _filter)));
        }

        public TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Max, _repository.CreateQuerySpec(new FilterSpec(filter, _filter),new ScalarSpec(expression)));
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Min, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), new ScalarSpec(expression)));
        }

        public TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Sum, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), new ScalarSpec(expression)));
        }

        public bool All(Expression<Func<T, bool>> filter)
        {
            return Count(Expression.Lambda<Func<T, bool>>(Expression.Not(filter.Body), filter.Parameters[0])) == 0;
        }

        public TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Max, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression)));
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Min, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression)));
        }

        public TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Sum, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression)));
        }

        public TScalar Average<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Average, _repository.CreateQuerySpec(_filter, new ScalarSpec(expression)));
        }

        public bool Any()
        {
            return _repository.GetAggregate<bool>(Aggregate.Any, _repository.CreateQuerySpec(_filter));
        }

        public TScalar Average<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<TScalar>(Aggregate.Average, _repository.CreateQuerySpec(new FilterSpec(filter, _filter), new ScalarSpec(expression)));
        }

        public bool Any(Expression<Func<T, bool>> filter)
        {
            return _repository.GetAggregate<bool>(Aggregate.Any, _repository.CreateQuerySpec(new FilterSpec(filter, _filter)));
        }

        public T ElementAt(int index)
        {
            return Skip(index).Take(1).FirstOrDefault();
        }
    }

}