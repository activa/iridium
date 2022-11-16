#region License
//=============================================================================
// Iridium - Porable .NET ORM 
//
// Copyright (c) 2015-2017 Philippe Leybaert
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
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Iridium.DB.SqlServer
{
    public class SqlServerDataProvider : SqlAdoDataProvider<SqlConnection, SqlServerDialect>
    {
        public SqlServerDataProvider()
        {}

        public SqlServerDataProvider(string connectionString) : base(connectionString)
        {}

        public override void ClearConnectionPool() => SqlConnection.ClearAllPools();

        private readonly ThreadLocal<Stack<string>> _transactionStack = new ThreadLocal<Stack<string>>(() => new Stack<string>());
        private readonly ThreadLocal<SqlTransaction> _transaction = new ThreadLocal<SqlTransaction>(true);

        public override void BeginTransaction(IsolationLevel isolationLevel)
        {
            if (isolationLevel == IsolationLevel.None)
                _transactionStack.Value.Push(null);
            else
            {
                if (_transaction.Value == null)
                {
                    _transaction.Value = Connection.BeginTransaction(AdoIsolationLevel(isolationLevel));

                    _transactionStack.Value.Push("");
                }
                else
                {
                    string savePoint = "SP" + _transactionStack.Value.Count;

                    _transaction.Value.Save(savePoint);
                    _transactionStack.Value.Push(savePoint);
                }
            }

        }
    

        public override void CommitTransaction()
        {
            try
            {
                string name = _transactionStack.Value.Pop();

                if (name != null)
                {
                    if (name == "")
                    {
                        _transaction.Value.Commit();
                        _transaction.Value = null;
                    }
                    else
                    {
                        // no need to commit named savepoint
                    }
                }
            }
            finally
            {
                if (_transactionStack.Value.Count == 0)
                    CloseConnection();
            }
        }

        public override void RollbackTransaction()
        {
            try
            {
                string name = _transactionStack.Value.Pop();

                if (name != null)
                {
                    if (name == "")
                    {
                        _transaction.Value.Rollback();
                        _transaction.Value = null;
                    }
                    else
                    {
                        _transaction.Value.Rollback(name);
                    }
                }
            }
            finally
            {
                if (_transactionStack.Value.Count == 0)
                    CloseConnection();
            }
        }

        protected override DbTransaction CurrentTransaction => _transaction.Value;
    }
}
