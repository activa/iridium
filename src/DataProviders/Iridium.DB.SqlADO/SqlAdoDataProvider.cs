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
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Iridium.Reflection;

#if IRIDIUM_SQLSERVER
namespace Iridium.DB.SqlServer
#elif IRIDIUM_MYSQL
namespace Iridium.DB.MySql
#else
namespace Iridium.DB.Sql
#endif
{
    public abstract class SqlAdoDataProvider<TConnection, TDialect> : SqlDataProvider<TDialect> where TConnection : DbConnection, new() where TDialect : SqlDialect, new()
    {
        public string ConnectionString { get; set; }

        private readonly ThreadLocal<TConnection> _localConnection = new ThreadLocal<TConnection>(true);

        protected SqlAdoDataProvider()
        {
        }

        protected SqlAdoDataProvider(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected TConnection Connection
        {
            get
            {
                var connection = _localConnection.Value;

                if (connection != null)
                {
                    if (connection.State == ConnectionState.Open)
                        return connection;

                    connection.Dispose();

                    ClearConnectionPool();
                }

                _localConnection.Value = CreateAndOpenConnection();

                return _localConnection.Value;
            }
        }

        protected virtual TConnection CreateAndOpenConnection()
        {
            var connection = new TConnection() { ConnectionString = ConnectionString };

            connection.Open();

            return connection;
        }

        protected void CloseConnection()
        {
            var connection = _localConnection.Value;

            if (connection != null && connection.State != ConnectionState.Closed)
                connection.Close();

            _localConnection.Value = null;
        }

        public abstract void ClearConnectionPool();

        protected DbCommand CreateCommand(string sqlQuery, QueryParameterCollection parameters, CommandType commandType)
        {
            DbCommand dbCommand = Connection.CreateCommand();

            dbCommand.CommandType = commandType;
            dbCommand.CommandText = sqlQuery;

            dbCommand.Transaction = CurrentTransaction;


            dbCommand.CommandText = Regex.Replace(sqlQuery, @"@(?<name>[a-z0-9A-Z_]+)", match => SqlDialect.CreateParameterExpression(match.Value.Substring(1)));

            if (parameters != null)
                foreach (var parameter in parameters)
                {
                    IDbDataParameter dataParameter = dbCommand.CreateParameter();

                    dataParameter.ParameterName = SqlDialect.CreateParameterExpression(parameter.Name);
                    dataParameter.Direction = ParameterDirection.Input;
                    dataParameter.DbType = DbType(parameter.Type);
                    
                    dataParameter.Value = ConvertParameter(parameter.Value);

                    dbCommand.Parameters.Add(dataParameter);
                }

            return dbCommand;
        }

        public DbType DbType(Type type)
        {
            var insp = type.Inspector();

            if (insp.Is(TypeFlags.String))
                return System.Data.DbType.String;
            if (insp.Is(TypeFlags.Integer16))
                return System.Data.DbType.Int16;
            if (insp.Is(TypeFlags.Integer32))
                return System.Data.DbType.Int32;
            if (insp.Is(TypeFlags.Integer64))
                return System.Data.DbType.Int64;
            if (insp.Is(TypeFlags.Byte))
                return System.Data.DbType.Byte;
            if (insp.Is(TypeFlags.SByte))
                return System.Data.DbType.SByte;
            if (insp.Is(TypeFlags.Boolean))
                return System.Data.DbType.Boolean;
            if (insp.Is(TypeFlags.Decimal))
                return System.Data.DbType.Decimal;
            if (insp.Is(TypeFlags.Single))
                return System.Data.DbType.Single;
            if (insp.Is(TypeFlags.Double))
                return System.Data.DbType.Double;
            if (insp.Is(TypeFlags.DateTime))
                return System.Data.DbType.DateTime;
            if (insp.Is(TypeFlags.DateTimeOffset))
                return System.Data.DbType.DateTimeOffset;
            if (insp.Is(TypeFlags.Guid))
                return System.Data.DbType.Guid;
            if (insp.Is(TypeFlags.UInt16))
                return System.Data.DbType.UInt16;
            if (insp.Is(TypeFlags.UInt32))
                return System.Data.DbType.UInt32;
            if (insp.Is(TypeFlags.UInt64))
                return System.Data.DbType.UInt64;
            if (insp.Is(TypeFlags.Array | TypeFlags.Byte))
                return System.Data.DbType.Binary;
            if (insp.Is(TypeFlags.Integer))
                return System.Data.DbType.Int32;

            return System.Data.DbType.Object;
        }

        protected virtual object ConvertParameter(object value)
        {
            if (value == null)
                return DBNull.Value;

            if (value is Enum)
                return Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), null);

            return value;
        }

        public override IEnumerable<Dictionary<string, object>> ExecuteSqlReader(string sql, QueryParameterCollection parameters)
        {
            var stopwatch = SqlLogger != null ? Stopwatch.StartNew() : null;

            try
            {
                BeginTransaction(IsolationLevel.None);

                List<Dictionary<string, object>> records = new List<Dictionary<string, object>>();

                using (var cmd = CreateCommand(sql, parameters, CommandType.Text))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> rec = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string fieldName = reader.GetName(i);

                                if (reader.IsDBNull(i))
                                    rec[fieldName] = null;
                                else
                                    rec[fieldName] = reader.GetValue(i);
                            }

                            records.Add(rec);
                        }
                    }
                }

                return records;
            }
            finally
            {
                CommitTransaction();

                SqlLogger?.LogSql(sql, parameters?.ToDictionary(p => SqlDialect.CreateParameterExpression(p.Name), p => p.Value), stopwatch?.Elapsed ?? TimeSpan.Zero);
            }
        }

        public override int ExecuteSql(string sql, QueryParameterCollection parameters = null)
        {
            var stopwatch = SqlLogger != null ? Stopwatch.StartNew() : null;

            try
            {
                BeginTransaction(IsolationLevel.None);

                using (var cmd = CreateCommand(sql, parameters, CommandType.Text))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                CommitTransaction();

                SqlLogger?.LogSql(sql, parameters?.ToDictionary(p => SqlDialect.CreateParameterExpression(p.Name), p => p.Value), stopwatch?.Elapsed ?? TimeSpan.Zero);
            }
        }

        public override int ExecuteProcedure(string procName, QueryParameterCollection parameters = null)
        {
            var stopwatch = SqlLogger != null ? Stopwatch.StartNew() : null;

            try
            {
                BeginTransaction(IsolationLevel.None);

                using (var cmd = CreateCommand(procName, parameters, CommandType.StoredProcedure))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                CommitTransaction();

                SqlLogger?.LogSql("EXEC " + procName, parameters?.ToDictionary(p => SqlDialect.CreateParameterExpression(p.Name), p => p.Value), stopwatch?.Elapsed ?? TimeSpan.Zero);
            }
        }

        public override void Dispose()
        {
            foreach (var connection in _localConnection.Values)
            {
                connection?.Dispose();
            }
        }

        protected System.Data.IsolationLevel AdoIsolationLevel(IsolationLevel isolationLevel)
        {
            System.Data.IsolationLevel adoIsolationLevel;

            switch (isolationLevel)
            {
                case IsolationLevel.Chaos:
                    adoIsolationLevel = System.Data.IsolationLevel.Chaos;
                    break;
                case IsolationLevel.ReadUncommitted:
                    adoIsolationLevel = System.Data.IsolationLevel.ReadUncommitted;
                    break;
                case IsolationLevel.ReadCommitted:
                    adoIsolationLevel = System.Data.IsolationLevel.ReadCommitted;
                    break;
                case IsolationLevel.RepeatableRead:
                    adoIsolationLevel = System.Data.IsolationLevel.RepeatableRead;
                    break;
                case IsolationLevel.Serializable:
                    adoIsolationLevel = System.Data.IsolationLevel.Serializable;
                    break;
                case IsolationLevel.Snapshot:
                    adoIsolationLevel = System.Data.IsolationLevel.Snapshot;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isolationLevel), isolationLevel, null);
            }

            return adoIsolationLevel;

        }

        protected abstract DbTransaction CurrentTransaction { get; }
    }

    public interface ITransaction
    {
    }
}