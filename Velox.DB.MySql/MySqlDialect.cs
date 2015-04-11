namespace Velox.DB.Sql
{
    public class MySqlDialect : SqlDialect
    {
        public override string QuoteField(string fieldName)
        {
            return "`" + fieldName.Replace(".", "`.`") + "`";
        }

        public override string QuoteTable(string tableName)
        {
            return "`" + tableName.Replace(".", "`.`") + "`";
        }

        public override string CreateParameterExpression(string parameterName)
        {
            return "?" + parameterName;
        }

        public override string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere)
        {
            if (tableName.Alias != null)
                return "delete " + tableName.Alias + " from " + QuoteTable(tableName.TableName) + (tableName.Alias != null ? (" " + tableName.Alias + " ") : "") + " where " + sqlWhere;
            else
                return "delete from " + QuoteTable(tableName.TableName) + " where " + sqlWhere;
        }

        public override string GetLastAutoincrementIdSql(string columnName, string alias, string tableName)
        {
            return "select last_insert_id() as " + alias;
        }

    }
}