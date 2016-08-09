#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015 Philippe Leybaert
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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Iridium.Core;

namespace Iridium.DB.SqlService
{
    public class SqlServiceDataProvider : SqlDataProvider<SqlServiceDialect>
    {
        private readonly HttpClient _httpClient;
        private readonly ThreadLocal<long?> _lastRowId = new ThreadLocal<long?>();
        private readonly string _server;

        public SqlServiceDataProvider()
        {
        }

        public SqlServiceDataProvider(string server, string login, string password)
        {
            _httpClient = new HttpClient();

            _server = server;
        }


        public override bool RequiresAutoIncrementGetInSameStatement => false;

        public override int ExecuteSql(string sql, QueryParameterCollection parameters)
        {
            var content = CreatePayload(sql, parameters);

            var result = _httpClient.PostAsync(new Uri(_server + "/svc/execute"), content).Result;

            var json = result.Content.ReadAsStringAsync().Result;

            Debug.WriteLine("Received JSON: {0}", json);

            JsonObject jsonResult = JsonParser.Parse(json);

            _lastRowId.Value = jsonResult["lastrowid"].As<long>();

            return jsonResult["rowsaffected"].As<int>();
        }

        private static HttpContent CreatePayload(string sql, QueryParameterCollection parameters)
        {
            var json = JsonSerializer.ToJson(new
            {
                sql,
                parameters = parameters?.ToDictionary(kvp => kvp.Name, kvp => kvp.Value is byte[] ? new[] {Convert.ToBase64String((byte[]) kvp.Value)} : kvp.Value is DateTime ? ((DateTime) kvp.Value).Ticks : kvp.Value)
            });

            Debug.WriteLine("Sending payload: {0}",json);

            return new StringContent(json, Encoding.UTF8);
        }


        public override IEnumerable<Dictionary<string, object>> ExecuteSqlReader(string sql, QueryParameterCollection parameters)
        {
            var content = CreatePayload(sql, parameters);

            var result = _httpClient.PostAsync(new Uri(_server + "/svc/executereader"), content).Result;

            var json = result.Content.ReadAsStringAsync().Result;

            Debug.WriteLine("Received JSON: {0}",json);

            JsonObject jsonResult = JsonParser.Parse(json);

            _lastRowId.Value = jsonResult["lastrowid"].As<long>();

            foreach (var jsonRecord in jsonResult["records"])
            {
                Dictionary<string, object> record = new Dictionary<string, object>();

                foreach (var key in jsonRecord.Keys)
                {
                    record[key] = jsonRecord[key].As<object>();
                }

                yield return record;

            }
        }

        public override void Purge(TableSchema schema)
        {
            var tableName = SqlDialect.QuoteTable(schema.MappedName);

            ExecuteSql($"DELETE FROM {tableName}", null);
            ExecuteSql("delete from sqlite_sequence where name=@name", QueryParameterCollection.FromObject(new { name = schema.MappedName }));
        }

        public override long GetLastAutoIncrementValue(TableSchema schema)
        {
            return _lastRowId.Value ?? 0;
        }

        private readonly ThreadLocal<Stack<bool>> _transactionStack = new ThreadLocal<Stack<bool>>(() => new Stack<bool>());

        public override void BeginTransaction(IsolationLevel isolationLevel)
        {
            if (isolationLevel == IsolationLevel.None)
                _transactionStack.Value.Push(false);
            else
            {
                ExecuteSql("BEGIN TRANSACTION", null);

                _transactionStack.Value.Push(true);
            }

        }

        public override void CommitTransaction()
        {
            bool realTransaction = _transactionStack.Value.Pop();

            if (realTransaction)
                ExecuteSql("COMMIT", null);

        }

        public override void RollbackTransaction()
        {
            bool realTransaction = _transactionStack.Value.Pop();

            if (realTransaction)
                ExecuteSql("ROLLBACK", null);
        }


        public override void Dispose()
        {
            lock (this)
            {
                _httpClient.Dispose();
            }
        }
    }

}