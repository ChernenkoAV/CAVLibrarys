using System.Data.Common;

#pragma warning disable CA1003 // Используйте экземпляры обработчика универсальных событий

namespace Cav;

/// <summary>
/// Делегат окончания работы транзакции
/// </summary>
/// <param name="connName">Имя соединения с БД</param>
public delegate void DbTransactionScopeEnd(string connName);

/// <summary>
/// "Груповая" транзакция. Обертка для вызовов в БД. Только для одного DbConnection
/// </summary>
public sealed class DbTransactionScope : IDisposable
{
    /// <summary>
    /// Создание нового экземпляра обертки транзации
    /// </summary>
    /// <param name="connectionName">Имя соедиенения, для которого назначается транзакция</param>
    public DbTransactionScope(string? connectionName = null)
    {
        currentTran = Guid.NewGuid();
        connName = connectionName.GetNullIfIsNullOrWhiteSpace() ?? DbContext.defaultNameConnection;

        if (rootTran.Value == null)
            rootTran.Value = currentTran;

        if (rootTran.Value != currentTran)
            return;

        if (TransactionGet(connName) == null)
        {
            if (transactions.Value is null)
                transactions.Value = [];

            transactions.Value.Add(connName, DbContext.Connection(connName).BeginTransaction());
        }
    }

    private bool complete;
    private string connName;
    private static AsyncLocal<Guid?> rootTran = new();

    private static AsyncLocal<Dictionary<string, DbTransaction>> transactions = new() { };

    private readonly Guid currentTran;

    internal static DbTransaction? TransactionGet(string? connectionName = null)
    {
        if (connectionName.IsNullOrWhiteSpace())
            connectionName = DbContext.defaultNameConnection;

        DbTransaction? res = null;
        transactions.Value?.TryGetValue(connectionName!, out res);
        return res;
    }

    #region Члены IDisposable

    /// <summary>
    /// Пометить, что транзакцию можно закомитить
    /// </summary>
    public void Complete() => complete = true;

    /// <summary>
    /// Реализация IDisposable
    /// </summary>
    public void Dispose()
    {
        var connNameEv = connName;

        if (connName.IsNullOrWhiteSpace())
            connName = DbContext.defaultNameConnection;

        var tran = TransactionGet(connName);

        if (tran != null && !complete)
        {
            transactions.Value!.Remove(connName!);
            rootTran.Value = null;

            var conn = tran.Connection;
            if (conn != null)
            {
                tran.Rollback();
                tran.Dispose();

                conn.Close();
                conn.Dispose();

                TransactionRollback?.Invoke(connNameEv!);
            }
        }

        if (rootTran.Value != currentTran)
            return;

        tran = TransactionGet(connName);
        if (tran != null)
        {
            transactions.Value!.Remove(connName!);
            rootTran.Value = null;

            var conn = tran.Connection;
            if (conn != null)
            {
                tran.Commit();
                tran.Dispose();

                conn.Close();
                conn.Dispose();

                TransactionCommit?.Invoke(connNameEv!);
            }
        }
    }

    #endregion

    /// <summary>
    /// Событие окончания транзакции комитом
    /// </summary>
    public static event DbTransactionScopeEnd TransactionCommit = null!;

    /// <summary>
    /// Событие окончание транзакции откатом
    /// </summary>
    public static event DbTransactionScopeEnd TransactionRollback = null!;
}
