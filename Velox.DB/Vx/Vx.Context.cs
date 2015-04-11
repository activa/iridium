using System;
using System.Collections.Generic;
using System.Linq;
using Velox.DB.Core;

namespace Velox.DB
{
    public static partial class Vx
    {
        public class Context
        {
            private readonly SafeDictionary<Type, Repository> _repositories = new SafeDictionary<Type, Repository>();

            public IDataProvider DataProvider { get; private set; }

            public Context(IDataProvider dataProvider)
            {
                DataProvider = dataProvider;

                foreach (var property in GetType().Inspector().GetFieldsAndProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var fieldTypeInspector = property.FieldType.Inspector();

                    if (!fieldTypeInspector.IsGenericType || property.FieldType.GetGenericTypeDefinition() != typeof (IDataSet<>))
                        continue;

                    var objectType = fieldTypeInspector.GetGenericArguments()[0];
                    var repository = (Repository) Activator.CreateInstance(typeof (Repository<>).MakeGenericType(objectType), this);
                    var dataSet = Activator.CreateInstance(typeof (DataSet<>).MakeGenericType(objectType), repository);

                    _repositories[objectType] = repository;

                    property.SetValue(this, dataSet);
                }
            }

            internal OrmSchema GetSchema(Type objectType)
            {
                var repository = _repositories[objectType];

                return repository == null ? null : repository.Schema;
            }

            private void InvalidateRelations()
            {
                foreach (var repository in _repositories.Values)
                {
                    repository.Schema.InvalidateRelations();
                }
            }

            private Repository<T> GetRepository<T>()
            {
                var repository = _repositories[typeof(T)];

                if (repository == null)
                {
                    InvalidateRelations();

                    _repositories[typeof(T)] = (repository = new Repository<T>(this));
                }

                return (Repository<T>) repository;
            }

            public IDataSet<T> DataSet<T>()
            {
                return new DataSet<T>(GetRepository<T>());
            }

            public void CreateTable<T>()
            {
                DataProvider.CreateOrUpdateTable(GetRepository<T>().Schema);
            }

            public int Execute(string sql, object parameters)
            {
                return DataProvider.ExecuteSql(sql, new QueryParameterCollection(parameters));
            }

            public int Execute(string sql, QueryParameterCollection parameters = null)
            {
                return DataProvider.ExecuteSql(sql, parameters);
            }

            public IEnumerable<T> Query<T>(string sql, object parameters = null) where T : new()
            {
                return DataProvider.Query(sql, new QueryParameterCollection(parameters)).Select(entity => entity.CreateObject<T>());
            }

            public T QueryScalar<T>(string sql, object parameters = null) where T : new()
            {
                return DataProvider.QueryScalar(sql, new QueryParameterCollection(parameters)).Convert<T>();
            }


        }
    }
}