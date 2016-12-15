using System;
using System.Data.Common;

namespace Cav
{
    /// <summary>
    /// "Груповая" транзакция. Обертка для вызовов в БД. Только для одного DbConnection
    /// </summary>
    public class DbTransactionScope : IDisposable
    {
        /// <summary>
        /// Создание нового экземпляра обертки транзации
        /// </summary>
        public DbTransactionScope()
        {
            if (!DbTransactionScope.rootTran.HasValue)
                rootTran = currentTran;

            if (rootTran != currentTran)
                return;

            if (Transaction == null)
                Transaction = DbTransactionScope.Connection.BeginTransaction();
        }

        private Boolean complete = false;

        internal static Guid? rootTran = null;
        private Guid currentTran = Guid.NewGuid();

        internal static DbConnection Connection
        {
            get
            {
                if (Transaction != null)
                    return Transaction.Connection;
                return DomainContext.Connection();
            }
        }

        [ThreadStatic]
        internal static DbTransaction Transaction = null;

        #region Члены IDisposable

        /// <summary>
        /// Пометить, что транзакцию можно закомитеть
        /// </summary>
        public void Complete()
        {
            complete = true;
        }

        void IDisposable.Dispose()
        {

            if (Transaction != null && !complete)
            {
                var conn = Transaction.Connection;
                Transaction.Rollback();
                Transaction.Dispose();
                Transaction = null;
                rootTran = null;
                conn.Close();
                conn.Dispose();
            }

            if (rootTran != currentTran)
                return;

            if (Transaction != null)
            {
                var conn = Transaction.Connection;
                Transaction.Commit();
                Transaction.Dispose();
                Transaction = null;
                rootTran = null;
                conn.Close();
                conn.Dispose();
            }

        }

        #endregion
    }
}
