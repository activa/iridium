using System;

namespace Iridium.DB
{
    public class Transaction : IDisposable
    {
        private StorageContext _context;

        public Transaction(StorageContext context, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            _context = context;

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
            _context?.DataProvider.RollbackTransaction();
        }
    }
}