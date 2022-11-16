using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Iridium.DB
{
    public interface IStorageContext : IDisposable
    {
        IDataProvider DataProvider { get; }
        void LoadRelations<T>(T obj, params Expression<Func<T, object>>[] relationsToLoad);
        void LoadRelations<T>(T obj, IEnumerable<Expression<Func<T, object>>> relationsToLoad);
        void LoadRelations<T>(IEnumerable<T> list, params Expression<Func<T, object>>[] relationsToLoad);
        void LoadRelations<T>(IEnumerable<T> list, IEnumerable<Expression<Func<T, object>>> relationsToLoad);
        TProp LoadRelation<TProp>(Expression<Func<TProp>> propExpression);
        void LoadRelations(params Expression<Func<object>>[] relationsToLoad);
        IDataSet<T> DataSet<T>();
        IDataSet<TBase> DataSet<T, TBase>() where T:TBase;
        IDataSet<T> DataSet<T>(Type entityType);
        object DataSet(Type entityType);
        Transaction CreateTransaction(IsolationLevel isolationLevel = IsolationLevel.Serializable, bool commitOnDispose = false);
        bool RunTransaction(Func<bool> block, IsolationLevel isolationLevel = IsolationLevel.Serializable);
        void CreateTable<T>(bool recreateTable = false, bool recreateIndexes = false);
        Task CreateTableAsync<T>(bool recreateTable = false, bool recreateIndexes = false);
        T Read<T>(object key, params Expression<Func<T, object>>[] relationsToLoad);
        Task<T> ReadAsync<T>(object key, params Expression<Func<T, object>>[] relationsToLoad);
        T Read<T>(Expression<Func<T,bool>> condition,  params Expression<Func<T, object>>[] relationsToLoad);
        Task<T> ReadAsync<T>(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad);
        T Load<T>(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad);
        Task<T> LoadAsync<T>(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad);
        bool Save<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> SaveAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool InsertOrUpdate<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> InsertOrUpdateAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Update<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        Task<bool> UpdateAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Insert<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool InsertDeep<T,U>(T obj, Expression<Func<T, IEnumerable<U>>> rel1, Expression<Func<U, object>> rel2);
        Task<bool> InsertAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave);
        bool Delete<T>(T obj);
        Task<bool> DeleteAsync<T>(T obj);
        bool Delete<T>(IEnumerable<T> objects);
        Task<bool> DeleteAsync<T>(IEnumerable<T> objects);
        bool Delete<T>(Expression<Func<T, bool>> condition);
        Task<bool> DeleteAsync<T>(Expression<Func<T, bool>> condition);
        void UpdateOrCreateOnlyRecord<T>(T rec);
        Task UpdateOrCreateOnlyRecordAsync<T>(T rec);
        IAsyncDataSet<T> AsyncDataSet<T>();
        Task LoadRelationsAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToLoad);
        Task LoadRelationsAsync<T>(T obj, IEnumerable<Expression<Func<T, object>>> relationsToLoad);
        Task LoadRelationsAsync<T>(IEnumerable<T> list, params Expression<Func<T, object>>[] relationsToLoad);
        Task LoadRelationsAsync<T>(IEnumerable<T> list, IEnumerable<Expression<Func<T, object>>> relationsToLoad);
        Task LoadRelationsAsync(params Expression<Func<object>>[] relationsToLoad);
    }
}