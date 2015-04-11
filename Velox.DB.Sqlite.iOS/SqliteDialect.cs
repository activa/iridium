using System;

namespace Velox.DB.Sql.Sqlite
{
    public class SqliteDialect : SqlDialect
    {
        public override string QuoteField(string fieldName)
        {
            return "\"" + fieldName.Replace(".", "\".\"") + "\"";
        }

        public override string QuoteTable(string tableName)
        {
            return "\"" + tableName.Replace("\"", "\".\"") + "\"";
        }

        public override string CreateParameterExpression(string parameterName)
        {
            return "@" + parameterName;
        }

        public override string TruncateTableSql(string tableName)
        {
            return "DELETE FROM " + QuoteTable(tableName) + ";delete from sqlite_sequence where name='" + tableName + "'";
        }

        public override string GetLastAutoincrementIdSql(string columnName, string alias, string tableName)
        {
            return "select last_insert_rowid() as " + alias;
        }

        public override string DeleteSql(SqlTableNameWithAlias tableName, string sqlWhere)
        {
            if (tableName.Alias != null)
                sqlWhere = sqlWhere.Replace(QuoteTable(tableName.Alias) + ".", "");
            
            return "delete from " + QuoteTable(tableName.TableName) + " where " + sqlWhere;
        }
    }
}
