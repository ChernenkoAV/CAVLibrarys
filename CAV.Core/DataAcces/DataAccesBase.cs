using System;
using System.Data.Common;

namespace Cav.DataAcces
{
    /// <summary>
    /// Базовый клас для доступа к функционалу, реализованному в БД. Например, вызову хранимых процедур, возвращающих скалярное значение
    /// </summary>
    public class DataAccesBase
    {
        /// <summary>
        /// Обработчик исключения пры запуске DbCommand. Должен генерировать новое исключение (Для обертки "страшных" сиключений в "нестрашные")
        /// </summary>
        public Action<Exception> ExceptionHandlingExecuteCommand { get; set; }
        /// <summary>
        /// Получение объекта DbCommand при наличии настроенного соединения с БД
        /// </summary>
        /// <returns></returns>
        protected DbCommand CreateCommandObject()
        {
            DbCommand command = null;
            try
            {
                command = DbTransactionScope.Connection.CreateCommand();
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
                DisposeConnection(command);
            }
            return command;
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
                return tuneCommand(cmd).ExecuteScalar();
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
        /// Выполнене команды с возвратом DbDataReader. После обработки данных необходимо выполнить DisposeConnection(DbCommand cmd)
        /// </summary>
        /// <param name="cmd">команда</param>
        /// <returns>Ридер</returns>
        protected DbDataReader ExecuteReader(DbCommand cmd)
        {
            try
            {
                return tuneCommand(cmd).ExecuteReader();
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
                return tuneCommand(cmd).ExecuteNonQuery();
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
            cmd.Transaction = null;
            var conn = cmd.Connection;
            cmd.Connection = null;
            if (DbTransactionScope.Transaction == null)
                conn.Dispose();
        }

        private DbCommand tuneCommand(DbCommand cmd)
        {
            cmd.Connection = DbTransactionScope.Connection;
            cmd.Transaction = DbTransactionScope.Transaction;
            return cmd;
        }
    }
}
