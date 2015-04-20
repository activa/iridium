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
using System.Collections.Generic;
using System.Linq;
using Velox.DB.Core;


namespace Velox.DB.Sql
{
    public abstract class SqlDataProvider<T> : SqlDataProvider where T : SqlDialect, new()
    {
        protected SqlDataProvider() : base(new T())
        {
        }
    }

    public abstract class SqlDataProvider : IDataProvider
    {
        public SqlDialect SqlDialect { get; private set; }

        protected SqlDataProvider(SqlDialect sqlDialect)
        {
            SqlDialect = sqlDialect;
        }

        public object GetScalar(Aggregate aggregate, INativeQuerySpec nativeQuerySpec, OrmSchema schema)
        {
            var querySpec = (SqlQuerySpec) nativeQuerySpec;

            string expressionSql;

            int? limit = null;

            switch (aggregate)
            {
                case Aggregate.Sum:
                    expressionSql = "sum(" + querySpec.ExpressionSql + ")";
                    break;
                case Aggregate.Average:
                    expressionSql = "avg(" + querySpec.ExpressionSql + ")";
                    break;
                case Aggregate.Max:
                    expressionSql = "max(" + querySpec.ExpressionSql + ")";
                    break;
                case Aggregate.Min:
                    expressionSql = "min(" + querySpec.ExpressionSql + ")";
                    break;
                case Aggregate.Count:
                    expressionSql = "count(*)";
                    break;
                case Aggregate.Any:
                    expressionSql = "1";
                    limit = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("aggregate");
            }

            var valueAlias = SqlNameGenerator.NextFieldAlias();

            string sql = SqlDialect.SelectSql(
                new SqlTableNameWithAlias(schema.MappedName, querySpec.TableAlias), 
                new[] { new SqlExpressionWithAlias(expressionSql,valueAlias) },
                querySpec.FilterSql, 
                querySpec.Joins,
                numRecords: limit
                );

            var record = ExecuteSqlReader(sql, querySpec.SqlParameters).FirstOrDefault();

            SqlNameGenerator.Reset();

            if (aggregate == Aggregate.Any)
                return record != null;
            
            return record[valueAlias];
        }

        public IEnumerable<SerializedEntity> GetObjects(INativeQuerySpec filter, OrmSchema schema)
        {
            try
            {
                SqlQuerySpec sqlQuerySpec = ((SqlQuerySpec) filter) ?? new SqlQuerySpec {FilterSql = null, TableAlias = SqlNameGenerator.NextTableAlias()};

                var fieldList = (from f in schema.Fields.Values select new {Field = f, Alias = SqlNameGenerator.NextFieldAlias()}).ToArray();

                string sql = SqlDialect.SelectSql(
                    new SqlTableNameWithAlias(schema.MappedName, sqlQuerySpec.TableAlias),
                    fieldList.Select(field => new SqlColumnNameWithAlias(sqlQuerySpec.TableAlias + "." + field.Field.MappedName, field.Alias)).ToArray(),
                    sqlQuerySpec.FilterSql,
                    sqlQuerySpec.Joins,
                    sqlQuerySpec.SortExpressionSql,
                    sqlQuerySpec.Skip + 1,
                    sqlQuerySpec.Take
                    );

                return from record in ExecuteSqlReader(sql, sqlQuerySpec.SqlParameters)
                    select new SerializedEntity(fieldList.ToDictionary(c => c.Field.MappedName, c => record[c.Alias].Convert(c.Field.FieldType)));
            }
            finally
            {
                SqlNameGenerator.Reset();
            }
        }

        private class PrefetchFieldDefinition
        {
            public string TableAlias;
            public string FieldAlias;
            public OrmSchema.Field Field;
        }
        
        public IEnumerable<SerializedEntity> GetObjectsWithPrefetch(INativeQuerySpec filter, OrmSchema schema, IEnumerable<OrmSchema.Relation> prefetchRelations, out IEnumerable<Dictionary<OrmSchema.Relation, SerializedEntity>> relatedEntities)
        {
            try
            {
                SqlQuerySpec sqlQuerySpec = ((SqlQuerySpec)filter) ?? new SqlQuerySpec { FilterSql = null, TableAlias = SqlNameGenerator.NextTableAlias() };

                var fieldList = (from f in schema.Fields.Values select new { Field = f, Alias = SqlNameGenerator.NextFieldAlias() }).ToList();

                var joins = new HashSet<SqlJoinDefinition>(sqlQuerySpec.Joins);
                var fieldsByRelation = new Dictionary<OrmSchema.Relation, PrefetchFieldDefinition[]>();
                var foreignKeyAliases = new Dictionary<OrmSchema.Relation, string>();

                foreach (var prefetchRelation in prefetchRelations)
                {
                    var sqlJoin = new SqlJoinDefinition
                    {
                        Left = new SqlJoinPart(schema, prefetchRelation.LocalField, sqlQuerySpec.TableAlias),
                        Right = new SqlJoinPart(prefetchRelation.ForeignSchema, prefetchRelation.ForeignField, SqlNameGenerator.NextTableAlias()),
                        Type = SqlJoinType.LeftOuter
                    };

                    if (joins.Contains(sqlJoin))
                    {
                        sqlJoin = joins.First(j => j.Equals(sqlJoin));

                        sqlJoin.Type = SqlJoinType.LeftOuter;
                    }
                    else
                    {
                        joins.Add(sqlJoin);
                    }

                    fieldsByRelation[prefetchRelation] = prefetchRelation.ForeignSchema.FieldList.Select(f => new PrefetchFieldDefinition {Field = f, FieldAlias = SqlNameGenerator.NextFieldAlias(), TableAlias = sqlJoin.Right.Alias}).ToArray();
                    foreignKeyAliases[prefetchRelation] = fieldList.First(f => f.Field == prefetchRelation.LocalField).Alias;
                }

                string sql = SqlDialect.SelectSql(
                    new SqlTableNameWithAlias(schema.MappedName, sqlQuerySpec.TableAlias),
                    fieldList
                        .Select(field => new SqlColumnNameWithAlias(sqlQuerySpec.TableAlias + "." + field.Field.MappedName, field.Alias))
                        .Union(
                            fieldsByRelation.SelectMany(kv => kv.Value.Select(f => new SqlColumnNameWithAlias(f.TableAlias + "." + f.Field.MappedName, f.FieldAlias)))
                            )
                        ,
                    sqlQuerySpec.FilterSql,
                    joins,
                    sqlQuerySpec.SortExpressionSql,
                    sqlQuerySpec.Skip + 1,
                    sqlQuerySpec.Take
                    );

                var records = ExecuteSqlReader(sql, sqlQuerySpec.SqlParameters ?? null).ToArray();

                relatedEntities  = records.Select(
                    rec => prefetchRelations.ToDictionary(
                                relation => relation, 
                                relation => rec[foreignKeyAliases[relation]] == null ? (SerializedEntity)null : new SerializedEntity(fieldsByRelation[relation].ToDictionary(f => f.Field.MappedName, f => rec[f.FieldAlias].Convert(f.Field.FieldType)))
                            )
                        );

                return from record in records select new SerializedEntity(fieldList.ToDictionary(c => c.Field.MappedName, c => record[c.Alias].Convert(c.Field.FieldType)));
            }
            finally
            {
                SqlNameGenerator.Reset();
            }
        }

        public ObjectWriteResult WriteObject(SerializedEntity o, bool create, OrmSchema schema)
        {
            var result = new ObjectWriteResult();

            string tableName = schema.MappedName;
            var autoIncrementField = schema.IncrementKeys.FirstOrDefault();
            var columnList = (from f in schema.Fields.Values where !f.AutoIncrement select new { Field = f, ParameterName = SqlNameGenerator.NextParameterName()  }).ToArray();
            var parameters = new QueryParameterCollection(columnList.ToDictionary(c => c.ParameterName, c => o[c.Field.MappedName]));

            string sql;

            if (create)
            {
                sql = SqlDialect.InsertSql(
                                    tableName, 
                                    columnList.Select(c => c.Field.MappedName), columnList.Select(c => SqlDialect.CreateParameterExpression(c.ParameterName))
                                    );


                

                if (autoIncrementField != null)
                {
                    object autoIncrementValue = null;

                    if (RequiresAutoIncrementGetInSameStatement)
                    {
                        string fieldAlias = SqlNameGenerator.NextFieldAlias();

                        sql += ';' + SqlDialect.GetLastAutoincrementIdSql(autoIncrementField.MappedName, fieldAlias, schema.MappedName);

                        autoIncrementValue = ExecuteSqlReader(sql, parameters).FirstOrDefault()[fieldAlias];
                    }
                    else
                    {
                        ExecuteSql(sql, parameters);
                        autoIncrementValue = GetLastAutoIncrementValue(schema);
                    }

                    o[autoIncrementField.MappedName] = autoIncrementValue.Convert(autoIncrementField.FieldType);
                    result.OriginalUpdated = true;
                }
                else
                {
                    ExecuteSql(sql, parameters);
                }

                result.Added = true;
            }
            else
            {
                if (columnList.Length > 0 && schema.PrimaryKeys.Length > 0)
                {
                    var pkParameters = schema.PrimaryKeys.Select(pk => new KeyValuePair<string,OrmSchema.Field>(SqlNameGenerator.NextParameterName(), pk)).ToArray();

                    foreach (var primaryKey in pkParameters)
                        parameters[primaryKey.Key] = o[primaryKey.Value.MappedName];

                    sql = SqlDialect.UpdateSql(
                                        new SqlTableNameWithAlias(tableName),
                                        columnList.Select(c => new Tuple<string, string>(c.Field.MappedName, SqlDialect.CreateParameterExpression(c.ParameterName))),
                                        String.Join(" AND ", pkParameters.Select(pk => SqlDialect.QuoteField(pk.Value.MappedName) + "=" + SqlDialect.CreateParameterExpression(pk.Key)))
                                    );

                    ExecuteSql(sql, parameters);
                }
                else
                {
                    result.Success = false;
                }
            }

            result.Success = true;

            SqlNameGenerator.Reset();

            return result;
        }

        public SerializedEntity ReadObject(object[] keys, OrmSchema schema)
        {
            string tableName = schema.MappedName;
            var columnList = (from f in schema.Fields.Values select new { Field = f, Alias = SqlNameGenerator.NextFieldAlias() }).ToArray();
            var keyList = (from f in schema.PrimaryKeys select new {Field = f, ParameterName = SqlNameGenerator.NextParameterName()}).ToArray();
            var parameters = Enumerable.Range(0, keyList.Length).ToDictionary(i => keyList[i].ParameterName, i => keys[i]);

            string sql = SqlDialect.SelectSql(
                                        new SqlTableNameWithAlias(tableName), 
                                        columnList.Select(c => new SqlColumnNameWithAlias(c.Field.MappedName, c.Alias)),
                                        string.Join(" AND ", keyList.Select(k => SqlDialect.QuoteField(k.Field.MappedName) + "=" + SqlDialect.CreateParameterExpression(k.ParameterName)))
                                        );

            var record = ExecuteSqlReader(sql, new QueryParameterCollection(parameters)).FirstOrDefault();

            SqlNameGenerator.Reset();

            if (record == null)
                return null;

            return new SerializedEntity(columnList.ToDictionary(c => c.Field.MappedName , c => record[c.Alias].Convert(c.Field.FieldType)));
        }

        public bool DeleteObject(SerializedEntity o, OrmSchema schema)
        {
            string tableName = schema.MappedName;
            var keyList = (from f in schema.PrimaryKeys select new { Field = f, ParameterName = SqlNameGenerator.NextParameterName() }).ToArray();
            var parameters = keyList.ToDictionary(key => key.ParameterName, key => o[key.Field.MappedName]);

            string sql = SqlDialect.DeleteSql(
                                        new SqlTableNameWithAlias(tableName),
                                        string.Join(" AND ", keyList.Select(k => SqlDialect.QuoteField(k.Field.MappedName) + "=" + SqlDialect.CreateParameterExpression(k.ParameterName)))
                                        );

            var result = ExecuteSql(sql, new QueryParameterCollection(parameters));

            SqlNameGenerator.Reset();

            return result > 0;
        }

        public bool DeleteObjects(INativeQuerySpec filter, OrmSchema schema)
        {
            string tableName = schema.MappedName;
            var querySpec = (SqlQuerySpec) filter;

            string sql = SqlDialect.DeleteSql(new SqlTableNameWithAlias(tableName, querySpec.TableAlias),querySpec.FilterSql);

            var result = ExecuteSql(sql, querySpec.SqlParameters);

            SqlNameGenerator.Reset();

            return result > 0;
        }

        
        public QuerySpec CreateQuerySpec(FilterSpec filterSpec, ScalarSpec scalarSpec, SortOrderSpec sortSpec, int? skip, int? take, OrmSchema schema)
        {
            var tableAlias = SqlNameGenerator.NextTableAlias();

            SqlExpressionTranslator sqlTranslator = new SqlExpressionTranslator(SqlDialect, schema, tableAlias);

//            SqlExpressionTranslator sqlTranslator = new SqlExpressionTranslator(SqlDialect,schema,tableAlias);

            string filterSql = null;

            CodeQuerySpec codeQuerySpec = null;

            if (filterSpec != null)
            {
                // We split the translatable and non-translatable expressions
                var translationResults = filterSpec.Expressions.Select(e => new {Expression = e, Sql = sqlTranslator.Translate(e)}).ToLookup(result => result.Sql != null);

                filterSql = string.Join(" AND ", translationResults[true].Where(result => result.Sql != null).Select(result => "(" + result.Sql + ")"));

                if (translationResults[false].Any())
                {
                    codeQuerySpec = new CodeQuerySpec();

                    foreach (var result in translationResults[false])
                        codeQuerySpec.AddFilter(schema, result.Expression);
                }
            }
            
            string expressionSql = null;

            if (scalarSpec != null)
            {
                expressionSql = sqlTranslator.Translate(scalarSpec.Expression);
            }

            string sortSql = null;

            if (sortSpec != null)
            {
                var sqlParts = sortSpec.Expressions.Select(e => sqlTranslator.Translate(e.Expression) + (e.SortOrder == SortOrder.Descending ? " DESC" : ""));

                sortSql = string.Join(",",sqlParts);
            }

            var sqlQuery = new SqlQuerySpec
            {
                FilterSql = filterSql,
                ExpressionSql = expressionSql,
                SqlParameters = sqlTranslator.SqlParameters,
                Joins = sqlTranslator.Joins,
                TableAlias = tableAlias,
                SortExpressionSql = sortSql,
                Skip = skip,
                Take = take
            };

            return new QuerySpec(codeQuerySpec, sqlQuery);
        }
        
        public bool SupportsQueryTranslation(QueryExpression expression)
        {
            return true;
        }

        public bool SupportsRelationPrefetch
        {
            get { return true; }
        }

        public abstract bool RequiresAutoIncrementGetInSameStatement { get; }

        public virtual bool CreateOrUpdateTable(OrmSchema schema, bool recreateTable, bool recreateIndexes)
        {
            SqlDialect.CreateOrUpdateTable(schema, recreateTable, recreateIndexes, this);

            return true;
        }

        public abstract int ExecuteSql(string sql, QueryParameterCollection parameters);
        public abstract IEnumerable<Dictionary<string, object>> ExecuteSqlReader(string sql, QueryParameterCollection parameters);

        public virtual IEnumerable<SerializedEntity> Query(string sql, QueryParameterCollection parameters)
        {
            return ExecuteSqlReader(sql, parameters).Select(rec => new SerializedEntity(rec));
        }

        public virtual object QueryScalar(string sql, QueryParameterCollection parameters)
        {
            var result = ExecuteSqlReader(sql, parameters).FirstOrDefault();

            if (result != null)
                return result.First().Value;
            else
                return null;
        }

        public virtual void Purge(OrmSchema schema)
        {
            ExecuteSql(SqlDialect.TruncateTableSql(schema.MappedName), null);

            SqlNameGenerator.Reset();
        }

        public virtual long GetLastAutoIncrementValue(OrmSchema schema)
        {
            var autoIncrementField = schema.IncrementKeys.FirstOrDefault();
            var fieldAlias = SqlNameGenerator.NextFieldAlias();

            string sql = SqlDialect.GetLastAutoincrementIdSql(autoIncrementField.MappedName, fieldAlias, schema.MappedName);

            var sqlResult = ExecuteSqlReader(sql, null).FirstOrDefault();

            return sqlResult[fieldAlias].Convert<long>();
        }

        public abstract void Dispose();
    }
}