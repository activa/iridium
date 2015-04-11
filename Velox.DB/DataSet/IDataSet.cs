using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Velox.DB
{
    public interface IDataSet<T> : IEnumerable<T>
    {
        // Standard LINQ methods
        IDataSet<T> Where(Expression<Func<T, bool>> whereExpression);
        IDataSet<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
        IDataSet<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

        IDataSet<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
        IDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
        IDataSet<T> OrderBy(QueryExpression expression, SortOrder sortOrder = SortOrder.Ascending);
        IDataSet<T> Skip(int n);
        IDataSet<T> Take(int n);
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
        TScalar Average<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter);

        TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression);
        TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression);
        TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression);
        TScalar Average<TScalar>(Expression<Func<T, TScalar>> expression);

        T ElementAt(int index);

        // Specific VeloxDB methods

        IDataSet<T> Where(QueryExpression filterExpression);
        IDataSet<T> WithRelations(params Expression<Func<T, object>>[] relationsToLoad);

        void Purge();

        T Read(object key, params Expression<Func<T, object>>[] relationsToLoad);
        T Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad);
        bool Save(T obj, bool saveRelations = false, bool? create = null);
        bool Create(T obj, bool saveRelations = false);

        bool Delete(T obj);
        bool Delete(Expression<Func<T, bool>> filter);
        bool Delete(QueryExpression filterExpression);
    }
}