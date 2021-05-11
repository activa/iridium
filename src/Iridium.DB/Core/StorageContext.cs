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
using Iridium.Reflection;

namespace Iridium.DB
{
    public class StorageContext : IDisposable
    {
        private readonly SafeDictionary<Type, Repository> _repositories = new SafeDictionary<Type, Repository>();

        public IDataProvider DataProvider { get; private set; }

        public StorageContext(IDataProvider dataProvider)
        {
            DataProvider = dataProvider;

            foreach (var property in GetType().Inspector().GetFieldsAndProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldTypeInspector = property.Type.Inspector();

                if (!fieldTypeInspector.IsGenericType || property.Type.GetGenericTypeDefinition() != typeof (IDataSet<>))
                    continue;

                var objectType = fieldTypeInspector.GetGenericArguments()[0];
                var repository = (Repository) Activator.CreateInstance(typeof (Repository<>).MakeGenericType(objectType), this);
                var dataSet = Activator.CreateInstance(typeof (DataSet<,>).MakeGenericType(objectType, objectType), repository);

                _repositories[objectType] = repository;

                property.SetValue(this, dataSet);
            }

            GenerateRelations();

            Instance = this;
        }

        private static StorageContext _globalInstance;
        private static int _instanceCount;
        private static readonly object _staticLock = new object();

        public static StorageContext Instance
        {
            get
            {
                lock (_staticLock)
                {
                    if (_instanceCount > 1)
                        throw new Exception("Can't use global context when multiple contexts are used");

                    return _globalInstance;
                }
            }
            set
            {
                lock (_staticLock)
                {
                    if (_globalInstance != value)
                        if (value != null)
                            _instanceCount++;
                        else
                            _instanceCount--;

                    _globalInstance = value;
                }
            }
        }

        private void GenerateRelations()
        {
            lock (_repositories)
            {
                Repository repository;

                while ((repository = _repositories.Values.FirstOrDefault(rep => rep.Schema.Relations == null)) != null)
                    repository.Schema.UpdateRelations();

                foreach (var repo in _repositories.Values)
                    repo.Schema.UpdateReverseRelations();
            }
        }

        internal TableSchema GetSchema(Type objectType, bool autoCreate = true)
        {
            lock (_repositories)
            {
                var repository = _repositories[objectType];

                if (repository == null)
                {
                    if (!autoCreate)
                        return null;

                    _repositories[objectType] = (repository = (Repository) Activator.CreateInstance(typeof(Repository<>).MakeGenericType(objectType), this));

                    GenerateRelations();
                }

                return repository.Schema;
            }
        }

        private void _LoadRelations(object obj, IEnumerable<LambdaExpression> relationsToLoad/*, TableSchema parentSchema*/)
        {
            TableSchema parentSchema = GetSchema(obj.GetType());

            Ir.LoadRelations(obj, LambdaRelationFinder.FindRelations(relationsToLoad, parentSchema));
        }

        public void LoadRelations<T>(T obj, params Expression<Func<T, object>>[] relationsToLoad)
        {
            _LoadRelations(obj, relationsToLoad);
        }

        public void LoadRelations<T>(T obj, IEnumerable<Expression<Func<T, object>>> relationsToLoad)
        {
            _LoadRelations(obj, relationsToLoad);
        }

        public void LoadRelations<T>(IEnumerable<T> list, params Expression<Func<T, object>>[] relationsToLoad)
        {
            foreach (var obj in list)
            {
                _LoadRelations(obj, relationsToLoad);
            }
        }

        public void LoadRelations<T>(IEnumerable<T> list, IEnumerable<Expression<Func<T, object>>> relationsToLoad)
        {
            relationsToLoad = relationsToLoad.ToList();

            foreach (var obj in list)
            {
                _LoadRelations(obj, relationsToLoad);
            }
        }

        public TProp LoadRelation<TProp>(Expression<Func<TProp>> propExpression)
        {
            if (propExpression.Body is MemberExpression memberExpression)
            {
                var obj = Expression.Lambda(memberExpression.Expression).Compile().DynamicInvoke();

                _LoadRelations(obj, new[] { propExpression });

                return propExpression.Compile().Invoke();
            }

            throw new ArgumentException("lambda is not a property access expression",nameof(propExpression));
        }

        public void LoadRelations(params Expression<Func<object>>[] relationsToLoad)
        {
            foreach (var relation in relationsToLoad)
            {
                if (relation.Body is MemberExpression memberExpression)
                {
                    _LoadRelations(Expression.Lambda(memberExpression.Expression).Compile().DynamicInvoke(), new[] { relation });
                }
            }
        }

        internal Repository<T> GetRepository<T>()
        {
            return (Repository<T>) GetRepository(typeof(T));
            // lock (_repositories)
            // {
            //     var repository = _repositories[typeof(T)];
            //
            //     if (repository == null)
            //     {
            //         _repositories[typeof(T)] = (repository = new Repository<T>(this));
            //
            //         GenerateRelations();
            //     }
            //
            //     return (Repository<T>) repository;
            // }
        }

        internal Repository GetRepository(Type entityType)
        {
            lock (_repositories)
            {
                var repository = _repositories[entityType];

                if (repository == null)
                {
                    _repositories[entityType] = (repository = (Repository) Activator.CreateInstance(typeof(Repository<>).MakeGenericType(entityType), this));

                    GenerateRelations();
                }

                return repository;
            }
        }

        public IDataSet<T> DataSet<T>()
        {
            return new DataSet<T,T>(GetRepository<T>());
        }

        public IDataSet<TBase> DataSet<T, TBase>() where T:TBase
        {
            return new DataSet<TBase,T>(GetRepository<T>());
        }

        public IDataSet<T> DataSet<T>(Type entityType)
        {
            return (IDataSet<T>) Activator.CreateInstance(typeof(DataSet<,>).MakeGenericType(typeof(T), entityType), GetRepository(entityType));
        }

        public object DataSet(Type entityType)
        {
            return Activator.CreateInstance(typeof(DataSet<,>).MakeGenericType(entityType, entityType), GetRepository(entityType));
        }

        public Transaction CreateTransaction(IsolationLevel isolationLevel = IsolationLevel.Serializable, bool commitOnDispose = false)
        {
            return new Transaction(this, isolationLevel, commitOnDispose);
        }

        public bool RunTransaction(Func<bool> block, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            using (var transaction = CreateTransaction(isolationLevel))
            {
                var success = block();

                if (success)
                    transaction.Commit();
                else
                    transaction.Rollback();

                return success;
            }
        }

        public void CreateTable<T>(bool recreateTable = false, bool recreateIndexes = false)
        {
            DataProvider.CreateOrUpdateTable(GetRepository<T>().Schema, recreateTable, recreateIndexes);
        }

        public Task CreateTableAsync<T>(bool recreateTable = false, bool recreateIndexes = false)
        {
            return Task.Run(() => CreateTable<T>(recreateTable, recreateIndexes));
        }

        public T Read<T>(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return GetRepository<T>().Read(key, relationsToLoad);
        }

        public Task<T> ReadAsync<T>(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return Task.Run(() => Read(key, relationsToLoad));
        }

        public T Read<T>(Expression<Func<T,bool>> condition,  params Expression<Func<T, object>>[] relationsToLoad)
        {
            return DataSet<T>().WithRelations(relationsToLoad).FirstOrDefault(condition);
        }

        public Task<T> ReadAsync<T>(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return AsyncDataSet<T>().WithRelations(relationsToLoad).FirstOrDefault(condition);
        }

        public T Load<T>(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return DataSet<T>().Load(obj, key, relationsToLoad);
        }

        public Task<T> LoadAsync<T>(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return Task.Run(() => Load(obj, key, relationsToLoad));
        }

        public bool Save<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Save(obj, relationsToSave);
        }

        public Task<bool> SaveAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Task.Run(() => Save(obj, relationsToSave));
        }

        public bool InsertOrUpdate<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Save(obj, relationsToSave);
        }

        public Task<bool> InsertOrUpdateAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Task.Run(() => InsertOrUpdate(obj, relationsToSave));
        }

        public bool Update<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Update(obj, relationsToSave);
        }

        public Task<bool> UpdateAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Task.Run(() => Update(obj, relationsToSave));
        }

        public bool Insert<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Insert(obj, relationsToSave);
        }

        public bool InsertDeep<T,U>(T obj, Expression<Func<T, IEnumerable<U>>> rel1, Expression<Func<U, object>> rel2)
        {
            return DataSet<T>().Insert(obj);
        }

        public Task<bool> InsertAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Task.Run(() => DataSet<T>().Insert(obj, relationsToSave));
        }

        public bool Delete<T>(T obj)
        {
            return GetRepository<T>().Delete(obj);
        }

        public Task<bool> DeleteAsync<T>(T obj)
        {
            return Task.Run(() => Delete(obj));
        }

        public bool Delete<T>(IEnumerable<T> objects)
        {
            var repository = GetRepository<T>();

            return objects.All(obj => repository.Delete(obj));
        }

        public Task<bool> DeleteAsync<T>(IEnumerable<T> objects)
        {
            return Task.Run(() => Delete(objects));
        }

        public bool Delete<T>(Expression<Func<T, bool>> condition)
        {
            var repository = GetRepository<T>();

            return repository.Delete(repository.CreateQuerySpec(new FilterSpec(condition)));
        }

        public Task<bool> DeleteAsync<T>(Expression<Func<T, bool>> condition)
        {
            return Task.Run(() => Delete(condition));
        }

        public void UpdateOrCreateOnlyRecord<T>(T rec)
        {
            using (var transaction = CreateTransaction())
            {
                DataSet<T>().Purge();

                Insert(rec);

                transaction.Commit();
            }
        }

        public Task UpdateOrCreateOnlyRecordAsync<T>(T rec)
        {
            return Task.Run(() => UpdateOrCreateOnlyRecord(rec));
        }

        // Native SQL methods


        public int SqlNonQuery(string sql, object parameters = null)
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlNonQuery(sql, QueryParameterCollection.FromObject(parameters));

            throw new NotSupportedException();
        }

        public Task<int> SqlNonQueryAsync(string sql, object parameters = null)
        {
            return Task.Run(() => SqlNonQuery(sql, parameters));
        }

        public IEnumerable<T> SqlQuery<T>(string sql, object parameters = null) where T : new()
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlQuery(sql, QueryParameterCollection.FromObject(parameters)).Select(entity => entity.CreateObject<T>());

            throw new NotSupportedException();
        }

        public Task<T[]> SqlQueryAsync<T>(string sql, object parameters = null) where T : new()
        {
            return Task.Run(() => SqlQuery<T>(sql, parameters).ToArray());
        }

        public IEnumerable<Dictionary<string,object>> SqlQuery(string sql, object parameters = null)
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlQuery(sql, QueryParameterCollection.FromObject(parameters)).Select(entity => entity.AsDictionary());

            throw new NotSupportedException();
        }

        public T SqlQueryScalar<T>(string sql, object parameters = null) where T : new()
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlQueryScalar(sql, QueryParameterCollection.FromObject(parameters)).FirstOrDefault().Convert<T>();

            throw new NotSupportedException();
        }

        public Task<T> SqlQueryScalarAsync<T>(string sql, object parameters = null) where T : new()
        {
            return Task.Run(() => SqlQueryScalar<T>(sql, parameters));
        }

        public IEnumerable<T> SqlQueryScalars<T>(string sql, object parameters = null) where T : new()
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlQueryScalar(sql, QueryParameterCollection.FromObject(parameters)).Select(scalar => scalar.Convert<T>());

            throw new NotSupportedException();
        }

        public Task<T[]> SqlQueryScalarsAsync<T>(string sql, object parameters = null) where T : new()
        {
            return Task.Run(() => SqlQueryScalars<T>(sql, parameters).ToArray());
        }

        public int SqlProcedure(string procName, object parameters = null)
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlProcedure(procName, QueryParameterCollection.FromObject(parameters));

            throw new NotSupportedException();
        }

        // Async methods


        public IAsyncDataSet<T> AsyncDataSet<T>()
        {
            return new AsyncDataSet<T>(new DataSet<T,T>(GetRepository<T>()));
        }

        private Task _LoadRelationsAsync(object obj, IEnumerable<LambdaExpression> relationsToLoad)
        {
            return Task.Run(() =>
            {
                TableSchema parentSchema = GetSchema(obj.GetType());

                Ir.LoadRelations(obj, LambdaRelationFinder.FindRelations(relationsToLoad, parentSchema));
            });
        }

        public Task LoadRelationsAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return _LoadRelationsAsync(obj, relationsToLoad);
        }

        public Task LoadRelationsAsync<T>(T obj, IEnumerable<Expression<Func<T, object>>> relationsToLoad)
        {
            return _LoadRelationsAsync(obj, relationsToLoad);
        }

        public async Task LoadRelationsAsync<T>(IEnumerable<T> list, params Expression<Func<T, object>>[] relationsToLoad)
        {
            foreach (var obj in list)
            {
                await _LoadRelationsAsync(obj, relationsToLoad).ConfigureAwait(false);
            }
        }

        public async Task LoadRelationsAsync<T>(IEnumerable<T> list, IEnumerable<Expression<Func<T, object>>> relationsToLoad)
        {
            relationsToLoad = relationsToLoad.ToList();

            foreach (var obj in list)
            {
                await _LoadRelationsAsync(obj, relationsToLoad).ConfigureAwait(false);
            }
        }

        public async Task LoadRelationsAsync(params Expression<Func<object>>[] relationsToLoad)
        {
            foreach (var relation in relationsToLoad)
            {
                if (relation.Body is MemberExpression memberExpression)
                {
                    await _LoadRelationsAsync(Expression.Lambda(memberExpression.Expression).Compile().DynamicInvoke(), new[] { relation }).ConfigureAwait(false);
                }
            }
        }

        // Logging

        public SqlLoggingContext StartSqlLogging(bool replaceParameters = false, Action<TimedSqlLogEntry> onLog = null) => new SqlLoggingContext(DataProvider, replaceParameters, onLog);
        public void StopSqlLogging(SqlLoggingContext context) => context.Dispose();

        // IDisposable

        public void Dispose()
        {
            DataProvider.Dispose();

            DataProvider = null;
        }
    }

    public static class RelationChainExtensions
    {
        public static U With<T, U>(this IEnumerable<T> self, Func<T, U> r) => throw new NotSupportedException();
    }

}