using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Velox.DB.Sql
{
    internal sealed class SqlLambdaExpressionTranslator : ExpressionVisitor, ISqlExpressionTranslator
    {
        private SqlExpressionBuilder _builder;
        private OrmSchema _schema;
        private string _tableAlias;

        public SqlLambdaExpressionTranslator()
        {
        }

        public void Init(SqlExpressionBuilder sqlBuilder, OrmSchema schema, string tableAlias)
        {
            _builder = sqlBuilder;
            _schema = schema;
            _tableAlias = tableAlias;
        }

        public string Translate(QueryExpression expression)
        {
            if (expression == null)
                return null;

            LambdaExpression lambda = ((LambdaQueryExpression) expression).Expression;

            var rootIterator = lambda.Parameters[0];

            _builder.PrepareForNewExpression(rootIterator, _tableAlias, _schema);

            Visit(lambda);

            return _builder.SqlExpression;
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(PartialEvaluator.Eval(node));
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Visit(node.Expression);

            if (_builder.ProcessMember(node, node.Expression, node.Member.Name))
                return node;
            
            throw new SqlExpressionTranslatorException(node.ToString());
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            _builder.AppendSql("(");

            Visit(node.Left);

            if (node.Right.NodeType == ExpressionType.Constant && (((ConstantExpression)node.Right).Value == null))
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        _builder.AppendSql(" IS NULL)");
                        break;
                    case ExpressionType.NotEqual:
                        _builder.AppendSql(" IS NOT NULL)");
                        break;
                    default:
                        throw new SqlExpressionTranslatorException(node.ToString());
                }

                return node;
            }


            switch (node.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    _builder.AppendSql("+");
                    break;
                case ExpressionType.AndAlso:
                    _builder.AppendSql(" AND ");
                    break;
                case ExpressionType.Divide:
                    _builder.AppendSql("/");
                    break;
                case ExpressionType.Equal:
                    _builder.AppendSql("=");
                    break;
                case ExpressionType.GreaterThan:
                    _builder.AppendSql(">");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _builder.AppendSql(">=");
                    break;
                case ExpressionType.LessThan:
                    _builder.AppendSql("<");
                    break;
                case ExpressionType.LessThanOrEqual:
                    _builder.AppendSql("<=");
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    _builder.AppendSql("*");
                    break;
                case ExpressionType.NotEqual:
                    _builder.AppendSql("<>");
                    break;
                case ExpressionType.OrElse:
                    _builder.AppendSql(" OR ");
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    _builder.AppendSql("-");
                    break;
                default:
                    throw new SqlExpressionTranslatorException(node.ToString());
            }

            Visit(node.Right);

            _builder.AppendSql(")");

            return node;
        }

        private static Expression UnQuote(Expression expression)
        {
            return expression.NodeType == ExpressionType.Quote ? ((UnaryExpression) expression).Operand : expression;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var methodName = node.Method.Name;
            MemberExpression leftExpression = null;
            var arguments = new List<LambdaExpression>();

            if (node.Method.DeclaringType == typeof (string))
            {
                Visit(node.Object);

                object[] stringArguments = node.Arguments.Select(UnQuote).OfType<ConstantExpression>().Select(exp => exp.Value).ToArray();

                switch (methodName)
                {
                    case "StartsWith":
                        {
                            _builder.AppendSql(" like ");

                            _builder.ProcessConstant(typeof (string), stringArguments[0] + "%", true);

                            return node;
                        }
                    case "EndsWith":
                        {
                            _builder.AppendSql(" like ");

                            _builder.ProcessConstant(typeof(string), "%" + stringArguments[0], true);

                            return node;
                        }
                }

                throw new SqlExpressionTranslatorException(node.ToString());
            }

            if (node.Method.DeclaringType.IsConstructedGenericType && node.Method.DeclaringType.GetGenericTypeDefinition() == typeof (IDataSet<>))
            {
                leftExpression = node.Object as MemberExpression;
                arguments.AddRange(node.Arguments.Select(UnQuote).OfType<LambdaExpression>());
            }

            if (node.Method.DeclaringType == typeof(Enumerable) && node.Arguments.Count > 0)
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
                        VisitOneToManyFunction(
                            methodName,
                            leftExpression,
                            null,
                            arguments.Count > 0 ? arguments[0] : null
                            );
                        return node;
                        
                    case "Sum":
                    case "Avg":
                        VisitOneToManyFunction(
                            methodName,
                            leftExpression,
                            arguments[0],
                            null
                            );

                        return node;
                }
            }

            throw new SqlExpressionTranslatorException(node.ToString());
        }

        private static LambdaExpression CreateToManyFilterExpression(OrmSchema.Relation relation, Expression localExpression, LambdaExpression filterLambda, ParameterExpression lambdaParameter)
        {
            var expression = Expression.Equal(
                Expression.MakeMemberAccess(localExpression, relation.LocalField.Accessor.AsMember),
                Expression.MakeMemberAccess(lambdaParameter, relation.ForeignField.Accessor.AsMember)
                );

            if (filterLambda != null)
                expression = Expression.AndAlso(filterLambda.Body, expression);
                
            return Expression.Lambda(expression,lambdaParameter);
        }



        private void VisitOneToManyFunction(string functionName, MemberExpression memberExpression, LambdaExpression fieldExpression, LambdaExpression filterExpression)
        {
            Visit(memberExpression);

            var relation = _builder.GetRelation(memberExpression.Expression, memberExpression.Member.Name);

            if (relation == null)
                return;

            if (fieldExpression != null && filterExpression != null && fieldExpression.Parameters[0] != filterExpression.Parameters[0])
                throw new SqlExpressionTranslatorException(null);

            var parameter = fieldExpression != null ? fieldExpression.Parameters[0] : filterExpression != null ? filterExpression.Parameters[0] : Expression.Parameter(relation.ElementType);

            LambdaExpression relationExpression = CreateToManyFilterExpression(relation, memberExpression.Expression, filterExpression, parameter);

            _builder.OneToMany(
                relation,
                parameter,
                functionName,
                () => Visit(fieldExpression),
                () => Visit(relationExpression)
                );
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
                _builder.AppendSql(" NOT ");

            return base.VisitUnary(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (_builder.ProcessConstant(node.Type, node.Value, createParameter: true))
                return node;

            throw new SqlExpressionTranslatorException(node.ToString());
        }
    }
}