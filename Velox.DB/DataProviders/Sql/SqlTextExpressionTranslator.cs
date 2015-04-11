using System;
using System.Collections.Generic;
using Velox.Linq;
using Velox.Core.Parser;

namespace Velox.DB.Sql
{
    internal class SqlTextExpressionTranslator : ExpressionVisitor
    {
        private readonly Stack<Expression> _iteratorStack = new Stack<Expression>();

        private Expression CurrentIterator
        {
            get { return _iteratorStack.Peek(); }
        }

        private readonly SqlExpressionBuilder _builder;
        private readonly OrmSchema _schema;
        private readonly string _tableAlias;

        public SqlTextExpressionTranslator(SqlExpressionBuilder sqlBuilder, OrmSchema schema, string tableAlias)
        {
            _builder = sqlBuilder;
            _schema = schema;
            _tableAlias = tableAlias;
        }

        public string Translate(Expression expression)
        {
            if (expression == null)
                return null;

            var rootIterator = new VariableExpression("~");

            _iteratorStack.Push(rootIterator);

            _builder.PrepareForNewExpression(rootIterator, _tableAlias, _schema);

            Visit((Expression)expression);

            _iteratorStack.Pop();

            return _builder.SqlExpression;
        }


        protected override Expression VisitField(FieldExpression node)
        {
            Visit(node.Target);

            if (_builder.ProcessMember(node, node.Target, node.Member))
                return node;
            
            throw new SqlExpressionTranslatorException(node.ToString());
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            _builder.AppendSql("(");

            Visit(node.Left);

            if (node.Right is VariableExpression && (((VariableExpression)node.Right).VarName == "null"))
            {
                if (node is BinaryArithmicExpression && ((BinaryArithmicExpression)node).Operator == "==")
                    _builder.AppendSql(" IS NULL)");
                else if (node is BinaryArithmicExpression && ((BinaryArithmicExpression)node).Operator == "!=")
                    _builder.AppendSql(" IS NOT NULL)");
                else
                    throw new NotSupportedException(node + " not supported with null values");

                return node;
            }


            if (node is BinaryArithmicExpression)
            {
                var binaryArithmicExpression = (BinaryArithmicExpression)node;

                switch (binaryArithmicExpression.Operator)
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                    case "<=":
                    case ">=":
                    case ">":
                    case "<":
                        _builder.AppendSql(binaryArithmicExpression.Operator);
                        break;
                    case "==":
                        _builder.AppendSql("=");
                        break;
                    case "!=":
                        _builder.AppendSql("<>");
                        break;

                    default: 
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (node is AndAlsoExpression)
                _builder.AppendSql(" AND ");

            if (node is OrElseExpression)
                _builder.AppendSql(" OR ");

            Visit(node.Right);

            _builder.AppendSql(")");

            return node;
        }


        protected override Expression VisitCall(CallExpression node)
        {
            var fieldExpression = node.MethodExpression as FieldExpression;

            if (fieldExpression == null) 
                return node;

            switch (fieldExpression.Member)
            {
                case "Any":
                case "Count":
                case "All":
                    VisitOneToManyFunction(
                        fieldExpression.Member,
                        fieldExpression.Target,
                        null,
                        node.Parameters.Length > 0 ? node.Parameters[0] : null
                        );
                    break;
                case "Sum":
                case "Avg":
                    VisitOneToManyFunction(
                        fieldExpression.Member,
                        fieldExpression.Target,
                        node.Parameters[0],
                        node.Parameters.Length > 1 ? node.Parameters[1] : null
                        );
                    break;
            }

            return node;
        }

        private static Expression CreateToManyFilterExpression(OrmSchema.Relation relation, Expression localExpression, Expression filterExpression, Expression iterator)
        {
            Expression expression = Exp.Equal(
                Exp.Field(localExpression, relation.LocalField.FieldName),
                Exp.Field(iterator, relation.ForeignField.FieldName)
                );

            if (filterExpression != null)
                expression = Exp.AndAlso(filterExpression, expression);

            return expression;
        }

        private void VisitOneToManyFunction(string functionName, Expression collectionExpression, Expression fieldExpression, Expression filterExpression)
        {
            Visit(collectionExpression);

            OrmSchema.Relation relation = null;

            if (collectionExpression is VariableExpression)
                relation = _builder.GetRelation(CurrentIterator, ((VariableExpression) collectionExpression).VarName);
            else if (collectionExpression is FieldExpression)
                relation = _builder.GetRelation(((FieldExpression) collectionExpression).Target, ((FieldExpression) collectionExpression).Member);

            if (relation == null)
                return;

            var iterator = new VariableExpression("~" + relation.ForeignSchema.ObjectType.Name);

            Expression relationExpression = CreateToManyFilterExpression(relation, CurrentIterator, filterExpression, iterator);

            _builder.OneToMany(
                relation,
                iterator,
                functionName,
                delegate { _iteratorStack.Push(iterator); Visit(fieldExpression); _iteratorStack.Pop(); },
                delegate { _iteratorStack.Push(iterator); Visit(relationExpression); _iteratorStack.Pop(); }
                );
        }

        protected override Expression VisitVariable(VariableExpression expression)
        {
            if (expression.VarName.StartsWith("@")) // named parameter
            {
                _builder.AppendVariableName(expression.VarName.Substring(1));
            }
            else if (!expression.VarName.StartsWith("~")) // not an iterator
            {
                _builder.ProcessMember(expression, CurrentIterator, expression.VarName);
            }

            return expression;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node is NegationExpression)
                _builder.AppendSql(" NOT ");

            return base.VisitUnary(node);
        }

        protected override Expression VisitValue(ValueExpression node)
        {
            if (_builder.ProcessConstant(node.Type, node.Value, createParameter:false))
                return node;

            throw new SqlExpressionTranslatorException(node.ToString());
        }
    }
}