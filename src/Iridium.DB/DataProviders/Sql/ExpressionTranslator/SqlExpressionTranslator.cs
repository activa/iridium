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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Iridium.Reflection;

namespace Iridium.DB
{
    internal sealed class SqlExpressionTranslator
    {
        private class ExpressionMetaData
        {
            public ParameterExpression Iterator;
            public TableSchema.Relation Relation;
            public TableSchema Schema;

            public object Key => (object)Relation ?? Schema;
        }

        private class SubQuery
        {
            public readonly HashSet<SqlJoinDefinition> Joins = new HashSet<SqlJoinDefinition>();
        }

        private readonly SafeDictionary<Expression, ExpressionMetaData> _metaData = new();
        private readonly Dictionary<ParameterExpression, Dictionary<object, string>> _relationAliases = new();
        private readonly Stack<SubQuery> _subQueries = new();
        private ParameterExpression _rootIterator;
        private readonly Dictionary<ParameterExpression, ParameterExpression> _rootIterators = new();
        private readonly SqlDialect _sqlDialect;
        private readonly string _tableAlias;
        private readonly TableSchema _schema;

        public SqlExpressionTranslator(SqlDialect sqlDialect, TableSchema schema, string tableAlias)
        {
            _schema = schema;
            _tableAlias = tableAlias;
            _sqlDialect = sqlDialect;

            _subQueries.Push(new SubQuery());
        }

        private ParameterExpression GetRootIterator(ParameterExpression iterator) // TODO: needs better name
        {
            return _rootIterators.ContainsKey(iterator) ? _rootIterators[iterator] : iterator;
        }

        private SubQuery CurrentQuery => _subQueries.Peek();

        public QueryParameterCollection SqlParameters { get; } = new();

        public HashSet<SqlJoinDefinition> Joins => CurrentQuery.Joins;

        private void PrepareForNewExpression(ParameterExpression rootIterator, string tableAlias, TableSchema schema)
        {
            if (_rootIterator == null)
            {
                _rootIterator = rootIterator;

                _relationAliases[rootIterator] = new Dictionary<object, string> { { schema, tableAlias } };
            }

            _metaData[rootIterator] = new ExpressionMetaData
            {
                Relation = null,
                Iterator = rootIterator,
                Schema = schema
            };

            _rootIterators[rootIterator] = _rootIterator; // make additional root iterators equivalent to first one
        }

        private TableSchema.Relation GetRelation(Expression expression, string memberName)
        {
            return _metaData[expression].Schema.Relations[memberName];
        }

        private string ProcessRelation(Expression fullExpression, Expression leftExpression, string memberName)
        {
            if (leftExpression.NodeType == ExpressionType.Convert)
                leftExpression = ((UnaryExpression) leftExpression).Operand;
                
            var parentMetaData = _metaData[leftExpression];

            if (parentMetaData != null)
            {
                if (_metaData.ContainsKey(fullExpression))
                    return null; // relation already visited

                var iterator = GetRootIterator(parentMetaData.Iterator);

                var relation = parentMetaData.Schema.Relations[memberName];
                var leftAlias = _relationAliases[iterator][parentMetaData.Key];

                if (relation is { IsToOne: true })
                {
                    if (!_relationAliases[iterator].ContainsKey(relation))
                    {
                        var sqlJoin = new SqlJoinDefinition(
                            new SqlJoinPart(parentMetaData.Schema, relation.LocalField, leftAlias),
                            new SqlJoinPart(relation.ForeignSchema, relation.ForeignField, SqlNameGenerator.NextTableAlias())
                            );

                        CurrentQuery.Joins.Add(sqlJoin);

                        _relationAliases[iterator][relation] = sqlJoin.Right.Alias;
                    }

                    _metaData[fullExpression] = new ExpressionMetaData { Iterator = iterator, Relation = relation, Schema = relation.ForeignSchema };
                }
                else if (relation == null)
                {
					return leftAlias + "." + (_schema.FieldsByFieldName[memberName]?.MappedName ?? memberName);
                }
            }

            return null;
        }

        public string Translate(QueryExpression queryExpression)
        {
            var lambda = ((LambdaQueryExpression) queryExpression).Expression;

            PrepareForNewExpression(lambda.Parameters[0], _tableAlias, _schema);

            try
            {
                return Translate(lambda);
            }
            catch (SqlExpressionTranslatorException)
            {
                return null; // we couldn't translate the given expression
            }
        }

        private string Translate(Expression expression)
        {
            if (expression == null)
                return null;

            expression = PartialEvaluator.Eval(expression);

            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    return TranslateMethodCall((MethodCallExpression) expression);

                case ExpressionType.Constant:
                    {
                        if (expression.Type.Inspector().Is(TypeFlags.Numeric | TypeFlags.Boolean | TypeFlags.String | TypeFlags.DateTime | TypeFlags.Enum))
                            return CreateParameter(((ConstantExpression) expression).Value, expression.Type);
                    }
                    break;

                case ExpressionType.ConvertChecked:
                case ExpressionType.Convert:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Unbox:
                    return TranslateUnary((UnaryExpression) expression);
                    
                case ExpressionType.MemberAccess:
                    return TranslateMember((MemberExpression)expression);
                    
                case ExpressionType.Parameter:
                    return null;

                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.AndAlso:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.OrElse:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return TranslateBinary((BinaryExpression)expression);

                case ExpressionType.Lambda:
                    return Translate(((LambdaExpression) expression).Body);
            }

            throw new SqlExpressionTranslatorException(expression.ToString());
        }

        private string TranslateUnary(UnaryExpression expression)
        {
            string sql = Translate(expression.Operand);

            switch (expression.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return $"(-{sql})";

                case ExpressionType.UnaryPlus:
                case ExpressionType.Unbox:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Quote:
                    return sql;

                case ExpressionType.Not:
                    return $"(NOT {sql})";
            }

            throw new SqlExpressionTranslatorException(expression.ToString());
        }

        private string TranslateMethodCall(MethodCallExpression node)
        {
            var methodName = node.Method.Name;
            var methodDeclaringType = node.Method.DeclaringType;
            MemberExpression leftExpression = null;
            var arguments = new List<LambdaExpression>();


            if (methodDeclaringType == typeof(string))
            {
                string arg = Translate(node.Object);
                
                object[] stringArguments = node.Arguments.Select(UnQuote).OfType<ConstantExpression>().Select(exp => exp.Value).ToArray();

                switch (methodName)
                {
                    case "StartsWith":
                        return $"({arg} like {CreateParameter(stringArguments[0] + "%", typeof(string))})";
                    case "EndsWith":
                        return $"({arg} like {CreateParameter("%" + stringArguments[0], typeof(string))})";
                    case "Contains":
                        return $"({arg} like {CreateParameter("%" + stringArguments[0] + "%", typeof(string))})";
                    case "Trim":
                        return string.Format(_sqlDialect.SqlFunction(SqlDialect.Function.Trim), stringArguments[0]);
                }

                throw new SqlExpressionTranslatorException(node.ToString());
            }

            if (methodDeclaringType == typeof(QueryExtensions) && node.Arguments.Count > 1)
            {
                string arg = Translate(node.Arguments[0]);

                switch (methodName)
                {
                    case nameof(QueryExtensions.IsAnyOf):
                    case nameof(QueryExtensions.IsNotAnyOf):
                    {
                        object argument = ExpressionEvaluator.Evaluate(node.Arguments.Skip(1).Select(UnQuote).First()).Value;

                        if (!(argument is IEnumerable enumerable))
                            throw new SqlExpressionTranslatorException(node.ToString());

                        var values = enumerable.Cast<object>().ToList();
                        var paramNames = values.Where(o => o != null).Select(o => CreateParameter(o)).ToList();

                        var inExpr = "";

                        if (methodName == nameof(QueryExtensions.IsNotAnyOf))
                        {
                            if (values.Any(v => v == null))
                                inExpr += $"{arg} is not null and ";

                            inExpr += $"{arg} not in ({string.Join(",", paramNames)})";
                        }
                        else
                        {
                            if (values.Any(v => v == null))
                                inExpr += $"{arg} is null or ";

                            inExpr += $"{arg} in ({string.Join(",", paramNames)})";
                        }

                        return $"({inExpr})";
                    }
                    case nameof(QueryExtensions.IsBetween):
                    {
                        object[] args = node.Arguments.Skip(1).Select(UnQuote).OfType<ConstantExpression>().Select(exp => exp.Value).ToArray();
                        
                        if (args.Length != 2)
                            throw new SqlExpressionTranslatorException(node.ToString());

                        var from = CreateParameter(args[0]);
                        var to = CreateParameter(args[1]);

                        return $"({arg} between {from} and {to})";
                    }
                }
            }

            if (methodDeclaringType.IsConstructedGenericType && methodDeclaringType.GetGenericTypeDefinition() == typeof(IDataSet<>))
            {
                leftExpression = node.Object as MemberExpression;
                arguments.AddRange(node.Arguments.Select(UnQuote).OfType<LambdaExpression>());
            }

            if (methodDeclaringType == typeof(Enumerable) && node.Arguments.Count > 0)
            {
                leftExpression = node.Arguments[0] as MemberExpression;
                arguments.AddRange(node.Arguments.Skip(1).Select(UnQuote).OfType<LambdaExpression>());
            }

            if (leftExpression != null)
            {
                switch (methodName)
                {
                    case "Any":
                    case "Count":
                    case "All":
                        return TranslateOneToMany(
                            methodName,
                            leftExpression,
                            null,
                            arguments.Count > 0 ? arguments[0] : null
                            );
                    case "Sum":
                    case "Avg":
                        return TranslateOneToMany(
                            methodName,
                            leftExpression,
                            arguments[0],
                            null
                            );
                }
            }

            throw new SqlExpressionTranslatorException(node.ToString());
        }

        private string TranslateTrue(string sql)
        {
            return "(" + sql + " <> " + Translate(Expression.Constant(false, typeof(bool))) + ")";
        }

        private string TranslateMember(MemberExpression node)
        {
            if (node.Expression.Type == typeof(string) && node.Member.Name == "Length")
            {
                string fnName = _sqlDialect.SqlFunction(SqlDialect.Function.StringLength, Translate(node.Expression));

                if (fnName != null)
                    return fnName;
            }

            if (node.Member.DeclaringType.IsConstructedGenericType && node.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>) && node.Member.Name == "Value")
            {
                return Translate(node.Expression);
            }

            if (Translate(node.Expression) != null)
                throw new SqlExpressionTranslatorException(node.ToString());

            string sql = ProcessRelation(node, node.Expression, node.Member.Name);

            if (sql != null)
            {
                var sqlField = _sqlDialect.QuoteField(sql);

                if (node.Type == typeof (bool))
                {
                    return TranslateTrue(sqlField);
                }
                else
                {
                    return sqlField;
                }
            }

            return null;
        }

        private string TranslateBinary(BinaryExpression expression)
        {
            if (expression.Right.NodeType == ExpressionType.Constant && (((ConstantExpression)expression.Right).Value == null))
            {
                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        return "(" + Translate(expression.Left) + " IS NULL)";
                    case ExpressionType.NotEqual:
                        return "(" + Translate(expression.Left) + " IS NOT NULL)";
                    default:
                        throw new SqlExpressionTranslatorException(expression.ToString());
                }
            }

            string op;

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    op = "+";
                    break;
                case ExpressionType.AndAlso:
                    op = "AND";
                    break;
                case ExpressionType.Coalesce:
                {
                    string sql = _sqlDialect.SqlFunction(SqlDialect.Function.Coalesce, Translate(expression.Left), Translate(expression.Right));

                    return expression.Type == typeof (bool) ? TranslateTrue(sql) : sql;
                }
                case ExpressionType.Divide:
                    op = "/";
                    break;
                case ExpressionType.Equal:
                    op = "=";
                    break;
                case ExpressionType.GreaterThan:
                    op = ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    op = ">=";
                    break;
                case ExpressionType.LessThan:
                    op = "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    op = "<=";
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    op = "*";
                    break;
                case ExpressionType.NotEqual:
                    op = "<>";
                    break;
                case ExpressionType.OrElse:
                    op = "OR";
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    op = "-";
                    break;
                default:
                    return null;
            }

            return $"({Translate(expression.Left)} {op} {Translate(expression.Right)})";
        }

        private static Expression UnQuote(Expression expression)
        {
            return expression.NodeType == ExpressionType.Quote ? ((UnaryExpression)expression).Operand : expression;
        }

        private string TranslateOneToMany(string functionName, MemberExpression memberExpression, LambdaExpression fieldExpression, LambdaExpression filterExpression)
        {
            if (!string.IsNullOrEmpty(TranslateMember(memberExpression))) // this shouldn't return any SQL
                throw new SqlExpressionTranslatorException(memberExpression.ToString());

            var relation = GetRelation(memberExpression.Expression, memberExpression.Member.Name);

            if (relation == null)
                throw new SqlExpressionTranslatorException(memberExpression.ToString());

            if (fieldExpression != null && filterExpression != null && fieldExpression.Parameters[0] != filterExpression.Parameters[0])
                throw new SqlExpressionTranslatorException(null);

            var iterator = fieldExpression != null ? fieldExpression.Parameters[0] : filterExpression != null ? filterExpression.Parameters[0] : Expression.Parameter(relation.ElementType);

            LambdaExpression relationExpression = CreateToManyFilterExpression(relation, memberExpression.Expression, filterExpression, iterator);

            var template = _toManyTemplates[functionName];

            if (template == null)
                throw new NotSupportedException(functionName);

            var alias = SqlNameGenerator.NextTableAlias();

            _metaData[iterator] = new ExpressionMetaData { Iterator = iterator, Relation = relation, Schema = relation.ForeignSchema };

            _relationAliases[iterator] = new Dictionary<object, string> { { relation, alias } };

            _subQueries.Push(new SubQuery());

            string sqlFields = Translate(fieldExpression);
            string sqlWhere = Translate(relationExpression);
            string sqlJoins = (Joins.Count > 0) ? string.Join(" ", Joins.Select(join => join.ToSql(_sqlDialect))) : null;

            _subQueries.Pop();

            return string.Format(template,
                                    sqlFields,
                                    _sqlDialect.QuoteTable(relation.ForeignSchema.MappedName) + " " + alias, sqlJoins ?? "",
                                    sqlWhere
                                );
        }

        private static LambdaExpression CreateToManyFilterExpression(TableSchema.Relation relation, Expression localExpression, LambdaExpression filterLambda, ParameterExpression lambdaParameter)
        {
            Expression exp1 = Expression.MakeMemberAccess(localExpression, relation.LocalField.FieldInfo);
            Expression exp2 = Expression.MakeMemberAccess(lambdaParameter, relation.ForeignField.FieldInfo);

            if (exp2.Type != exp1.Type)
                exp2 = Expression.Convert(exp2, exp1.Type);

            var expression = Expression.Equal(exp1,exp2);

            if (filterLambda != null)
                expression = Expression.AndAlso(filterLambda.Body, expression);

            return Expression.Lambda(expression, lambdaParameter);
        }
        
        private string CreateParameter(object value, Type type = null)
        {
            var parameterName = SqlNameGenerator.NextParameterName();

            SqlParameters[parameterName] = new QueryParameter(parameterName, value, type);

            return _sqlDialect.CreateParameterExpression(parameterName);
        }

        private static readonly SafeDictionary<string, string> _toManyTemplates = new SafeDictionary<string, string>()
        {
            {"Any", "exists (select * from {1} {2} where {3})"},
            {"Count", "(select count(*) from {1} {2} where {3})"},
            {"Sum","(select sum({0}) from {1} {2} where {3})"},
            {"Average","(select avg({0}) from {1} {2} where {3})"}
        };

    }
}