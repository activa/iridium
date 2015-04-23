#region License
//=============================================================================
// Velox.DB - Portable .NET ORM 
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Velox.DB.Core;


namespace Velox.DB
{
    public class MemoryDataProvider : IDataProvider
    {
        private class CompositeKey
        {
            private Dictionary<string,object>  _keyValues;

            public CompositeKey(Dictionary<string,object> keyValues)
            {
                _keyValues = keyValues;
            }

            public CompositeKey(OrmSchema schema, SerializedEntity o)
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
                public List<Dictionary<string, object>> Objects { get { return _bucket._objects; } }
                public Dictionary<CompositeKey, Dictionary<string, object>> IndexedObjects { get { return _bucket._indexedObjects; } }
            }

        }

        private readonly SafeDictionary<OrmSchema,StorageBucket> _buckets = new SafeDictionary<OrmSchema, StorageBucket>();

        private StorageBucket.BucketAccessor GetBucket(OrmSchema schema)
        {
            return (_buckets[schema] ?? (_buckets[schema] = new StorageBucket())).Accessor();
        }

        public object GetScalar(Aggregate aggregate, INativeQuerySpec nativeQuerySpec, OrmSchema schema)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<SerializedEntity> GetObjects(INativeQuerySpec querySpec, OrmSchema schema)
        {
            if (querySpec != null)
                throw new NotSupportedException();

            using (var bucket = GetBucket(schema))
            {
                return from o in bucket.Objects select new SerializedEntity(o);
            }
        }

        public IEnumerable<SerializedEntity> GetObjectsWithPrefetch(INativeQuerySpec filter, OrmSchema schema, IEnumerable<OrmSchema.Relation> prefetchRelations, out IEnumerable<Dictionary<OrmSchema.Relation, SerializedEntity>> relatedEntities)
        {
            throw new NotSupportedException();
        }

        public ObjectWriteResult WriteObject(SerializedEntity o, bool createNew, OrmSchema schema)
        {
            var result = new ObjectWriteResult();

            using (var bucket = GetBucket(schema))
            {
                if (createNew)
                {
                    foreach (var incrementKey in schema.IncrementKeys.Where(incrementKey => o[incrementKey.MappedName].Convert<long>() == 0))
                    {
                        o[incrementKey.MappedName] = bucket.NextIncrementCounter(incrementKey.FieldName).Convert(incrementKey.FieldType);

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

        public SerializedEntity ReadObject(Dictionary<string,object> keys, OrmSchema schema)
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

        public bool DeleteObject(SerializedEntity o, OrmSchema schema)
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

        public bool DeleteObjects(INativeQuerySpec filter, OrmSchema schema)
        {
            throw new NotSupportedException();
        }

        public QuerySpec CreateQuerySpec(FilterSpec filter, ScalarSpec expression, SortOrderSpec sortOrder, int? skip, int? take, OrmSchema schema)
        {
            throw new NotSupportedException();
        }

        public bool SupportsQueryTranslation(QueryExpression expression)
        {
            return false;
        }

        public bool SupportsRelationPrefetch
        {
            get { return false; }
        }

        public bool CreateOrUpdateTable(OrmSchema schema, bool recreateTable, bool recreateIndexes)
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

        public void Purge(OrmSchema schema)
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