using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Iridium.DB
{
    public interface ISqlStorageContext : IStorageContext
    {
        int SqlNonQuery(string sql, object parameters = null);
        Task<int> SqlNonQueryAsync(string sql, object parameters = null);
        IEnumerable<T> SqlQuery<T>(string sql, object parameters = null) where T : new();
        Task<T[]> SqlQueryAsync<T>(string sql, object parameters = null) where T : new();
        IEnumerable<Dictionary<string, object>> SqlQuery(string sql, object parameters = null);
        Task<List<Dictionary<string, object>>> SqlQueryAsync(string sql, object parameters = null);
        T SqlQueryScalar<T>(string sql, object parameters = null) where T : new();
        Task<T> SqlQueryScalarAsync<T>(string sql, object parameters = null) where T : new();
        IEnumerable<T> SqlQueryScalars<T>(string sql, object parameters = null) where T : new();
        Task<T[]> SqlQueryScalarsAsync<T>(string sql, object parameters = null) where T : new();
        int SqlProcedure(string procName, object parameters = null);
        SqlLoggingContext StartSqlLogging(bool replaceParameters = false, Action<TimedSqlLogEntry> onLog = null);
        void StopSqlLogging(SqlLoggingContext context);
    }
}