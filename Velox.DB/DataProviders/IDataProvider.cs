using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Velox.DB
{
    public interface IDataProvider
    {
        object GetScalar(Aggregate aggregate, INativeQuerySpec nativeQuerySpec, OrmSchema schema);
        IEnumerable<SerializedEntity> GetObjects(INativeQuerySpec filter, OrmSchema schema);
        IEnumerable<SerializedEntity> GetObjectsWithPrefetch(INativeQuerySpec filter, OrmSchema schema, IEnumerable<OrmSchema.Relation> prefetchRelations, out IEnumerable<Dictionary<OrmSchema.Relation, SerializedEntity>> relatedEntities);
        ObjectWriteResult WriteObject(SerializedEntity o, bool createNew, OrmSchema schema);
        SerializedEntity ReadObject(object[] keys, OrmSchema schema);
        bool DeleteObject(SerializedEntity o, OrmSchema schema);
        bool DeleteObjects(INativeQuerySpec filter, OrmSchema schema);
        QuerySpec CreateQuerySpec(FilterSpec filter, ScalarSpec scalarEpression, SortOrderSpec sortOrder, int? skip, int? take, OrmSchema schema);
        void Purge(OrmSchema schema);

        bool SupportsQueryTranslation(QueryExpression expression = null);

        bool SupportsRelationPrefetch { get; }

        bool CreateOrUpdateTable(OrmSchema schema);

        int ExecuteSql(string sql, QueryParameterCollection parameters);
        IEnumerable<SerializedEntity> Query(string sql, QueryParameterCollection parameters);
        object QueryScalar(string sql, QueryParameterCollection parameters);
    }
}