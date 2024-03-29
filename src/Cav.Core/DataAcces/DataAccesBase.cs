﻿using System.Data;
using System.Data.Common;

namespace Cav.DataAcces;

/// <summary>
/// Базовый клас для доступа к функционалу, реализованному в БД. Например, вызову хранимых процедур, возвращающих скалярное значение
/// </summary>
public class DataAccesBase : IDataAcces
{
    /// <summary>
    /// Обработчик исключения пры запуске <see cref="DbCommand"/>. Должен генерировать новое исключение (Для обертки "страшных" сиключений в "нестрашные")
    /// </summary>
    public Action<Exception> ExceptionHandlingExecuteCommand { get; set; } = _ => { };

    /// <summary>
    /// Метод, выполняемый перед выполнением <see cref="DbCommand"/>. Возвращаемое значение - объект кореляции вызовов (с <see cref="MonitorCommandAfterExecute"/>)
    /// </summary>
    /// <remarks>Метод выполняется обернутым в try cath.</remarks>
    public Func<object?> MonitorCommandBeforeExecute { get; set; } = () => null;
    /// <summary>
    /// Метод, выполняемый после выполнения <see cref="DbCommand"/>.
    /// <see cref="string"/> - текст команды,
    /// <see cref="object"/> - объект кореляции, возвращяемый из <see cref="MonitorCommandBeforeExecute"/> (либо null, если <see cref="MonitorCommandBeforeExecute"/> == null),
    /// <see cref="DbParameter"/>[] - копия параметров, с которыми отработала команда <see cref="DbCommand"/>.
    /// </summary>
    /// <remarks>Метод выполняется в отдельном потоке, обернутый в try cath.</remarks>
    public Action<string, object, DbParameter[]> MonitorCommandAfterExecute { get; set; } = (_, __, ___) => { };

    private object? monitorHelperBefore()
    {
        if (MonitorCommandBeforeExecute != null)
            try
            {
                return MonitorCommandBeforeExecute.Invoke();
            }
            catch { }

        return null;
    }
    private void monitorHelperAfter(DbCommand command, object? objColrn)
    {
        if (MonitorCommandAfterExecute == null)
            return;

        var cmndText = command.CommandText;
        var dbParm = new DbParameter[command.Parameters.Count];
        if (command.Parameters.Count > 0)
            command.Parameters.CopyTo(dbParm, 0);
        new Task(mcaftexex!, Tuple.Create(MonitorCommandAfterExecute, cmndText, objColrn, dbParm)).Start();

    }
    private static void mcaftexex(object o)
    {
        try
        {
            var p = (Tuple<Action<string, object, DbParameter[]>, string, object, DbParameter[]>)o;
            p.Item1(p.Item2, p.Item3, p.Item4);
        }
        catch { }
    }

    /// <summary>
    /// Выполнять команды в изолированном соедении к БД. (То есть, вне транзакции, которая может быть начата)
    /// </summary>
    protected bool ExecuteIsolationConnection { get; set; }

    /// <summary>
    /// Имя соединения, с которым будет работать текущий объект
    /// </summary>
    protected string? ConnectionName { get; set; }
    /// <summary>
    /// Получение объекта DbCommand при наличии настроенного соединения с БД
    /// </summary>
    /// <returns></returns>
    protected DbCommand? CreateCommandObject() => DbContext.DbProviderFactory(ConnectionName).CreateCommand();

    /// <summary>
    /// Выполняет запрос и возвращает первый столбец первой строки результирующего набора, возвращаемого запросом. Все другие столбцы и строки игнорируются.
    /// </summary>
    /// <param name="cmd">команда</param>
    /// <returns>Результат выполнения команды</returns>
    protected object? ExecuteScalar(DbCommand cmd)
    {
        if (cmd is null)
            throw new ArgumentNullException(nameof(cmd));

        try
        {
            var correlationObject = monitorHelperBefore();

            var res = tuneCommand(cmd).ExecuteScalar();

            monitorHelperAfter(cmd, correlationObject);

            return res;
        }
        catch (Exception ex)
        {
            if (ExceptionHandlingExecuteCommand != null)
                ExceptionHandlingExecuteCommand(ex);
            else
                throw;
        }
        finally
        {
            DisposeConnection(cmd);
        }

        throw new InvalidOperationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
    }

    /// <summary>
    /// Выполнене команды с возвратом DbDataReader. После обработки данных необходимо выполнить <see cref="DisposeConnection(DbCommand)"/>
    /// </summary>
    /// <param name="cmd">команда</param>
    /// <returns>Ридер</returns>
    protected DbDataReader ExecuteReader(DbCommand cmd)
    {
        if (cmd is null)
            throw new ArgumentNullException(nameof(cmd));

        try
        {
            var correlationObject = monitorHelperBefore();

            var res = tuneCommand(cmd).ExecuteReader();

            monitorHelperAfter(cmd, correlationObject);

            return res;
        }
        catch (Exception ex)
        {
            if (ExceptionHandlingExecuteCommand != null)
                ExceptionHandlingExecuteCommand(ex);
            else
                throw;
        }

        throw new InvalidOperationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
    }

    /// <summary>
    /// Выполнение команды без возврата данных
    /// </summary>
    /// <param name="cmd">команда</param>
    /// <returns>Количество затронутых строк</returns>
    protected int ExecuteNonQuery(DbCommand cmd)
    {
        if (cmd is null)
            throw new ArgumentNullException(nameof(cmd));

        try
        {
            var correlationObject = monitorHelperBefore();

            var res = tuneCommand(cmd).ExecuteNonQuery();

            monitorHelperAfter(cmd, correlationObject);

            return res;
        }
        catch (Exception ex)
        {
            if (ExceptionHandlingExecuteCommand != null)
                ExceptionHandlingExecuteCommand(ex);
            else
                throw;
        }
        finally
        {
            DisposeConnection(cmd);
        }

        throw new InvalidOperationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
    }

    /// <summary>
    /// Получение результата в <see cref="DataTable"/>. (Заполняется через <see cref="DataTable.Load(IDataReader)"/> из результата типа <see cref="DbDataReader"/> метода <see cref="DbCommand.ExecuteReader()"/>)
    /// </summary>
    /// <param name="cmd">Команда на выполенение.</param>
    /// <returns>Результат работы команды</returns>
    protected DataTable FillTable(DbCommand cmd)
    {
        if (cmd is null)
            throw new ArgumentNullException(nameof(cmd));

        try
        {
#pragma warning disable CA2000 // Ликвидировать объекты перед потерей области
            var res = new DataTable();
#pragma warning restore CA2000 // Ликвидировать объекты перед потерей области

            var correlationObject = monitorHelperBefore();

            using (var reader = ExecuteReader(cmd))
                res.Load(reader);

            monitorHelperAfter(cmd, correlationObject);

            return res;
        }
        catch (Exception ex)
        {
            if (ExceptionHandlingExecuteCommand != null)
                ExceptionHandlingExecuteCommand(ex);
            else
                throw;
        }
        finally
        {
            DisposeConnection(cmd);
        }

        throw new InvalidOperationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
    }

    /// <summary>
    /// Освобождение соедиения с БД
    /// </summary>
    /// <param name="cmd"></param>
    protected void DisposeConnection(DbCommand cmd)
    {
        if (cmd == null)
            return;

        var cmdConn = cmd.Connection;
        var cmdTran = cmd.Transaction;

        cmd.Transaction = null;
        cmd.Connection = null;
        try
        {
            cmd.Dispose();
        }
        catch { }

        var tran = DbTransactionScope.TransactionGet(ConnectionName);

        if (tran != null && tran.Connection == null)
            throw new InvalidOperationException("Несогласованное состояние объекта транзакции. Соедиение с БД сброшено.");

        if (tran != null)
        {
            if (cmdConn == null || cmdTran == null)
                throw new InvalidOperationException("Несогласованное состояние объекта транзакции в команде. Соедиение с БД сброшено.");
        }
        else if (cmdConn != null)
            try
            {
                cmdConn.Close();
                cmdConn.Dispose();
            }
            catch { }
    }

    private DbCommand tuneCommand(DbCommand cmd)
    {
        if (ExecuteIsolationConnection)
            cmd.Connection = DbContext.Connection(ConnectionName);
        else
        {
            cmd.Connection = DbTransactionScope.Connection(ConnectionName);
            cmd.Transaction = DbTransactionScope.TransactionGet(ConnectionName);
        }

        return cmd;
    }
}
