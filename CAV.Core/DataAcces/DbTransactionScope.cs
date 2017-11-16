﻿using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Cav
{
    /// <summary>
    /// Делегат окончания работы транзакции
    /// </summary>
    /// <param name="connName">Имя соединения с БД</param>
    public delegate void DbTransactionScopeEnd(String connName);

    /// <summary>
    /// "Груповая" транзакция. Обертка для вызовов в БД. Только для одного DbConnection
    /// </summary>
    public sealed class DbTransactionScope : IDisposable
    {
        /// <summary>
        /// Создание нового экземпляра обертки транзации
        /// </summary>
        /// <param name="connectionName">Имя соедиенения, для которого назначается транзакция</param>
        public DbTransactionScope(String connectionName = null)
        {
            currentTran = Guid.NewGuid();
            if (connectionName.IsNullOrWhiteSpace())
                connectionName = DomainContext.defaultNameConnection;

            if (!rootTran.HasValue)
                rootTran = currentTran;

            if (rootTran != currentTran)
                return;

            if (TransactionGet(connectionName) == null)
            {
                if (transactions == null)
                    transactions = new Dictionary<string, DbTransaction>();
                transactions.Add(connectionName, DbTransactionScope.Connection(connectionName).BeginTransaction());
                this.connName = connectionName;
            }

        }

        private Boolean complete = false;
        private String connName = null;

        [ThreadStatic]
        private static Guid? rootTran = null;

        [ThreadStatic]
        private static Dictionary<String, DbTransaction> transactions;

        private readonly Guid currentTran;

        internal static DbConnection Connection(String connectionName = null)
        {
            if (connectionName.IsNullOrWhiteSpace())
                connectionName = DomainContext.defaultNameConnection;

            var tran = TransactionGet(connectionName);
            if (tran != null)
                return tran.Connection;
            return DomainContext.Connection(connectionName);
        }

        internal static DbTransaction TransactionGet(String connectionName = null)
        {
            if (connectionName.IsNullOrWhiteSpace())
                connectionName = DomainContext.defaultNameConnection;

            DbTransaction tran = null;
            if (transactions == null)
                transactions = new Dictionary<string, DbTransaction>();

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
            String connNameEv = connName;

            if (connName.IsNullOrWhiteSpace())
                connName = DomainContext.defaultNameConnection;

            var tran = TransactionGet(connName);

            if (tran != null && !complete)
            {
                transactions.Remove(connName);
                rootTran = null;

                var conn = tran.Connection;
                if (conn != null)
                {
                    tran.Rollback();
                    tran.Dispose();

                    conn.Close();
                    conn.Dispose();

                    if (TransactionRollback != null)
                        TransactionRollback(connNameEv);
                }
            }

            if (rootTran != currentTran)
                return;

            tran = TransactionGet(connName);
            if (tran != null)
            {
                transactions.Remove(connName);
                rootTran = null;

                var conn = tran.Connection;
                if (conn != null)
                {
                    tran.Commit();
                    tran.Dispose();

                    conn.Close();
                    conn.Dispose();

                    if (TransactionCommit != null)
                        TransactionCommit(connNameEv);
                }
            }
        }

        #endregion

        /// <summary>
        /// Событие окончания транзакции комитом
        /// </summary>
        public static event DbTransactionScopeEnd TransactionCommit;

        /// <summary>
        /// Событие окончание транзакции откатом
        /// </summary>
        public static event DbTransactionScopeEnd TransactionRollback;
    }
}
