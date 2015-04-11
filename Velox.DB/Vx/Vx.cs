using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Velox.DB
{
    public static partial class Vx
    {
        private static Context _defaultContext;

        public static IDataSet<T> DataSet<T>()
        {
            return Repository.CreateDataSet<T>();
        }

        public static Context DB
        {
            get { return _defaultContext; }
            set { _defaultContext = value; }
        }

        public static void LoadRelations<T>(T obj, params Expression<Func<T,object>>[] relationsToLoad)
        {
            _LoadRelations(obj, relationsToLoad, null);
        }

        public static void LoadRelations<T>(T obj, IEnumerable<Expression<Func<T, object>>> relationsToLoad)
        {
            _LoadRelations(obj, relationsToLoad, null);
        }

        public static void LoadRelations(params Expression<Func<object>>[] relationsToLoad)
        {
            foreach (var relation in relationsToLoad)
            {
                var memberExpression = relation.Body as MemberExpression;

                if (memberExpression != null)
                {
                    _LoadRelations(Expression.Lambda(memberExpression.Expression).Compile().DynamicInvoke(), new[]{relation}, null);
                }
            }
        }

        public static int GetQueryCount<T>(IDataSet<T> dataSet)
        {
            return ((DataSet<T>) dataSet).Repository.QueryCount;
        }

        public static void ResetStats<T>(IDataSet<T> dataSet)
        {
            ((DataSet<T>) dataSet).Repository.QueryCount = 0;
        }

        internal static T WithLoadedRelations<T>(T obj, IEnumerable<OrmSchema.Relation> relations)
        {
            if (relations!= null)
                _LoadRelations(obj, relations);

            return obj;
        }

        private static void _LoadRelations(object obj, IEnumerable<LambdaExpression> relationsToLoad, OrmSchema parentSchema)
        {
            if (parentSchema == null)
                parentSchema = SchemaForObject(obj);

            if (parentSchema == null)
                throw new Exception("LoadRelations() not supported for multiple contexts");

            _LoadRelations(obj, LambdaRelationFinder.FindRelations(relationsToLoad, parentSchema));
        }

        private static void _LoadRelations(object obj, IEnumerable<OrmSchema.Relation> relationsToLoad)
        {
            var objectType = obj.GetType();

            var splitRelations = relationsToLoad.ToLookup(r => r.LocalSchema.ObjectType == objectType);

            var localRelations = splitRelations[true];

            foreach (var relation in localRelations)
            {
                object value = relation.GetField(obj);

                if (value == null)
                {
                    value = relation.LoadRelation(obj);

                    relation.SetField(obj, value);

                    if (value == null)
                        continue;
                }

                var deepRelations = splitRelations[false].ToList(); // avoid multiple enumerations

                if (relation.RelationType == OrmSchema.RelationType.OneToMany)
                {
                    foreach (var item in (IEnumerable) value)
                    {
                        _LoadRelations(item, deepRelations);
                    }
                }
                else
                {
                    _LoadRelations(value, deepRelations);
                }
            }
        }

        private static OrmSchema SchemaForObject(object obj)
        {
            var objectType = obj.GetType();

            var repository = Repository.GetRepository(objectType);

            return repository == null ? null : repository.Schema;
        }
    }
}