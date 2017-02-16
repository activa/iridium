using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Iridium.DB
{
    public class UnboundDataSet<T> : IDataSet<T>, ICollection<T>
    {
        private readonly List<T> _list;

        public UnboundDataSet()
        {
            _list = new List<T>();
        }

        public UnboundDataSet(IEnumerable<T> list)
        {
            _list = new List<T>(list);
        }

        public bool IsBound => false;

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
            throw new NotSupportedException();
        }

        public IDataSet<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            throw new NotSupportedException();
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

        long IDataSet<T>.Count()
        {
            return _list.Count();
        }

        public long Count(Expression<Func<T, bool>> filter)
        {
            return _list.Count(filter.Compile());
        }

        public TScalar Max<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _list.Where(filter.Compile()).Max(expression.Compile());
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression, Expression<Func<T, bool>> filter)
        {
            return _list.Where(filter.Compile()).Min(expression.Compile());
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
            return _list.Max(expression.Compile());
        }

        public TScalar Min<TScalar>(Expression<Func<T, TScalar>> expression)
        {
            return _list.Min(expression.Compile());
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
            return _list[index];
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

        public bool Save(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            throw new NotSupportedException();
        }

        public bool Update(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            throw new NotSupportedException();
        }

        public bool Insert(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Insert(obj, deferSave: null, relationsToSave: relationsToSave);
        }

        public bool Insert(T obj, bool? deferSave = null, params Expression<Func<T, object>>[] relationsToSave)
        {
            if (relationsToSave.Length > 0)
                throw new NotSupportedException();

            _list.Add(obj);

            return true;
        }

        public bool InsertOrUpdate(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            throw new NotSupportedException();
        }

        public bool Delete(T obj)
        {
            return _list.Remove(obj);
        }

        public bool Save(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave)
        {
            throw new NotSupportedException();
        }

        public bool InsertOrUpdate(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave)
        {
            throw new NotSupportedException();
        }

        public bool Insert(IEnumerable<T> objects, bool? deferSave = null, params Expression<Func<T, object>>[] relationsToSave)
        {
            if (relationsToSave.Length > 0)
                throw new NotSupportedException();

            _list.AddRange(objects);

            return true;
        }

        public bool Update(IEnumerable<T> objects, params Expression<Func<T, object>>[] relationsToSave)
        {
            throw new NotSupportedException();
        }

        public bool Delete(IEnumerable<T> objects)
        {
            _list.RemoveAll(objects.Contains);

            return true;
        }

        public bool DeleteAll()
        {
            _list.Clear();

            return true;
        }

        public bool Delete(Expression<Func<T, bool>> filter)
        {
            _list.RemoveAll(obj => filter.Compile().Invoke(obj));

            return true;
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

        public void Add(T item) => _list.Add(item);

        public void Clear() => _list.Clear();

        public bool Contains(T item) => _list.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public bool Remove(T item) => _list.Remove(item);

        int ICollection<T>.Count => _list.Count;

        public bool IsReadOnly => false;

        public static implicit operator UnboundDataSet<T>(T[] items) => new UnboundDataSet<T>(items);
        public static implicit operator UnboundDataSet<T>(List<T> items) => new UnboundDataSet<T>(items);
    }
}