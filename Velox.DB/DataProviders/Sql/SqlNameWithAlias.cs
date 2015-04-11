namespace Velox.DB.Sql
{
    public class SqlExpressionWithAlias
    {
        public SqlExpressionWithAlias(string expression, string alias = null)
        {
            Expression = expression;
            Alias = alias;
        }

        public string Expression;
        public string Alias;

        public virtual bool ShouldQuote { get { return false; } }
    }

    public class SqlColumnNameWithAlias : SqlExpressionWithAlias
    {
        public SqlColumnNameWithAlias(string name, string alias = null) : base(name,alias)
        {
        }

        //public string ColumnName { get {  return Expression; } }

        public override bool ShouldQuote { get { return true; } }
    }

    public class SqlTableNameWithAlias : SqlExpressionWithAlias
    {
        public SqlTableNameWithAlias(string name, string alias = null) : base(name, alias)
        {
        }

        public string TableName { get { return Expression; } }

        public override bool ShouldQuote { get { return true; } }
    }

}