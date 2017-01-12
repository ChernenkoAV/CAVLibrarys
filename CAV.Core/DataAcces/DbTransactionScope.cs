using System;
using System.Collections.Generic;
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
        /// <param name="connectionName">Имя соедиенения, для которого назначается транзакция</param>
        public DbTransactionScope(String connectionName = null)
        {
            if (!DbTransactionScope.rootTran.HasValue)
                rootTran = currentTran;

            if (rootTran != currentTran)
                return;

            if (TransactionGet(connectionName) == null)
            {
                transactions.Add(connectionName, DbTransactionScope.Connection(connectionName).BeginTransaction());
                this.connectionName = connectionName;
            }

        }

        private Boolean complete = false;
        private String connectionName = null;

        internal static Guid? rootTran = null;
        private Guid currentTran = Guid.NewGuid();

        internal static DbConnection Connection(String connectionName = null)
        {
            var tran = TransactionGet(connectionName);
            if (tran != null)
                return tran.Connection;
            return DomainContext.Connection(connectionName);
        }

        [ThreadStatic]
        private static Dictionary<String, DbTransaction> transactions = new Dictionary<string, DbTransaction>();

        internal static DbTransaction TransactionGet(String connectionName = null)
        {
            DbTransaction tran = null;
            transactions.TryGetValue(connectionName, out tran);
            return tran;
        }

        #region Члены IDisposable

        /// <summary>
        /// Пометить, что транзакцию можно закомитеть
        /// </summary>
        public void Complete()
        {
            complete = true;
        }

        /// <summary>
        /// Реализация IDisposable
        /// </summary>
        public void Dispose()
        {
            var tran = TransactionGet(connectionName);

            if (tran != null && !complete)
            {
                var conn = tran.Connection;
                tran.Rollback();
                tran.Dispose();
                transactions.Remove(connectionName);
                rootTran = null;
                conn.Close();
                conn.Dispose();
            }

            if (rootTran != currentTran)
                return;

            if (tran != null)
            {
                var conn = tran.Connection;
                tran.Commit();
                tran.Dispose();
                transactions.Remove(connectionName);
                rootTran = null;
                conn.Close();
                conn.Dispose();
            }

        }

        #endregion
    }
}
