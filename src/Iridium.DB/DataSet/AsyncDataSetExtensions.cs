using System.Threading.Tasks;

namespace Iridium.DB
{
    public static class AsyncDataSetExtensions
    {
        public static Task<T> FirstAsync<T>(this IDataSet<T> dataSet) => dataSet.Async().First();

        /*
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

        void Purge();

        T Read(object key, params Expression<Func<T, object>>[] relationsToLoad);
        T Read(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad);
        T Load(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad);

        bool Save(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Update(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Insert(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Insert(T obj, bool? deferSave, params Expression<Func<T, object>>[] relationsToSave);
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
        */


    }
}