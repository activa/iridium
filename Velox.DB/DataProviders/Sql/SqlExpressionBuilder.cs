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
    internal class SqlExpressionBuilder
    {
        private static readonly HashSet<Type> _nativeTypes = new HashSet<Type>(new[]
        {
            typeof(Boolean), typeof(Byte), typeof(SByte), typeof(UInt16), typeof(UInt32), typeof(UInt64), typeof(Int16), typeof(Int32), typeof(Int64), typeof(Single), typeof(Double), typeof(Decimal), typeof(string), typeof(DateTime)
        });

        public class ExpressionMetaData
        {
            public object Iterator;
            public OrmSchema.Relation Relation;
            public OrmSchema Schema;

            public object Key { get { return (object)Relation ?? Schema; } }
        }

        public class SubQuery
        {
            public string Sql = "";
            public readonly HashSet<SqlJoinDefinition> Joins = new HashSet<SqlJoinDefinition>();
        }

        private readonly SafeDictionary<object, ExpressionMetaData> _metaData = new SafeDictionary<object, ExpressionMetaData>();
        private readonly Dictionary<object, Dictionary<object, string>> _relationAliases = new Dictionary<object, Dictionary<object, string>>();
        private readonly QueryParameterCollection _sqlParameters = new QueryParameterCollection();
        private readonly SqlDialect _sqlDialect;
        private readonly Stack<SubQuery> _subQueries = new Stack<SubQuery>();
        private  object _rootIterator;
        private readonly Dictionary<object, object> _rootIterators = new Dictionary<object, object>();

        private object GetRootIterator(object iterator) // TODO: needs better name
        {
            return _rootIterators.ContainsKey(iterator) ? _rootIterators[iterator] : iterator;
        }

        private SubQuery CurrentQuery
        {
            get { return _subQueries.Peek(); }
        }

        public QueryParameterCollection SqlParameters
        {
            get { return _sqlParameters; }
        }

        public string SqlExpression
        {
            get { return CurrentQuery.Sql; }
        }

        public HashSet<SqlJoinDefinition> Joins
        {
            get { return CurrentQuery.Joins; }
        }

        public SqlExpressionBuilder(SqlDialect sqlDialect)
        {
            _sqlDialect = sqlDialect;

            _subQueries.Push(new SubQuery());
        }

        public void PrepareForNewExpression(object rootIterator, string tableAlias, OrmSchema schema)
        {
            CurrentQuery.Sql = "";

            if (_rootIterator == null)
            {
                _rootIterator = rootIterator;

                _relationAliases[rootIterator] = new Dictionary<object, string> {{schema, tableAlias}};
            }

            _metaData[rootIterator] = new ExpressionMetaData
            {
                Relation = null,
                Iterator = rootIterator,
                Schema = schema
            };

            _rootIterators[rootIterator] = _rootIterator; // make additional root iterators equivalent to first one
        }

        public void AppendSql(string s)
        {
            CurrentQuery.Sql += s;
        }

        public void AppendVariableName(string s)
        {
            CurrentQuery.Sql += _sqlDialect.CreateParameterExpression(s);
        }

        public bool ProcessMember(object fullExpression, object leftExpression, string memberName)
        {
            var parentMetaData = _metaData[leftExpression];

            if (parentMetaData != null)
            {
                if (_metaData.ContainsKey(fullExpression))
                    return true; // relation already visited

                var iterator = GetRootIterator(parentMetaData.Iterator);

                var relation = parentMetaData.Schema.Relations[memberName];
                var leftAlias = _relationAliases[iterator][parentMetaData.Key];

                if (relation != null && relation.RelationType == OrmSchema.RelationType.ManyToOne)
                {
                    if (!_relationAliases[iterator].ContainsKey(relation))
                    {
                        var sqlJoin = new SqlJoinDefinition
                        {
                            Left = new SqlJoinPart(parentMetaData.Schema, relation.LocalField, leftAlias),
                            Right = new SqlJoinPart(relation.ForeignSchema, relation.ForeignField, SqlNameGenerator.NextTableAlias()),
                            Type = SqlJoinType.Inner
                        };

                        CurrentQuery.Joins.Add(sqlJoin);

                        _relationAliases[iterator][relation] = sqlJoin.Right.Alias;
                    }

                    _metaData[fullExpression] = new ExpressionMetaData { Iterator = iterator, Relation = relation, Schema = relation.ForeignSchema };
                }
                else if (relation == null)
                {
                    CurrentQuery.Sql += _sqlDialect.QuoteField(leftAlias + "." + memberName);
                }

                return true;
            }

            return false;
        }

        public bool ProcessConstant(Type type, object value, bool createParameter)
        {
            if (_nativeTypes.Contains(type.Inspector().RealType))
            {
                if (createParameter)
                {
                    var parameterName = SqlNameGenerator.NextParameterName();

                    SqlParameters[parameterName] = value;

                    CurrentQuery.Sql += _sqlDialect.CreateParameterExpression(parameterName);
                }
                else
                {
                    if (value is string)
                        CurrentQuery.Sql += "'" + value + "'";
                    else
                        CurrentQuery.Sql += value.ToString();
                }
                return true;
            }

            return false;
        }

        public OrmSchema.Relation GetRelation(object expression, string memberName)
        {
            return _metaData[expression].Schema.Relations[memberName];
        }

        private static readonly SafeDictionary<string,string> _toManyTemplates = new SafeDictionary<string, string>()
        {
            {"Any", "exists (select * from {1} {2} where {3})"},
            {"Count", "(select count(*) from {1} {2} where {3})"},
            {"Sum","(select sum({0}) from {1} {2} where {3})"},
            {"Average","(select avg({0}) from {1} {2} where {3})"}
        };

        public void OneToMany(OrmSchema.Relation relation, object iterator, string functionName, Action fieldVisitor, Action filterVisitor)
        {
            var template = _toManyTemplates[functionName];

            if (template == null)
                throw new NotSupportedException(functionName);

            var alias = SqlNameGenerator.NextTableAlias();

            _metaData[iterator] = new ExpressionMetaData { Iterator = iterator, Relation = relation, Schema = relation.ForeignSchema };

            _relationAliases[iterator] = new Dictionary<object, string> { { relation, alias} };

            _subQueries.Push(new SubQuery());

                fieldVisitor();

                string sqlFields = CurrentQuery.Sql;

                CurrentQuery.Sql = "";

                filterVisitor();

                string sqlWhere = CurrentQuery.Sql;
                string sqlJoins = (CurrentQuery.Joins.Count > 0) ? string.Join(" ", CurrentQuery.Joins.Select(join => join.ToSql(_sqlDialect))) : null;

            _subQueries.Pop();

            CurrentQuery.Sql += string.Format(template, 
                                                sqlFields, 
                                                _sqlDialect.QuoteTable(relation.ForeignSchema.MappedName) + " " + alias, sqlJoins ?? "", 
                                                sqlWhere
                                             );
        }


        public void AppendFunctionName(SqlDialect.Function function)
        {
            AppendSql(_sqlDialect.SqlFunctionName(function));
        }
    }
}