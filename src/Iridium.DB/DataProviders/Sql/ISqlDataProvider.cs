using System.Collections.Generic;

namespace Iridium.DB
{
    public interface ISqlDataProvider
    {
        int SqlProcedure(string procName, QueryParameterCollection parameters);
        int SqlNonQuery(string sql, QueryParameterCollection parameters);
        IEnumerable<SerializedEntity> SqlQuery(string sql, QueryParameterCollection parameters);
        IEnumerable<object> SqlQueryScalar(string sql, QueryParameterCollection parameters);

        ISqlLogger SqlLogger { get; set; }
    }
}