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
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Velox.DB
{
    public interface IAsyncDataSet<T>
    {
        Task<T> Read(object key, params Expression<Func<T, object>>[] relationsToLoad);
        Task<T> Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad);
        Task<bool> Save(T obj, bool saveRelations = false, bool? create = null);
        Task<bool> Create(T obj, bool saveRelations = false);
        Task<bool> Delete(T obj);
        Task<bool> Delete(Expression<Func<T, bool>> filter);
        Task<bool> Delete(QueryExpression filterExpression);
        Task<T> First();
        Task<T> First(Expression<Func<T, bool>> filter);
        Task<T> FirstOrDefault();
        Task<T> FirstOrDefault(Expression<Func<T, bool>> filter);
        Task<long> Count();
        Task<long> Count(Expression<Func<T, bool>> filter);
        Task<TScalar> Max<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        Task<TScalar> Min<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        Task<TScalar> Sum<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        Task<bool> All(Expression<Func<T, bool>> filter);
        Task<TScalar> Max<TScalar>(Expression<Func<T, TScalar>> expression);
        Task<TScalar> Min<TScalar>(Expression<Func<T, TScalar>> expression);
        Task<TScalar> Sum<TScalar>(Expression<Func<T, TScalar>> expression);
        Task<TScalar> Average<TScalar>(Expression<Func<T, TScalar>> expression);
        Task<bool> Any();
        Task<TScalar> Average<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        Task<bool> Any(Expression<Func<T, bool>> filter);
        Task<T> ElementAt(int index);
        Task<List<T>> ToList();
        IAsyncDataSet<T> Where(Expression<Func<T, bool>> whereExpression);
        IAsyncDataSet<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
        IAsyncDataSet<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
        IAsyncDataSet<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
        IAsyncDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
        IAsyncDataSet<T> OrderBy(QueryExpression expression, SortOrder sortOrder);
        IAsyncDataSet<T> WithRelations(params Expression<Func<T, object>>[] relationsToLoad);
        IAsyncDataSet<T> Where(QueryExpression filterExpression);
        IAsyncDataSet<T> Skip(int n);
        IAsyncDataSet<T> Take(int n);

        IDataSet<T> Sync();
    }
}