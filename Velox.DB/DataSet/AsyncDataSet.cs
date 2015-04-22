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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Velox.DB
{
    internal class AsyncDataSet<T> : IAsyncDataSet<T>
    {
        private readonly IDataSet<T> _dataSet;

        public AsyncDataSet(IDataSet<T> dataSet)
        {
            _dataSet = dataSet;
        }

        public IAsyncDataSet<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            return new AsyncDataSet<T>(_dataSet.Where(whereExpression));
        }

        public IAsyncDataSet<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new AsyncDataSet<T>(_dataSet.OrderBy(keySelector));
        }

        public IAsyncDataSet<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new AsyncDataSet<T>(_dataSet.OrderByDescending(keySelector));
        }

        public IAsyncDataSet<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new AsyncDataSet<T>(_dataSet.ThenBy(keySelector));
        }

        public IAsyncDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new AsyncDataSet<T>(_dataSet.ThenByDescending(keySelector));
        }

        public IAsyncDataSet<T> OrderBy(QueryExpression expression, SortOrder sortOrder)
        {
            return new AsyncDataSet<T>(_dataSet.OrderBy(expression, sortOrder));
        }

        public IAsyncDataSet<T> WithRelations(params Expression<Func<T, object>>[] relationsToLoad)
        {
            return new AsyncDataSet<T>(_dataSet.WithRelations(relationsToLoad));
        }

        public IAsyncDataSet<T> Where(QueryExpression filterExpression)
        {
            return new AsyncDataSet<T>(_dataSet.Where(filterExpression));
        }

        public IAsyncDataSet<T> Skip(int n)
        {
            return new AsyncDataSet<T>(_dataSet.Skip(n));
        }

        public IAsyncDataSet<T> Take(int n)
        {
            return new AsyncDataSet<T>(_dataSet.Take(n));
        }

        public IDataSet<T> Sync()
        {
            return _dataSet;
        }

        public Task<T> Read(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return Task.Factory.StartNew( () => _dataSet.Read(key, relationsToLoad));
        }

        public Task<T> Read(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return Task.Factory.StartNew(() => _dataSet.Read(condition, relationsToLoad));
        }

        public Task<T> Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return Task.Factory.StartNew( () => _dataSet.Load(obj, key, relationsToLoad));
        }

        public Task<bool> InsertOrUpdate(T obj, bool saveRelations = false)
        {
            return Task.Factory.StartNew( () => _dataSet.InsertOrUpdate(obj, saveRelations));
        }

        public Task<bool> Insert(T obj, bool saveRelations = false)
        {
            return Task.Factory.StartNew( () => _dataSet.Insert(obj, saveRelations));
        }

        public Task<bool> Update(T obj, bool saveRelations = false)
        {
            return Task.Factory.StartNew(() => _dataSet.Update(obj, saveRelations));
        }

        public Task<bool> Delete(T obj)
        {
            return Task.Factory.StartNew( () => _dataSet.Delete(obj));
        }

        public Task<bool> DeleteAll()
        {
            return Task.Factory.StartNew(() => _dataSet.DeleteAll());
        }

        public Task<bool> Delete(Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew( () => _dataSet.Delete(filter));
        }

        public Task<bool> Delete(QueryExpression filterExpression)
        {
            return Task.Factory.StartNew(() => _dataSet.Delete(filterExpression));
        }

        public Task<T> First()
        {
            return Task.Factory.StartNew(() => _dataSet.First());
        }

        public Task<T> First(Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.First(filter));
        }

        public Task<T> FirstOrDefault()
        {
            return Task.Factory.StartNew(() => _dataSet.FirstOrDefault());
        }

        public Task<T> FirstOrDefault(Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.FirstOrDefault(filter));
        }

        public Task<long> Count()
        {
            return Task.Factory.StartNew(() => _dataSet.Count());
        }

        public Task<long> Count(Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.Count(filter));
        }

        public Task<TScalar> Max<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.Max(expression, filter));
        }

        public Task<TScalar> Min<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.Min(expression, filter));
        }

        public Task<TScalar> Sum<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.Sum(expression, filter));
        }

        public Task<bool> All(Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.All(filter));
        }

        public Task<TScalar> Max<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return Task.Factory.StartNew(() => _dataSet.Max(expression));
        }

        public Task<TScalar> Min<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return Task.Factory.StartNew(() => _dataSet.Min(expression));
        }

        public Task<TScalar> Sum<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return Task.Factory.StartNew(() => _dataSet.Sum(expression));
        }

        public Task<TScalar> Average<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return Task.Factory.StartNew(() => _dataSet.Average(expression));
        }

        public Task<bool> Any()
        {
            return Task.Factory.StartNew(() => _dataSet.Any());
        }

        public Task<TScalar> Average<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.Average(expression, filter));
        }

        public Task<bool> Any(Expression<Func<T, bool>> filter)
        {
            return Task.Factory.StartNew(() => _dataSet.Any(filter));
        }

        public Task<T> ElementAt(int index)
        {
            return Task.Factory.StartNew(() => _dataSet.ElementAt(index));
        }

        public Task<List<T>> ToList()
        {
            return Task.Factory.StartNew(() => _dataSet.ToList());
        }

        public Task<T[]> ToArray()
        {
            return Task.Factory.StartNew(() => _dataSet.ToArray());
        }

        public Task<Dictionary<TKey, T>> ToDictionary<TKey>(Func<T, TKey> keySelector)
        {
            return Task.Factory.StartNew(() => _dataSet.ToDictionary(keySelector));
        }

        public Task<Dictionary<TKey, T>> ToDictionary<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Factory.StartNew(() => _dataSet.ToDictionary(keySelector, comparer));
        }

        public Task<Dictionary<TKey, TValue>> ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            return Task.Factory.StartNew(() => _dataSet.ToDictionary(keySelector, valueSelector));
        }

        public Task<Dictionary<TKey, TValue>> ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Factory.StartNew(() => _dataSet.ToDictionary(keySelector, valueSelector, comparer));
        }

        public Task<ILookup<TKey, T>> ToLookup<TKey>(Func<T, TKey> keySelector)
        {
            return Task.Factory.StartNew(() => _dataSet.ToLookup(keySelector));
        }

        public Task<ILookup<TKey, T>> ToLookup<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Factory.StartNew(() => _dataSet.ToLookup(keySelector, comparer));
        }

        public Task<ILookup<TKey, TValue>> ToLookup<TKey,TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            return Task.Factory.StartNew(() => _dataSet.ToLookup(keySelector, valueSelector));
        }

        public Task<ILookup<TKey, TValue>> ToLookup<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, IEqualityComparer<TKey> comparer)
        {
            return Task.Factory.StartNew(() => _dataSet.ToLookup(keySelector, valueSelector, comparer));
        }

    }
}