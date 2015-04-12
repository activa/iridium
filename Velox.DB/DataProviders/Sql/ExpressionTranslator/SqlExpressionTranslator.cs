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