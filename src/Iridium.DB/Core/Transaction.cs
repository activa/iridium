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

namespace Iridium.DB
{
    public class Transaction : IDisposable
    {
        private StorageContext _context;
        private readonly bool _commitOnDispose;

        public Transaction(StorageContext context, IsolationLevel isolationLevel = IsolationLevel.Serializable, bool commitOnDispose = false)
        {
            _context = context;
            _commitOnDispose = commitOnDispose;

            _context.DataProvider.BeginTransaction(isolationLevel);
        }

        public void Commit()
        {
            _context.DataProvider.CommitTransaction();

            _context = null;
        }

        public void Rollback()
        {
            _context.DataProvider.RollbackTransaction();

            _context = null;
        }

        public void Dispose()
        {
            if (_commitOnDispose)
                _context?.DataProvider.CommitTransaction();
            else
                _context?.DataProvider.RollbackTransaction();
        }
    }
}