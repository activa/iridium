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
using System.Threading;
using Iridium.Reflection;

namespace Iridium.DB
{
    public class MemoryDataProvider : IDataProvider
    {
        private class CompositeKey
        {
            private readonly Dictionary<string,object> _keyValues;

            public CompositeKey(Dictionary<string,object> keyValues)
            {
                _keyValues = keyValues;
            }

            public CompositeKey(TableSchema schema, SerializedEntity o)
            {
                _keyValues = schema.PrimaryKeys.ToDictionary(pk => pk.MappedName, pk => o[pk.MappedName]);
            }

            public override int GetHashCode()
            {
                return _keyValues.Aggregate(0, (current, t) => current ^ t.Value.GetHashCode() ^ t.Key.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                var other = (CompositeKey) obj;

                return _keyValues.All(pk => other._keyValues.ContainsKey(pk.Key) && pk.Value.Equals(other._keyValues[pk.Key]));
            }
        }

        private class StorageBucket
        {
            private readonly List<Dictionary<string, object>> _objects = new List<Dictionary<string, object>>();
            private readonly Dictionary<CompositeKey,Dictionary<string,object>> _indexedObjects = new Dictionary<CompositeKey, Dictionary<string, object>>();
            private readonly SafeDictionary<string, long> _autoIncrementCounters = new SafeDictionary<string, long>();

            private long NextIncrementCounter(string fieldName)
            {
                var counter = _autoIncrementCounters[fieldName] + 1;

                _autoIncrementCounters[fieldName] = counter;

                return counter;
            }

            private void Purge()
            {
                _autoIncrementCounters.Clear();
                _objects.Clear();
                _indexedObjects.Clear();
            }

            public BucketAccessor Accessor() { return new BucketAccessor(this); }

            public class BucketAccessor : IDisposable
            {
                private readonly StorageBucket _bucket;

                public BucketAccessor(StorageBucket bucket)
                {
                    _bucket = bucket;

                    Monitor.Enter(_bucket);
                }

                public void Dispose()
                {
                    Monitor.Exit(_bucket);
                }

                public long NextIncrementCounter(string fieldName) { return _bucket.NextIncrementCounter(fieldName); }
                public void Purge() { _bucket.Purge(); }
                public List<Dictionary<string, object>> Objects => _bucket._objects;
                public Dictionary<CompositeKey, Dictionary<string, object>> IndexedObjects => _bucket._indexedObjects;
            }

        }

        private readonly SafeDictionary<TableSchema,StorageBucket> _buckets = new SafeDictionary<TableSchema, StorageBucket>();

        private StorageBucket.BucketAccessor GetBucket(TableSchema schema)
        {
            return (_buckets[schema] ?? (_buckets[schema] = new StorageBucket())).Accessor();
        }

        public object GetScalar(Aggregate aggregate, INativeQuerySpec nativeQuerySpec, TableSchema schema)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<SerializedEntity> GetObjects(INativeQuerySpec querySpec, TableSchema schema)
        {
            if (querySpec != null)
                throw new NotSupportedException();

            using (var bucket = GetBucket(schema))
            {
                return from o in bucket.Objects select new SerializedEntity(o);
            }
        }

        public IEnumerable<SerializedEntity> GetObjectsWithPrefetch(INativeQuerySpec filter, TableSchema schema, IEnumerable<TableSchema.Relation> prefetchRelations, out IEnumerable<Dictionary<TableSchema.Relation, SerializedEntity>> relatedEntities)
        {
            throw new NotSupportedException();
        }

        public ObjectWriteResult WriteObject(SerializedEntity o, bool? createNew, TableSchema schema)
        {
            if (schema.IncrementKey != null && createNew == null)
                return new ObjectWriteResult() {Success = false};

            var result = new ObjectWriteResult();

            using (var bucket = GetBucket(schema))
            {
                if (createNew == null)
                {
                    if (schema.PrimaryKeys.Length == 0)
                        return new ObjectWriteResult() { Success = false };

                    var compositeKey = new CompositeKey(schema.PrimaryKeys.ToDictionary(field => field.MappedName, field => o[field.MappedName]));

                    createNew = !bucket.IndexedObjects.ContainsKey(compositeKey);
                }

                if (createNew.Value)
                {
                    if (schema.IncrementKey != null)
                    {
                        o[schema.IncrementKey.MappedName] = bucket.NextIncrementCounter(schema.IncrementKey.FieldName).Convert(schema.IncrementKey.FieldType);

                        result.OriginalUpdated = true;
                    }

                    var storedObject = o.AsDictionary();

                    bucket.Objects.Add(storedObject);
                    bucket.IndexedObjects[new CompositeKey(schema,o)] = storedObject;

                    result.Added = true;
                    result.Success = true;
                }
                else
                {
                    if (schema.PrimaryKeys.Length > 0)
                    {
                        for (int i = 0; i < bucket.Objects.Count; i++)
                        {
                            if (schema.PrimaryKeys.All(primaryKey => Equals(bucket.Objects[i][primaryKey.MappedName], o[primaryKey.MappedName])))
                            {
                                var compositeKey = new CompositeKey(schema, o);

                                var storedObject = o.AsDictionary();

                                bucket.Objects[i] = storedObject;
                                bucket.IndexedObjects[compositeKey] = storedObject;
                                result.Success = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        result.Success = false;
                    }
                }
            }

            return result;
        }

        public SerializedEntity ReadObject(Dictionary<string,object> keys, TableSchema schema)
        {
            using (var bucket = GetBucket(schema))
            {
                var compositeKey = new CompositeKey(keys);

                if (!bucket.IndexedObjects.ContainsKey(compositeKey))
                    return null;

                var storedObject = bucket.IndexedObjects[compositeKey];

                return new SerializedEntity(storedObject);
            }
        }

        public bool DeleteObject(SerializedEntity o, TableSchema schema)
        {
            using (var bucket = GetBucket(schema))
            {
                for (int i = 0; i < bucket.Objects.Count; i++)
                {
                    if (schema.PrimaryKeys.All(primaryKey => Equals(bucket.Objects[i][primaryKey.MappedName], o[primaryKey.MappedName])))
                    {
                        bucket.IndexedObjects.Remove(bucket.IndexedObjects.First(kv => kv.Value == bucket.Objects[i]).Key);
                        bucket.Objects.RemoveAt(i);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool DeleteObjects(INativeQuerySpec filter, TableSchema schema)
        {
            throw new NotSupportedException();
        }

        public QuerySpec CreateQuerySpec(FilterSpec filter, ScalarSpec expression, SortOrderSpec sortOrder, int? skip, int? take, TableSchema schema)
        {
            throw new NotSupportedException();
        }

        public bool SupportsQueryTranslation(QueryExpression expression)
        {
            return false;
        }

        public bool SupportsRelationPrefetch => false;
        public bool SupportsTransactions => false;
        public bool SupportsSql => false;

        public bool CreateOrUpdateTable(TableSchema schema, bool recreateTable, bool recreateIndexes)
        {
            return true; // NOP
        }

        public int ExecuteSql(string sql, QueryParameterCollection parameters)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<SerializedEntity> Query(string sql, QueryParameterCollection parameters)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<object> QueryScalar(string sql, QueryParameterCollection parameters)
        {
            throw new NotSupportedException();
        }

        public void BeginTransaction(IsolationLevel isolationLevel)
        {
            
        }

        public void CommitTransaction()
        {
            
        }

        public void RollbackTransaction()
        {
            
        }

        public ISqlLogger SqlLogger
        {
            get => null;
            set { }
        }

        public void Purge(TableSchema schema)
        {
            using (var bucket = GetBucket(schema))
            {
                bucket.Purge();
            }
        }

        public void Dispose()
        {
            _buckets.Clear();
        }
    }
}