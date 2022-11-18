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
using Iridium.Reflection;

namespace Iridium.DB
{
    public abstract class SqlDataProvider<T> : SqlDataProvider where T : SqlDialect, new()
    {
        protected SqlDataProvider() : base(new T())
        {
        }
    }

    public abstract class SqlDataProvider : IDataProvider, ISqlDataProvider
    {
        public SqlDialect SqlDialect { get; }
        public ISqlLogger SqlLogger { get; set; }

        protected SqlDataProvider(SqlDialect sqlDialect)
        {
            SqlDialect = sqlDialect;
        }

        public object GetScalar(Aggregate aggregate, INativeQuerySpec nativeQuerySpec, TableSchema schema)
        {
            var querySpec = (SqlQuerySpec) nativeQuerySpec;

            string expressionSql;

            int? limit = null;

            switch (aggregate)
            {
                case Aggregate.Sum:
                    expressionSql = SqlDialect.SqlFunction(SqlDialect.Function.Sum, querySpec.ExpressionSql);
                    break;
                case Aggregate.Average:
                    expressionSql = SqlDialect.SqlFunction(SqlDialect.Function.Average, querySpec.ExpressionSql);
                    break;
                case Aggregate.Max:
                    expressionSql = SqlDialect.SqlFunction(SqlDialect.Function.Max, querySpec.ExpressionSql);
                    break;
                case Aggregate.Min:
                    expressionSql = SqlDialect.SqlFunction(SqlDialect.Function.Min, querySpec.ExpressionSql);
                    break;
                case Aggregate.Count:
                    expressionSql = SqlDialect.SqlFunction(SqlDialect.Function.Count, "*");
                    break;
                case Aggregate.Any:
                    expressionSql = "1";
                    limit = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(aggregate));
            }

            var valueAlias = SqlNameGenerator.NextFieldAlias();

            string sql = SqlDialect.SelectSql(
                new SqlTableNameWithAlias(schema.MappedName, querySpec.TableAlias), 
                new[] { new SqlExpressionWithAlias(expressionSql,valueAlias) },
                querySpec.FilterSql, 
                querySpec.Joins,
                querySpec.SortExpressionSql,
                querySpec.Skip + 1,
                limit ?? querySpec.Take
                );

            var record = ExecuteSqlReader(sql, querySpec.SqlParameters).FirstOrDefault();

            SqlNameGenerator.Reset();

            if (aggregate == Aggregate.Any)
                return record != null;
            
            return record[valueAlias];
        }

        // public IEnumerable<SerializedEntity> GetObjects(INativeQuerySpec filter, TableSchema schema, ProjectionInfo projection)
        // {
        //     try
        //     {
        //         SqlQuerySpec sqlQuerySpec = ((SqlQuerySpec) filter) ?? new SqlQuerySpec {FilterSql = null, TableAlias = SqlNameGenerator.NextTableAlias()};
        //
        //         var fieldList = (from f in projection.Fields(schema) select new { Field = f, Alias = SqlNameGenerator.NextFieldAlias() }).ToArray();
        //
        //         var columns = fieldList.Select(field => new SqlColumnNameWithAlias(sqlQuerySpec.TableAlias + "." + field.Field.MappedName, field.Alias)).ToArray();
        //
        //         string sql = SqlDialect.SelectSql(
        //             new SqlTableNameWithAlias(schema.MappedName, sqlQuerySpec.TableAlias),
        //             columns,
        //             sqlQuerySpec.FilterSql,
        //             sqlQuerySpec.Joins,
        //             sqlQuerySpec.SortExpressionSql,
        //             sqlQuerySpec.Skip + 1,
        //             sqlQuerySpec.Take,
        //             distinct: projection?.Distinct ?? false
        //             );
        //
        //         return from record in ExecuteSqlReader(sql, sqlQuerySpec.SqlParameters)
        //             select new SerializedEntity(fieldList.ToDictionary(c => c.Field.MappedName, c => record[c.Alias].Convert(c.Field.FieldType)));
        //     }
        //     finally
        //     {
        //         SqlNameGenerator.Reset();
        //     }
        // }

        private class PrefetchFieldDefinition
        {
            public string TableAlias;
            public string FieldAlias;
            public TableSchema.Field Field;
        }
        
        public IEnumerable<SerializedEntity> GetObjects(INativeQuerySpec filter, TableSchema schema, ProjectionInfo projection, IEnumerable<TableSchema.Relation> prefetchRelations, out IEnumerable<Dictionary<TableSchema.Relation, SerializedEntity>> relatedEntities)
        {
            try
            {
                SqlQuerySpec sqlQuerySpec = ((SqlQuerySpec)filter) ?? new SqlQuerySpec { FilterSql = null, TableAlias = SqlNameGenerator.NextTableAlias() };

                var projectionRelations = new ReadOnlySet<TableSchema.Relation>(projection?.RelationsReferenced);
                var toOneRelations = new ReadOnlySet<TableSchema.Relation>(prefetchRelations?.Where(r => r.IsToOne && r.LocalSchema == schema));
                var toManyRelations = new ReadOnlySet<TableSchema.Relation>(prefetchRelations?.Where(r => !r.IsToOne && r.LocalSchema == schema));

                var joinRelations = projectionRelations.Union(toOneRelations);

                var projectionFields = new HashSet<TableSchema.Field>(projection.Fields(schema));
                var rootFields = new ReadOnlySet<TableSchema.Field>(projectionFields).Intersection(schema.Fields);

                var fieldList = (from f in rootFields select new { Field = f, Alias = SqlNameGenerator.NextFieldAlias() }).ToList();

                var joins = new HashSet<SqlJoinDefinition>(sqlQuerySpec.Joins);
                var fieldsByRelation = new Dictionary<TableSchema.Relation, PrefetchFieldDefinition[]>();
                var foreignKeyAliases = new Dictionary<TableSchema.Relation, string>();

                foreach (var joinRelation in joinRelations)
                {
                    var sqlJoin = new SqlJoinDefinition
                    (
                        new SqlJoinPart(schema, joinRelation.LocalField, sqlQuerySpec.TableAlias),
                        new SqlJoinPart(joinRelation.ForeignSchema, joinRelation.ForeignField, SqlNameGenerator.NextTableAlias()),
                        SqlJoinType.LeftOuter
                    );

                    if (joins.Contains(sqlJoin))
                    {
                        sqlJoin = joins.First(j => j.Equals(sqlJoin));

                        sqlJoin.Type = SqlJoinType.LeftOuter;
                    }
                    else
                    {
                        joins.Add(sqlJoin);
                    }

                    var foreignFields = new HashSet<TableSchema.Field>(joinRelation.ForeignSchema.Fields);

                    if (projection != null && !toOneRelations.Contains(joinRelation))
                    {
                        foreignFields.IntersectWith(projectionFields);
                        foreignFields.Add(joinRelation.ForeignField);
                    }

                    var relationFields = foreignFields.Select(f => new PrefetchFieldDefinition {Field = f, FieldAlias = SqlNameGenerator.NextFieldAlias(), TableAlias = sqlJoin.Right.Alias}).ToArray();

                    fieldsByRelation[joinRelation] = relationFields;
                    foreignKeyAliases[joinRelation] = relationFields.First(f => f.Field == joinRelation.ForeignField).FieldAlias;
                }

                var columns = fieldList
                    .Select(field => new SqlColumnNameWithAlias(sqlQuerySpec.TableAlias + "." + field.Field.MappedName, field.Alias))
                    .Union(
                        fieldsByRelation.SelectMany(kv => kv.Value.Select(f => new SqlColumnNameWithAlias(f.TableAlias + "." + f.Field.MappedName, f.FieldAlias)))
                    );

                string sql = SqlDialect.SelectSql(
                    new SqlTableNameWithAlias(schema.MappedName, sqlQuerySpec.TableAlias),
                    columns,
                    sqlQuerySpec.FilterSql,
                    joins,
                    sqlQuerySpec.SortExpressionSql,
                    sqlQuerySpec.Skip + 1,
                    sqlQuerySpec.Take,
                    distinct: projection?.Distinct ?? false
                    );

                var records = ExecuteSqlReader(sql, sqlQuerySpec.SqlParameters ?? null).ToArray();
                
                if (joinRelations.Any())
                {
                    relatedEntities = records.Select(
                        rec => joinRelations.ToDictionary(
                            relation => relation,
                            relation => rec[foreignKeyAliases[relation]] == null ? // checks if related record was found
                                null
                                :
                                new SerializedEntity(fieldsByRelation[relation].ToDictionary(f => f.Field.MappedName, f => rec[f.FieldAlias].Convert(f.Field.FieldType)))
                        )
                    ).ToList();
                }
                else
                {
                    relatedEntities = null;
                }

                return from record in records select new SerializedEntity(fieldList.ToDictionary(c => c.Field.MappedName, c => record[c.Alias].Convert(c.Field.FieldType)));
            }
            finally
            {
                SqlNameGenerator.Reset();
            }
        }

        public ObjectWriteResult WriteObject(SerializedEntity o, bool? create, TableSchema schema)
        {
            var result = new ObjectWriteResult();

            var tableName = schema.MappedName;
            var autoIncrementField = schema.IncrementKey;
            var columnList = (from f in schema.WriteFields select new { Field = f, ParameterName = SqlNameGenerator.NextParameterName()  }).ToArray();
            var parameters = new QueryParameterCollection(columnList.Select(c => new QueryParameter(c.ParameterName, o[c.Field.MappedName],c.Field.FieldType)));

            if (autoIncrementField != null && create == null) 
                throw new NotSupportedException("INT.ERR.27");

            string sql;

            if (create ?? false)
            {
                sql = SqlDialect.InsertSql(
                                    tableName, 
                                    columnList.Select(c => new StringPair(c.Field.MappedName,SqlDialect.CreateParameterExpression(c.ParameterName))).ToArray()
                                    );

                if (autoIncrementField != null)
                {
                    object autoIncrementValue;

                    if (SqlDialect.RequiresAutoIncrementGetInSameStatement)
                    {
                        string fieldAlias = SqlNameGenerator.NextFieldAlias();

                        sql += ';' + SqlDialect.GetLastAutoincrementIdSql(autoIncrementField.MappedName, fieldAlias, schema.MappedName);

                        autoIncrementValue = ExecuteSqlReader(sql, parameters).First()[fieldAlias];
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
                    var pkParameters = schema.PrimaryKeys.Select(
                        pk => new KeyValuePair<string,TableSchema.Field>(SqlNameGenerator.NextParameterName(), pk)
                        ).ToArray();

                    foreach (var primaryKey in pkParameters)
                        parameters[primaryKey.Key] = new QueryParameter(primaryKey.Key, o[primaryKey.Value.MappedName], primaryKey.Value.FieldType);

                    var sqlWhere = string.Join(" AND ", pkParameters.Select(pk => SqlDialect.QuoteField(pk.Value.MappedName) + "=" + SqlDialect.CreateParameterExpression(pk.Key)));
                    var fields = columnList.Select(c => new StringPair(c.Field.MappedName, SqlDialect.CreateParameterExpression(c.ParameterName))).ToArray();

                    if (create == null && !SqlDialect.SupportsInsertOrUpdate)
                        throw new NotSupportedException("InsertOrUpdate not supported by data provider");

                    if (create == null)
                    {
                        sql = SqlDialect.InsertOrUpdateSql(
                            tableName,
                            fields,
                            schema.PrimaryKeys.Select(pk => pk.MappedName).ToArray(),
                            sqlWhere
                        );
                    }
                    else
                    {
                        sql = SqlDialect.UpdateSql(
                            tableName, 
                            fields, 
                            schema.PrimaryKeys.Select(pk => pk.MappedName).ToArray(), 
                            sqlWhere
                            );
                    }

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

        public SerializedEntity ReadObject(Dictionary<string,object> keys, TableSchema schema)
        {
            string tableName = schema.MappedName;
            var columnList = (from f in schema.FieldsByFieldName.Values select new { Field = f, Alias = SqlNameGenerator.NextFieldAlias() }).ToArray();
            var keyList = (from f in schema.PrimaryKeys select new {Field = f, ParameterName = SqlNameGenerator.NextParameterName()}).ToArray();
            //var parameters = keyList.ToDictionary(key => key.ParameterName, key => keys[key.Field.MappedName]);

            string sql = SqlDialect.SelectSql(
                                        new SqlTableNameWithAlias(tableName), 
                                        columnList.Select(c => new SqlColumnNameWithAlias(c.Field.MappedName, c.Alias)),
                                        string.Join(" AND ", keyList.Select(k => SqlDialect.QuoteField(k.Field.MappedName) + "=" + SqlDialect.CreateParameterExpression(k.ParameterName)))
                                        );

            var record = ExecuteSqlReader(sql, new QueryParameterCollection(keyList.Select(k => new QueryParameter(k.ParameterName,keys[k.Field.MappedName],k.Field.FieldType)))).FirstOrDefault();

            SqlNameGenerator.Reset();

            if (record == null)
                return null;

            return new SerializedEntity(columnList.ToDictionary(c => c.Field.MappedName , c => ConvertValue(record[c.Alias], c.Field.FieldType)));
        }

        private static object ConvertValue(object value, Type type)
        {
            if (type == typeof(Guid) && value is string s)
            {
                if (s.Length == 32 && Guid.TryParseExact(s, "N", out var guidValue))
                    return guidValue;
                else if (s.Length == 36 && Guid.TryParseExact(s, "D", out guidValue))
                    return guidValue;

                return null;
            }

            return value.Convert(type);
        }

        public bool DeleteObject(SerializedEntity o, TableSchema schema)
        {
            string tableName = schema.MappedName;
            var keyList = (from f in schema.PrimaryKeys select new { Field = f, ParameterName = SqlNameGenerator.NextParameterName() }).ToArray();
            var parameters = keyList.Select(key => new QueryParameter(key.ParameterName, o[key.Field.MappedName], key.Field.FieldType));

            string sql = SqlDialect.DeleteSql(
                                        new SqlTableNameWithAlias(tableName),
                                        string.Join(" AND ", keyList.Select(k => SqlDialect.QuoteField(k.Field.MappedName) + "=" + SqlDialect.CreateParameterExpression(k.ParameterName))),
                                        null
                                        );

            var result = ExecuteSql(sql, new QueryParameterCollection(parameters));

            SqlNameGenerator.Reset();

            return result > 0;
        }

        public bool DeleteObjects(INativeQuerySpec filter, TableSchema schema)
        {
            string tableName = schema.MappedName;
            var querySpec = (SqlQuerySpec) filter;

            string sql = SqlDialect.DeleteSql(new SqlTableNameWithAlias(tableName, querySpec.TableAlias),querySpec.FilterSql,querySpec.Joins);

            var result = ExecuteSql(sql, querySpec.SqlParameters);

            SqlNameGenerator.Reset();

            return result > 0;
        }

        
        public QuerySpec CreateQuerySpec(FilterSpec filterSpec, ScalarSpec scalarSpec, SortOrderSpec sortSpec, ProjectionSpec projectionSpec, int? skip, int? take, TableSchema schema)
        {
            var tableAlias = SqlNameGenerator.NextTableAlias();

            SqlExpressionTranslator sqlTranslator = new SqlExpressionTranslator(SqlDialect, schema, tableAlias);

            string filterSql = null;

            CodeQuerySpec codeQuerySpec = null;

            if (filterSpec != null)
            {
                // We split the translatable and non-translatable expressions
                var translationResults = filterSpec.Expressions.Select(e => new {Expression = e, Sql = sqlTranslator.Translate(e)}).ToLookup(result => result.Sql != null);

                filterSql = string.Join(" AND ", translationResults[true]/*.Where(result => result.Sql != null)*/.Select(result => $"({result.Sql})"));

                if (translationResults[false].Any())
                {
                    codeQuerySpec = new CodeQuerySpec();

                    foreach (var result in translationResults[false])
                        codeQuerySpec.AddFilter(schema, result.Expression);
                }
            }
            
            string expressionSql = null;
            ProjectionInfo projectionInfo = null;

            if (scalarSpec != null)
            {
                expressionSql = sqlTranslator.Translate(scalarSpec.Expression);
            }

            if (projectionSpec != null)
            {
                var projectionFinder = new ProjectionExpressionParser(schema);

                projectionInfo = projectionFinder.FindFields(((LambdaQueryExpression)projectionSpec.Expression).Expression);
                projectionInfo.Distinct = projectionSpec.Distinct;
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

            // if we have a skip or take with a combined SQL/code query, only do the take and skip in code
            if (codeQuerySpec != null)
            {
                codeQuerySpec.Skip = skip;
                codeQuerySpec.Take = take;
                sqlQuery.Skip = null;
                sqlQuery.Take = null;
            }

            return new QuerySpec(codeQuerySpec, sqlQuery, projectionInfo);
        }
        
        public bool SupportsQueryTranslation(QueryExpression expression)
        {
            return true;
        }

        public bool SupportsRelationPrefetch => true;
        public virtual bool SupportsTransactions => true;
        public bool SupportsSql => true;

        public virtual bool CreateOrUpdateTable(TableSchema schema, bool recreateTable, bool recreateIndexes)
        {
            SqlDialect.CreateOrUpdateTable(schema, recreateTable, recreateIndexes, this);

            return true;
        }

        public int SqlProcedure(string procName, QueryParameterCollection parameters)
        {
            return ExecuteProcedure(procName, parameters);
        }

        public int SqlNonQuery(string sql, QueryParameterCollection parameters = null)
        {
            return ExecuteSql(sql, parameters);
        }

        public abstract int ExecuteProcedure(string procName, QueryParameterCollection parameters = null);
        public abstract int ExecuteSql(string sql, QueryParameterCollection parameters = null);
        public abstract IEnumerable<Dictionary<string, object>> ExecuteSqlReader(string sql, QueryParameterCollection parameters);

        public virtual IEnumerable<SerializedEntity> SqlQuery(string sql, QueryParameterCollection parameters = null)
        {
            return ExecuteSqlReader(sql, parameters).Select(rec => new SerializedEntity(rec));
        }

        public virtual IEnumerable<object> SqlQueryScalar(string sql, QueryParameterCollection parameters = null)
        {
            var results = ExecuteSqlReader(sql, parameters);

            return results?.Select(r => r.First().Value);
        }

        public abstract void BeginTransaction(IsolationLevel isolationLevel);
        public abstract void CommitTransaction();
        public abstract void RollbackTransaction();

        public virtual void Purge(TableSchema schema)
        {
            ExecuteSql(SqlDialect.TruncateTableSql(schema.MappedName));

            SqlNameGenerator.Reset();
        }

        public virtual long GetLastAutoIncrementValue(TableSchema schema)
        {
            var autoIncrementField = schema.IncrementKey;
            var fieldAlias = SqlNameGenerator.NextFieldAlias();

            string sql = SqlDialect.GetLastAutoincrementIdSql(autoIncrementField.MappedName, fieldAlias, schema.MappedName);

            var sqlResult = ExecuteSqlReader(sql, null).First();

            return sqlResult[fieldAlias].Convert<long>();
        }

        public abstract void Dispose();
    }
}