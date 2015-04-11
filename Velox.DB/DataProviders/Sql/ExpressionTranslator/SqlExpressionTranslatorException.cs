using System;

namespace Velox.DB.Sql
{
    public class SqlExpressionTranslatorException : Exception
    {
        public SqlExpressionTranslatorException(string expression)
            : base("Expression not supported:" + expression)
        {
        }
    }
}