using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Iridium.DB
{
    public class UnboundDataSet<T> : IDataSet<T>
    {
        private readonly IEnumerable<T> _list;

        public UnboundDataSet(IEnumerable<T> list)
        {
            _list = list;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IAsyncDataSet<T> Async()
        {
            throw new NotSupportedException();
        }

        public IDataSet<T> Where(Expression<Func<T, bool>> whereExpression)
        {
            return new UnboundDataSet<T>(_list.Where(whereExpression.Compile()));
        }

        public IDataSet<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new UnboundDataSet<T>(_list.OrderBy(keySelector.Compile()));
        }

        public IDataSet<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            return new UnboundDataSet<T>(_list.OrderByDescending(keySelector.Compile()));
        }

        public IDataSet<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var orderedList = _list as IOrderedEnumerable<T>;

            if (orderedList == null)
                throw new ArgumentException("ThenBy() out of sequence");

            return new UnboundDataSet<T>(orderedList.ThenBy(keySelector.Compile()));
        }

        public IDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var orderedList = _list as IOrderedEnumerable<T>;

            if (orderedList == null)
                throw new ArgumentException("ThenByDescending() out of sequence");

            return new UnboundDataSet<T>(orderedList.ThenByDescending(keySelector.Compile()));
        }

        public IDataSet<T> OrderBy(QueryExpression expression, SortOrder sortOrder = SortOrder.Ascending)
        {
            throw new NotSupportedException();
        }

        public IDataSet<T> Skip(int n)
        {
            return new UnboundDataSet<T>(_list.Skip(n));
        }

        public IDataSet<T> Take(int n)
        {
            return new UnboundDataSet<T>(_list.Take(n));
        }

        public T First()
        {
            return _list.First();
        }

        public T First(Expression<Func<T, bool>> filter)
        {
            return _list.First(filter.Compile());
        }

        public T FirstOrDefault()
        {
            return _list.FirstOrDefault();
        }

        public T FirstOrDefault(Expression<Func<T, bool>> filter)
        {
            return _list.FirstOrDefault(filter.Compile());
        }

        public bool Any(Expression<Func<T, bool>> filter)
        {
            return _list.Any(filter.Compile());
        }

        public bool All(Expression<Func<T, bool>> filter)
        {
            return _list.All(filter.Compile());
        }

        public bool Any()
        {
            return _list.Any();
        }

        public long Count()
        {
            return _list.Count();
        }

        public long Count(Expression<Func<T, bool>> filter)
        {
            return _list.Count(filter.Compile());
        }

        public TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            throw new NotSupportedException();
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            throw new NotSupportedException();
        }

        public TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            throw new NotSupportedException();
        }

        public TScalar Average<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            throw new NotSupportedException();
        }

        public TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            throw new NotSupportedException();
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            throw new NotSupportedException();
        }

        public TScalar Sum<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            throw new NotSupportedException();
        }

        public TScalar Average<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            throw new NotSupportedException();
        }

        public T ElementAt(int index)
        {
            return _list.ElementAt(index);
        }

        public IDataSet<T> Where(QueryExpression filterExpression)
        {
            throw new NotSupportedException();
        }

        public IDataSet<T> WithRelations(params Expression<Func<T, object>>[] relationsToLoad)
        {
            throw new NotSupportedException();
        }

        public void Purge()
        {
            throw new NotSupportedException();
        }

        public T Read(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            throw new NotSupportedException();
        }

        public T Read(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad)
        {
            throw new NotSupportedException();
        }

        public T Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            throw new NotSupportedException();
        }

        public bool Save(T obj, bool saveRelations = false, bool? create = null)
        {
            throw new NotSupportedException();
        }

        public bool Update(T obj, bool saveRelations = false)
        {
            throw new NotSupportedException();
        }

        public bool Insert(T obj, bool saveRelations = false)
        {
            throw new NotSupportedException();
        }

        public bool InsertOrUpdate(T obj, bool saveRelations)
        {
            throw new NotSupportedException();
        }

        public bool Delete(T obj)
        {
            throw new NotSupportedException();
        }

        public bool Save(IEnumerable<T> objects, bool saveRelations = false, bool? create = null)
        {
            throw new NotSupportedException();
        }

        public bool InsertOrUpdate(IEnumerable<T> objects, bool saveRelations = false)
        {
            throw new NotSupportedException();
        }

        public bool Insert(IEnumerable<T> objects, bool saveRelations = false)
        {
            throw new NotSupportedException();
        }

        public bool Update(IEnumerable<T> objects, bool saveRelations = false)
        {
            throw new NotSupportedException();
        }

        public bool Delete(IEnumerable<T> objects)
        {
            throw new NotSupportedException();
        }

        public bool DeleteAll()
        {
            throw new NotSupportedException();
        }

        public bool Delete(Expression<Func<T, bool>> filter)
        {
            throw new NotSupportedException();
        }

        public bool Delete(QueryExpression filterExpression)
        {
            throw new NotSupportedException();
        }

        public IObjectEvents<T> Events
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}