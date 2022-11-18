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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Iridium.DB
{
    public interface IDataSet<T> : IEnumerable<T>
    {
        IAsyncDataSet<T> Async();

        // Standard LINQ methods
        IDataSet<T> Where(Expression<Func<T, bool>> whereExpression);
        IDataSet<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
        IDataSet<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

        IDataSet<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
        IDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
        IDataSet<T> OrderBy(QueryExpression expression, SortOrder sortOrder = SortOrder.Ascending);
        IDataSet<T> Skip(int n);
        IDataSet<T> Take(int n);

        IProjectedDataSet<TResult,T> Select<TResult>(Expression<Func<T, TResult>> selector);
        IDataSet<T> Distinct();

        T First();
        T First(Expression<Func<T, bool>> filter);
        T FirstOrDefault();
        T FirstOrDefault(Expression<Func<T, bool>> filter);

        bool Any(Expression<Func<T, bool>> filter);
        bool All(Expression<Func<T, bool>> filter);
        bool Any();

        long Count();
        long Count(Expression<Func<T, bool>> filter);

        TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);

        TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression);
        TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression);
        TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression);

        double Average(Expression<Func<T, int>> expression);
        double? Average(Expression<Func<T, int?>> expression);
        double Average(Expression<Func<T, double>> expression);
        double? Average(Expression<Func<T, double?>> expression);
        double Average(Expression<Func<T, long>> expression);
        double? Average(Expression<Func<T, long?>> expression);
        decimal Average(Expression<Func<T, decimal>> expression);
        decimal? Average(Expression<Func<T, decimal?>> expression);

        double Average(Expression<Func<T, int>> expression, Expression<Func<T, bool>> filter);
        double? Average(Expression<Func<T, int?>> expression, Expression<Func<T, bool>> filter);
        double Average(Expression<Func<T, double>> expression, Expression<Func<T, bool>> filter);
        double? Average(Expression<Func<T, double?>> expression, Expression<Func<T, bool>> filter);
        double Average(Expression<Func<T, long>> expression, Expression<Func<T, bool>> filter);
        double? Average(Expression<Func<T, long?>> expression, Expression<Func<T, bool>> filter);
        decimal Average(Expression<Func<T, decimal>> expression, Expression<Func<T, bool>> filter);
        decimal? Average(Expression<Func<T, decimal?>> expression, Expression<Func<T, bool>> filter);

        T ElementAt(int index);

        IDataSet<T> Where(QueryExpression filterExpression);
        IDataSet<T> WithRelations(params Expression<Func<T, object>>[] relationsToLoad);
        IDataSet<T> WithAction(Action<T> action);
        IDataSet<T> WithActions(params Action<T>[] actions);

        void Purge();

        T Read(object key, params Expression<Func<T, object>>[] relationsToLoad);
        T Read(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad);
        T Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad);

        bool Save(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Update(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Insert(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Insert(T obj, bool? deferSave, params Expression<Func<T,object>>[] relationsToSave);
        bool Add(T obj);

        bool InsertOrUpdate(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Delete(T obj);

        bool Save(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave);
        bool InsertOrUpdate(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave);
        bool Insert(IEnumerable<T> objects, bool? deferSave, params Expression<Func<T, object>>[] relationsToSave);
        bool Update(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave);
        bool Delete(IEnumerable<T> objects);

        bool DeleteAll();
        bool Delete(Expression<Func<T, bool>> filter);

        IObjectEvents<T> Events { get; }

        // Async methods

        Task<T> FirstAsync();
        Task<T> FirstAsync(Expression<Func<T, bool>> filter);
        Task<T> FirstOrDefaultAsync();
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> filter);

        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);
        Task<bool> AllAsync(Expression<Func<T, bool>> filter);
        Task<bool> AnyAsync();

        Task<long> CountAsync();
        Task<long> CountAsync(Expression<Func<T, bool>> filter);

        Task<TScalar> MaxAsync<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        Task<TScalar> MinAsync<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);
        Task<TScalar> SumAsync<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);

        Task<TScalar> MaxAsync<TScalar>(Expression<Func<T, TScalar>> expression);
        Task<TScalar> MinAsync<TScalar>(Expression<Func<T, TScalar>> expression);
        Task<TScalar> SumAsync<TScalar>(Expression<Func<T, TScalar>> expression);

        Task<double> AverageAsync(Expression<Func<T, int>> expression);
        Task<double?> AverageAsync(Expression<Func<T, int?>> expression);
        Task<double> AverageAsync(Expression<Func<T, double>> expression);
        Task<double?> AverageAsync(Expression<Func<T, double?>> expression);
        Task<double> AverageAsync(Expression<Func<T, long>> expression);
        Task<double?> AverageAsync(Expression<Func<T, long?>> expression);
        Task<decimal> AverageAsync(Expression<Func<T, decimal>> expression);
        Task<decimal?> AverageAsync(Expression<Func<T, decimal?>> expression);

        Task<double> AverageAsync(Expression<Func<T, int>> expression, Expression<Func<T, bool>> filter);
        Task<double?> AverageAsync(Expression<Func<T, int?>> expression, Expression<Func<T, bool>> filter);
        Task<double> AverageAsync(Expression<Func<T, double>> expression, Expression<Func<T, bool>> filter);
        Task<double?> AverageAsync(Expression<Func<T, double?>> expression, Expression<Func<T, bool>> filter);
        Task<double> AverageAsync(Expression<Func<T, long>> expression, Expression<Func<T, bool>> filter);
        Task<double?> AverageAsync(Expression<Func<T, long?>> expression, Expression<Func<T, bool>> filter);
        Task<decimal> AverageAsync(Expression<Func<T, decimal>> expression, Expression<Func<T, bool>> filter);
        Task<decimal?> AverageAsync(Expression<Func<T, decimal?>> expression, Expression<Func<T, bool>> filter);

        Task PurgeAsync();

        Task<T> ReadAsync(object key, params Expression<Func<T, object>>[] relationsToLoad);
        Task<T> ReadAsync(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad);
        Task<T> LoadAsync(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad);

        Task<bool> SaveAsync(T obj, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> UpdateAsync(T obj, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> InsertAsync(T obj, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> InsertAsync(T obj, bool? deferSave, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> AddAsync(T obj);

        Task<bool> InsertOrUpdateAsync(T obj, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> DeleteAsync(T obj);

        Task<bool> SaveAsync(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> InsertOrUpdateAsync(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> InsertAsync(IEnumerable<T> objects, bool? deferSave, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> UpdateAsync(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> DeleteAsync(IEnumerable<T> objects);

        Task<bool> DeleteAllAsync();
        Task<bool> DeleteAsync(Expression<Func<T, bool>> filter);



    }
}