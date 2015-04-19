using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Velox.DB.Core;

namespace Velox.DB.Sql
{
    internal class SqlTranslatorState
    {
        public class ExpressionMetaData
        {
            public object Iterator;
            public OrmSchema.Relation Relation;
            public OrmSchema Schema;

            public object Key { get { return (object)Relation ?? Schema; } }
        }

        public class SubQuery
        {
            public readonly HashSet<SqlJoinDefinition> Joins = new HashSet<SqlJoinDefinition>();
        }

        private readonly SafeDictionary<object, ExpressionMetaData> _metaData = new SafeDictionary<object, ExpressionMetaData>();
        private readonly Dictionary<object, Dictionary<object, string>> _relationAliases = new Dictionary<object, Dictionary<object, string>>();
        private readonly QueryParameterCollection _sqlParameters = new QueryParameterCollection();
        private readonly Stack<SubQuery> _subQueries = new Stack<SubQuery>();
        private object _rootIterator;
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

        public HashSet<SqlJoinDefinition> Joins
        {
            get { return CurrentQuery.Joins; }
        }

        public SqlTranslatorState()
        {
            _subQueries.Push(new SubQuery());
        }

        public void PrepareForNewExpression(object rootIterator, string tableAlias, OrmSchema schema)
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

        
    }

    internal class _SqlExpressionBuilder
    {
        private SqlDialect _sqlDialect;
        private SqlTranslatorState _state;

        private static readonly HashSet<Type> _nativeTypes = new HashSet<Type>(new[]
        {
            typeof(Boolean), typeof(Byte), typeof(SByte), typeof(UInt16), typeof(UInt32), typeof(UInt64), typeof(Int16), typeof(Int32), typeof(Int64), typeof(Single), typeof(Double), typeof(Decimal), typeof(string), typeof(DateTime)
        });


        public _SqlExpressionBuilder(SqlDialect sqlDialect)
        {
            _sqlDialect = sqlDialect;

            _state = new SqlTranslatorState(); //TODO: should be created outside class
        }

        public string ProcessMember(object fullExpression, object leftExpression, string memberName)
        {
            var parentMetaData = _state._metaData[leftExpression];

            if (parentMetaData != null)
            {
                if (_state._metaData.ContainsKey(fullExpression))
                    return true; // relation already visited

                var iterator = _state.GetRootIterator(parentMetaData.Iterator);

                var relation = parentMetaData.Schema.Relations[memberName];
                var leftAlias = _state._relationAliases[iterator][parentMetaData.Key];

                if (relation != null && relation.RelationType == OrmSchema.RelationType.ManyToOne)
                {
                    if (!_state._relationAliases[iterator].ContainsKey(relation))
                    {
                        var sqlJoin = new SqlJoinDefinition
                        {
                            Left = new SqlJoinPart(parentMetaData.Schema, relation.LocalField, leftAlias),
                            Right = new SqlJoinPart(relation.ForeignSchema, relation.ForeignField, SqlNameGenerator.NextTableAlias()),
                            Type = SqlJoinType.Inner
                        };

                        _state.CurrentQuery.Joins.Add(sqlJoin);

                        _state._relationAliases[iterator][relation] = sqlJoin.Right.Alias;
                    }

                    _state._metaData[fullExpression] = new  ExpressionMetaData { Iterator = iterator, Relation = relation, Schema = relation.ForeignSchema };
                }
                else if (relation == null)
                {
                    return _sqlDialect.QuoteField(leftAlias + "." + memberName);
                }
            }

            return null;
        }

        public string ProcessConstant(Type type, object value, bool createParameter)
        {
            if (_nativeTypes.Contains(type.Inspector().RealType))
            {
                if (createParameter)
                {
                    var parameterName = SqlNameGenerator.NextParameterName();

                    _state.SqlParameters[parameterName] = value;

                    return _sqlDialect.CreateParameterExpression(parameterName);
                }
                else
                {
                    if (value is string)
                        return "'" + value + "'";
                    else
                        return value.ToString();
                }
            }

            return null;
        }

        public OrmSchema.Relation GetRelation(object expression, string memberName)
        {
            return _state._metaData[expression].Schema.Relations[memberName];
        }

        private static readonly SafeDictionary<string, string> _toManyTemplates = new SafeDictionary<string, string>()
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

            _relationAliases[iterator] = new Dictionary<object, string> { { relation, alias } };

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
    }

    internal sealed class _SqlLambdaTranslator : _SqlExpressionBuilder
    {
        private readonly string _tableAlias;
        private readonly OrmSchema _schema;
        private readonly SqlDialect _sqlDialect;

        public _SqlLambdaTranslator(SqlDialect dialect, string tableAlias, OrmSchema schema) : base(dialect)
        {
            _tableAlias = tableAlias;
            _schema = schema;
            _sqlDialect = dialect;
        }

        public string Translate(QueryExpression queryExpression)
        {
            var lambda = ((LambdaQueryExpression) queryExpression).Expression;

            var rootIterator = lambda.Parameters[0];

            PrepareForNewExpression(rootIterator, _tableAlias, _schema);

            return Translate(lambda);
        }

        public string Translate(Expression expression)
        {
            if (expression is BinaryExpression)
                return TranslateBinary((BinaryExpression) expression);

            if (expression is MemberExpression)
                return TranslateMember((MemberExpression) expression);
        }

        private string TranslateMember(MemberExpression node)
        {
            if (node.Expression.Type == typeof(string) && node.Member.Name == "Length")
            {
                string fnName = _sqlDialect.SqlFunctionName(SqlDialect.Function.StringLength, Translate(node.Expression));

                if (fnName != null)
                    return fnName;
            }

            throw new SqlExpressionTranslatorException(node.ToString());

        }

        public string TranslateBinary(BinaryExpression expression)
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

            string op = null;

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    op = "+";
                    break;
                case ExpressionType.AndAlso:
                    op = "AND";
                    break;
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
                    throw new SqlExpressionTranslatorException(expression.ToString());
            }

            return string.Format("({0} {1} {2})", Translate(expression.Left), op, Translate(expression.Right));
        }
        
    }
}