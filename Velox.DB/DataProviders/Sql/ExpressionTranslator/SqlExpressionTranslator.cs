using System;
using System.Collections.Generic;
using System.Linq;

namespace Velox.DB.Sql
{
    internal interface ISqlExpressionTranslator
    {
        string Translate(QueryExpression expression);
        void Init(SqlExpressionBuilder sqlBuilder, OrmSchema schema, string tableAlias);
    }

    internal sealed class SqlExpressionTranslator
    {
        private readonly OrmSchema _schema;
        private readonly string _tableAlias;
        private readonly SqlExpressionBuilder _sqlBuilder;
        private readonly Dictionary<Type, ISqlExpressionTranslator> _translators = new Dictionary<Type, ISqlExpressionTranslator>();

        private static readonly Dictionary<Type,Func<ISqlExpressionTranslator>> _translatorFactories = new Dictionary<Type, Func<ISqlExpressionTranslator>>()
        {
            { typeof(LambdaQueryExpression), () => new SqlLambdaExpressionTranslator() }
        };

        public static void RegisterSqlTranslator<TExpression>(Func<ISqlExpressionTranslator> factory) 
        {
            _translatorFactories[typeof(TExpression)] = factory;
        }

        public SqlExpressionTranslator(SqlDialect sqlDialect, OrmSchema schema, string tableAlias)
        {
            _schema = schema;
            _tableAlias = tableAlias;
            _sqlBuilder = new SqlExpressionBuilder(sqlDialect);
        }

        public string Translate(QueryExpression expression)
        {
            ISqlExpressionTranslator translator;

            if (!_translators.TryGetValue(expression.GetType(), out translator))
            {
                translator = _translatorFactories[expression.GetType()]();

                translator.Init(_sqlBuilder, _schema, _tableAlias);

                _translators[expression.GetType()] = translator;
            }

            try
            {
                return translator.Translate(expression);
            }
            catch (SqlExpressionTranslatorException)
            {
                return null; // we couldn't translate the given expression
            }
        }

        public HashSet<SqlJoinDefinition> Joins
        {
            get { return _sqlBuilder.Joins; }
        }

        public QueryParameterCollection SqlParameters
        {
            get { return _sqlBuilder.SqlParameters; }
        }
    }
}