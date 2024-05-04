using System.Collections.Concurrent;
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

    private bool complete;
    private string connName;
    private static AsyncLocal<Guid?> rootTran = new();

    private static ConcurrentDictionary<string, DbTransaction> transactions = [];

    private readonly Guid currentTran;

    private static string getKeyTran(string? conName) =>
        $"{conName ?? DbContext.defaultNameConnection}|{rootTran.Value}";

    /// <summary>
    /// Создание нового экземпляра обертки транзации
    /// </summary>
    /// <param name="connectionName">Имя соедиенения, для которого назначается транзакция</param>
    public DbTransactionScope(string? connectionName = null)
    {
        currentTran = Guid.NewGuid();

        connName = connectionName ??= DbContext.defaultNameConnection;

        if (rootTran.Value == null)
            rootTran.Value = currentTran;

        if (rootTran.Value != currentTran)
            return;

        if (TransactionGet(connName) == null)
            transactions.AddOrUpdate(
                getKeyTran(connName),
                DbContext.Connection(connName).BeginTransaction(),
                (k, t) => t);
    }

    internal static DbTransaction? TransactionGet(string? connectionName = null)
    {
        transactions.TryGetValue(getKeyTran(connectionName), out var res);

        return res != null && res.Connection == null
            ? throw new InvalidOperationException("TransactionGet Несогласованное состояние объекта транзакции. Соедиение с БД сброшено.")
            : res;
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
        var tranKey = getKeyTran(connName);
        transactions.TryGetValue(tranKey, out var tran);

        if (tran != null && !complete)
        {
            transactions.TryRemove(tranKey, out var _);
            rootTran.Value = null;

            var conn = tran.Connection;

            tran.Rollback();
            tran.Dispose();

            conn?.Dispose();

            TransactionRollback?.Invoke(connName!);
        }

        if (rootTran.Value != currentTran)
            return;

        if (tran != null)
        {
            transactions.TryRemove(tranKey, out var _);
            rootTran.Value = null;

            var conn = tran.Connection;

            tran.Commit();
            tran.Dispose();

            conn!.Dispose();

            TransactionCommit?.Invoke(connName!);
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
