using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Velox.Linq;
using Vici.Core;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace Velox.DB
{
    public class SqlExpressionTranslator : ExpressionVisitor
    {
        private string _sql = "";
        private readonly Dictionary<string,object> _parameters = new Dictionary<string, object>(); 

        public static Tuple<string,Dictionary<string,object>> Translate(Expression exp)
        {
            SqlExpressionTranslator visitor = new SqlExpressionTranslator();

            visitor.Visit(exp);

            return new Tuple<string, Dictionary<string, object>>(visitor._sql,visitor._parameters);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression.NodeType == ExpressionType.Constant)
            {
                ConstantExpression constantExpression = (ConstantExpression) node.Expression;

                _parameters[node.Member.Name] = node.Member.Inspector().GetValue(constantExpression.Value);


                _sql += "@" + node.Member.Name;

                return node;
            }

            _sql += node.Member.Name;

            return base.VisitMember(node);
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(PartialEvaluator.Eval(node));
            //return base.Visit(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            _sql += "(";

            Visit(node.Left);

            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    _sql += "+";
                    break;
                case ExpressionType.AddChecked:
                    break;
                case ExpressionType.And:
                    break;
                case ExpressionType.AndAlso:
                    _sql += " AND ";
                    break;
                case ExpressionType.Divide:
                    _sql += "/";
                    break;
                case ExpressionType.Equal:
                    _sql += "=";
                    break;
                case ExpressionType.ExclusiveOr:
                    break;
                case ExpressionType.GreaterThan:
                    _sql += ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _sql += ">=";
                    break;
                case ExpressionType.LessThan:
                    _sql += "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    break;
                case ExpressionType.Modulo:
                    break;
                case ExpressionType.Multiply:
                    break;
                case ExpressionType.NotEqual:
                    _sql += "<>";
                    break;
                case ExpressionType.Or:
                    break;
                case ExpressionType.OrElse:
                    _sql += " OR ";
                    break;
                case ExpressionType.Power:
                    break;
                case ExpressionType.Subtract:
                    _sql += "-";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Visit(node.Right);

            _sql += ")";

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof (System.Linq.Enumerable))
            {
                if (node.Method.Name == "Any")
                {
                    _sql += "(exists (select * from ...";

                    if (node.Arguments.Count > 1)
                    {
                        _sql += " where ";
                        base.Visit(node.Arguments[1]);
                    }

                    _sql += "))";
                }

                if (node.Method.Name == "Count")
                {
                    _sql += "(select count(*) from ...";

                    if (node.Arguments.Count > 1)
                    {
                        _sql += " where ";
                        base.Visit(node.Arguments[1]);
                    }

                    _sql += ")";
                }

                if (node.Method.Name == "Sum")
                {
                    _sql += "(select sum(";

                    base.Visit(node.Arguments[1]);

                    _sql += ") from ...)";

                    return node;
                }

            }


            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
                _sql += " NOT ";

            return base.VisitUnary(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            //object value = ExpressionEvaluator.Eval(node);

            if (node.Type.Inspector().IsValueType)
                _sql += node.Value;

            return node;
        }


    }
}