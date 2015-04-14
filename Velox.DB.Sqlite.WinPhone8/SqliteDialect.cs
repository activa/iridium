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
