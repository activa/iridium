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
                var dataSet = Activator.CreateInstance(typeof (DataSet<>).MakeGenericType(objectType), repository);

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

        private Repository<T> GetRepository<T>()
        {
            lock (_repositories)
            {
                var repository = _repositories[typeof(T)];

                if (repository == null)
                {
                    _repositories[typeof(T)] = (repository = new Repository<T>(this));

                    GenerateRelations();
                }

                return (Repository<T>) repository;
            }
        }

        public IDataSet<T> DataSet<T>()
        {
            return new DataSet<T>(GetRepository<T>());
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

        public T Read<T>(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return GetRepository<T>().Read(key, relationsToLoad);
        }

        public T Read<T>(Expression<Func<T,bool>> condition,  params Expression<Func<T, object>>[] relationsToLoad)
        {
            return DataSet<T>().WithRelations(relationsToLoad).FirstOrDefault(condition);
        }

        public T Load<T>(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return DataSet<T>().Load(obj, key, relationsToLoad);
        }

        public bool Save<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Save(obj, relationsToSave);
        }

        public bool InsertOrUpdate<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Save(obj, relationsToSave);
        }

        public bool Update<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Update(obj, relationsToSave);
        }

        public bool Insert<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return DataSet<T>().Insert(obj, relationsToSave);
        }

        public bool Delete<T>(T obj)
        {
            return GetRepository<T>().Delete(obj);
        }

        public bool Delete<T>(Expression<Func<T, bool>> condition)
        {
            return GetRepository<T>().Delete(GetRepository<T>().CreateQuerySpec(new FilterSpec(condition)));
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

        // Native SQL methods

        public int SqlNonQuery(string sql, object parameters = null)
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlNonQuery(sql, QueryParameterCollection.FromObject(parameters));

            throw new NotSupportedException();
        }

        public IEnumerable<T> SqlQuery<T>(string sql, object parameters = null) where T : new()
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlQuery(sql, QueryParameterCollection.FromObject(parameters)).Select(entity => entity.CreateObject<T>());

            throw new NotSupportedException();
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

        public IEnumerable<T> SqlQueryScalars<T>(string sql, object parameters = null) where T : new()
        {
            if (DataProvider is ISqlDataProvider sqlProvider)
                return sqlProvider.SqlQueryScalar(sql, QueryParameterCollection.FromObject(parameters)).Select(scalar => scalar.Convert<T>());

            throw new NotSupportedException();
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
            return new AsyncDataSet<T>(new DataSet<T>(GetRepository<T>()));
        }

        public Task CreateTableAsync<T>(bool recreateTable = false, bool recreateIndexes = false)
        {
            return Task.Run(() => CreateTable<T>(recreateTable, recreateIndexes));
        }

        public Task<int> SqlNonQueryAsync(string sql, object parameters = null)
        {
            return Task.Run(() => SqlNonQuery(sql, parameters));
        }

        public Task<T[]> SqlQueryAsync<T>(string sql, object parameters = null) where T : new()
        {
            return Task.Run(() => SqlQuery<T>(sql, parameters).ToArray());
        }

        public Task<T> SqlQueryScalarAsync<T>(string sql, object parameters = null) where T : new()
        {
            return Task.Run(() => SqlQueryScalar<T>(sql, parameters));
        }

        public Task<T[]> SqlQueryScalarsAsync<T>(string sql, object parameters = null) where T : new()
        {
            return Task.Run(() => SqlQueryScalars<T>(sql, parameters).ToArray());
        }

        public Task<T> ReadAsync<T>(object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return Task.Run(() => GetRepository<T>().Read(key, relationsToLoad));
        }

        public Task<T> ReadAsync<T>(Expression<Func<T, bool>> condition, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return AsyncDataSet<T>().WithRelations(relationsToLoad).FirstOrDefault(condition);
        }

        public Task<T> LoadAsync<T>(T obj, object key, params Expression<Func<T, object>>[] relationsToLoad)
        {
            return Task.Run(() => GetRepository<T>().Load(obj, key, relationsToLoad));
        }

        public Task<bool> SaveAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Task.Run(() => DataSet<T>().Save(obj, relationsToSave));
        }

        public Task<bool> InsertAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Task.Run(() => DataSet<T>().Insert(obj, relationsToSave));
        }

        public Task<bool> UpdateAsync<T>(T obj, params Expression<Func<T, object>>[] relationsToSave)
        {
            return Task.Run(() => DataSet<T>().Update(obj, relationsToSave));
        }

        public Task<bool> DeleteAsync<T>(T obj)
        {
            return Task.Run(() => GetRepository<T>().Delete(obj));
        }

        public Task UpdateOrCreatOnlyRecordAsync<T>(T rec)
        {
            return Task.Run(() => UpdateOrCreateOnlyRecord(rec));
        }

        // IDisposable

        public void Dispose()
        {
            DataProvider.Dispose();

            DataProvider = null;
        }
    }
}