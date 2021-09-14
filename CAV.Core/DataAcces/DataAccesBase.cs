using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Cav.DataAcces
{
    /// <summary>
    /// Базовый клас для доступа к функционалу, реализованному в БД. Например, вызову хранимых процедур, возвращающих скалярное значение
    /// </summary>
    public class DataAccesBase : IDataAcces
    {
        /// <summary>
        /// Обработчик исключения пры запуске <see cref="DbCommand"/>. Должен генерировать новое исключение (Для обертки "страшных" сиключений в "нестрашные")
        /// </summary>
        public Action<Exception> ExceptionHandlingExecuteCommand { get; set; }

        /// <summary>
        /// Метод, выполняемый перед выполнением <see cref="DbCommand"/>. Возвращаемое значение - объект кореляции вызовов (с <see cref="DataAccesBase.MonitorCommandAfterExecute"/>)
        /// </summary>
        /// <remarks>Метод выполняется обернутым в try cath.</remarks>
        public Func<Object> MonitorCommandBeforeExecute { get; set; }
        /// <summary>
        /// Метод, выполняемый после выполнения <see cref="DbCommand"/>.
        /// <see cref="String"/> - текст команды,
        /// <see cref="Object"/> - объект кореляции, возвращяемый из <see cref="DataAccesBase.MonitorCommandBeforeExecute"/> (либо null, если <see cref="DataAccesBase.MonitorCommandBeforeExecute"/> == null),
        /// <see cref="DbParameter"/>[] - копия параметров, с которыми отработала команда <see cref="DbCommand"/>.
        /// </summary>
        /// <remarks>Метод выполняется в отдельном потоке, обернутый в try cath.</remarks>
        public Action<String, Object, DbParameter[]> MonitorCommandAfterExecute { get; set; }

        private Object monitorHelperBefore()
        {
            if (MonitorCommandBeforeExecute != null)
                try
                {
                    return MonitorCommandBeforeExecute.Invoke();
                }
                catch { }

            return null;
        }
        private void monitorHelperAfter(DbCommand command, object objColrn)
        {
            if (MonitorCommandAfterExecute == null)
                return;

            var cmndText = command.CommandText;
            var dbParm = new DbParameter[command.Parameters.Count];
            if (command.Parameters.Count > 0)
                command.Parameters.CopyTo(dbParm, 0);
            (new Task(mcaftexex, Tuple.Create(MonitorCommandAfterExecute, cmndText, objColrn, dbParm))).Start();

        }
        private static void mcaftexex(object o)
        {
            try
            {
                var p = (Tuple<Action<String, Object, DbParameter[]>, String, object, DbParameter[]>)o;
                p.Item1(p.Item2, p.Item3, p.Item4);
            }
            catch { }
        }

        private DbProviderFactory providerFactory = null;

        internal DbProviderFactory DbProviderFactoryGet()
        {
            if (providerFactory != null)
                return providerFactory;

            var tran = DbTransactionScope.TransactionGet(ConnectionName);
            if (tran != null)
                providerFactory = DbProviderFactories.GetFactory(tran.Connection);

            if (providerFactory == null)
            {
                var conn = DbTransactionScope.Connection(ConnectionName);
                providerFactory = DbProviderFactories.GetFactory(conn);
                if (DbTransactionScope.TransactionGet(ConnectionName) == null)
                    if (conn != null)
                        try
                        {
                            conn.Close();
                            conn.Dispose();
                        }
                        catch { }
            }

            return providerFactory;
        }

        /// <summary>
        /// Имя соединения, с которым будет работать текущий объект
        /// </summary>
        protected String ConnectionName = null;
        /// <summary>
        /// Получение объекта DbCommand при наличии настроенного соединения с БД
        /// </summary>
        /// <returns></returns>
        protected DbCommand CreateCommandObject()
        {
            return DbProviderFactoryGet().CreateCommand();
        }

        /// <summary>
        /// Выполняет запрос и возвращает первый столбец первой строки результирующего набора, возвращаемого запросом. Все другие столбцы и строки игнорируются.
        /// </summary>
        /// <param name="cmd">команда</param>
        /// <returns>Результат выполнения команды</returns>
        protected Object ExecuteScalar(DbCommand cmd)
        {
            try
            {
                Object correlationObject = monitorHelperBefore();

                Object res = tuneCommand(cmd).ExecuteScalar();

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

            throw new ApplicationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
        }

        /// <summary>
        /// Выполнене команды с возвратом DbDataReader. После обработки данных необходимо выполнить <see cref="DataAccesBase.DisposeConnection(DbCommand)"/>
        /// </summary>
        /// <param name="cmd">команда</param>
        /// <returns>Ридер</returns>
        protected DbDataReader ExecuteReader(DbCommand cmd)
        {
            try
            {
                Object correlationObject = monitorHelperBefore();

                DbDataReader res = tuneCommand(cmd).ExecuteReader();

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

            throw new ApplicationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
        }

        /// <summary>
        /// Выполнение команды без возврата данных
        /// </summary>
        /// <param name="cmd">команда</param>
        /// <returns>Количество затронутых строк</returns>
        protected int ExecuteNonQuery(DbCommand cmd)
        {
            try
            {
                Object correlationObject = monitorHelperBefore();

                int res = tuneCommand(cmd).ExecuteNonQuery();

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

            throw new ApplicationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
        }

        /// <summary>
        /// Получение результата в DataTable
        /// </summary>
        /// <param name="cmd">Команда на выполенение. Присваевается в SelectCommand DbDataAdapter`а</param>
        /// <returns>Результат работы команды</returns>
        protected DataTable FillTable(DbCommand cmd)
        {
            try
            {
                var res = new DataTable();

                using (var adapter = DbProviderFactoryGet().CreateDataAdapter())
                {
                    adapter.SelectCommand = tuneCommand(cmd);

                    Object correlationObject = monitorHelperBefore();

                    adapter.Fill(res);

                    monitorHelperAfter(cmd, correlationObject);
                }

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

            throw new ApplicationException("При обработке исключения выполнения команды дальнейшее выполнение невозможно.");
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
                throw new NullReferenceException("Несогласованное состояние объекта транзакции. Соедиение с БД сброшено.");

            if (tran != null)
            {
                if (cmdConn == null || cmdTran == null)
                    throw new NullReferenceException("Несогласованное состояние объекта транзакции в команде. Соедиение с БД сброшено.");
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
            var tran = DbTransactionScope.TransactionGet(ConnectionName);

            if (tran != null && tran.Connection == null)
                throw new NullReferenceException("Несогласованное состояние транзакции. Соедиение с БД сброшено.");

            cmd.Connection = tran?.Connection ?? DbTransactionScope.Connection(ConnectionName);

            cmd.Transaction = tran;

            return cmd;
        }
    }
}
