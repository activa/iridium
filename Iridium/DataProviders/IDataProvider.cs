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

namespace Iridium.DB
{
    public interface IDataProvider : IDisposable
    {
        object GetScalar(Aggregate aggregate, INativeQuerySpec nativeQuerySpec, TableSchema schema);
        IEnumerable<SerializedEntity> GetObjects(INativeQuerySpec filter, TableSchema schema);
        IEnumerable<SerializedEntity> GetObjectsWithPrefetch(INativeQuerySpec filter, TableSchema schema, IEnumerable<TableSchema.Relation> prefetchRelations, out IEnumerable<Dictionary<TableSchema.Relation, SerializedEntity>> relatedEntities);
        ObjectWriteResult WriteObject(SerializedEntity o, bool createNew, TableSchema schema);
        SerializedEntity ReadObject(Dictionary<string,object> keys, TableSchema schema);
        bool DeleteObject(SerializedEntity o, TableSchema schema);
        bool DeleteObjects(INativeQuerySpec filter, TableSchema schema);
        QuerySpec CreateQuerySpec(FilterSpec filter, ScalarSpec scalarEpression, SortOrderSpec sortOrder, int? skip, int? take, TableSchema schema);
        void Purge(TableSchema schema);

        bool SupportsQueryTranslation(QueryExpression expression = null);

        bool SupportsRelationPrefetch { get; }
        bool SupportsTransactions { get; }
        bool SupportsSql { get; }

        bool CreateOrUpdateTable(TableSchema schema, bool recreateTable = false, bool recreateIndexes = false);

        int ExecuteSql(string sql, QueryParameterCollection parameters);
        IEnumerable<SerializedEntity> Query(string sql, QueryParameterCollection parameters);
        IEnumerable<object> QueryScalar(string sql, QueryParameterCollection parameters);

        void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.None);
        void CommitTransaction();
        void RollbackTransaction();
    }
}