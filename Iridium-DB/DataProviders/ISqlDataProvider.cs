using System.Collections.Generic;

namespace Iridium.DB
{
    public interface ISqlDataProvider
    {
        int SqlNonQuery(string sql, QueryParameterCollection parameters);
        IEnumerable<SerializedEntity> SqlQuery(string sql, QueryParameterCollection parameters);
        IEnumerable<object> SqlQueryScalar(string sql, QueryParameterCollection parameters);
    }
}