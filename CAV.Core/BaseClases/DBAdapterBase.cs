using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace Cav.BaseClases
{
    /// <summary>
    /// Базовый класс для создания адаптеров к БД
    /// </summary>
    public class DBAdapterBase : Component
    {
        /// <summary>
        /// Имя соединения для использования адаптером
        /// </summary>
        protected String SqlConnectionName = null;

        private SqlConnection connection = null;

        /// <summary>
        /// Получение открытого соединения с БД 
        /// </summary>
        protected SqlConnection Connection
        {
            get
            {
                if (transaction != null && connection != null)
                    return connection;

                if (connection != null)
                    return connection;

                connection = DomainContext.Connection(SqlConnectionName);
                return connection;
            }
        }

        /// <summary>
        /// Закрытие соединения. Обязательно вызывать в конце выполнения методов работы с БД
        /// Иначе будут утечки памяти на соединения с БД.
        /// </summary>        
        public void CloseConnection()
        {
            if (transaction != null)
                return;
            try
            {
                if (connection != null)
                    connection.Dispose();
            }
            catch
            {
                // коннекшен уже задиспозен
            }
            connection = null;
        }

        #region Работа с транзакциями

        private SqlTransaction transaction = null;

        /// <summary>
        /// Маркер транзакции относительно адаптеров
        /// </summary>
        private Guid? rootTranMarker = null;

        /// <summary>
        /// Локальный маркер транзакции относительно текущего адаптера
        /// </summary>
        private Guid? localTranMarker = null;

        /// <summary>
        /// Начать транзакцию
        /// </summary>
        public void BeginTransaction()
        {
            if (localTranMarker.HasValue)
                return;
            localTranMarker = Guid.NewGuid();

            if (rootTranMarker.HasValue)
                return;
            rootTranMarker = localTranMarker;

            transaction = this.Connection.BeginTransaction();
        }

        /// <summary>
        /// Завершить транзакцию
        /// </summary>
        public void CommintTransaction()
        {
            if (!localTranMarker.HasValue)
                return;
            if (localTranMarker != rootTranMarker)
                return;

            try
            {
                transaction.Commit();
            }
            catch
            {
                throw;
            }
            finally
            {
                rootTranMarker = null;
                localTranMarker = null;

                transaction.Dispose();
                transaction = null;

                CloseConnection();
            }
        }

        /// <summary>
        /// Откатить транзакцию
        /// </summary>
        public void RollbackTransaction()
        {
            try
            {
                if (transaction != null)
                    transaction.Rollback();
            }
            catch
            {
                throw;
            }
            finally
            {
                localTranMarker = null;
                rootTranMarker = null;

                if (transaction != null)
                    transaction.Dispose();

                transaction = null;

                CloseConnection();
            }
        }

        #endregion

        /// <summary>
        /// Настройка комманды перед использованием
        /// </summary>
        /// <param name="comm"></param>
        /// <returns></returns>
        private SqlCommand tuneCommand(SqlCommand comm)
        {
            comm.CommandTimeout = 0;
            comm.Connection = this.Connection;
            comm.Transaction = transaction;
            return comm;
        }


        #region Выполнение комманд

        /// <summary>
        /// Выполнение команды с открытием соединения с БД
        /// </summary>
        /// <param name="comm"></param>
        /// <returns>Количество задействованных строк</returns>
        protected int ExecuteNonQuery(SqlCommand comm)
        {
            return tuneCommand(comm).ExecuteNonQuery();
        }

        /// <summary>
        /// Выполнение команды с открытием соединения с БД
        /// </summary>
        /// <param name="comm"></param>
        /// <returns>SqlDataReader</returns>
        protected SqlDataReader ExecuteReader(SqlCommand comm)
        {
            return tuneCommand(comm).ExecuteReader();
        }

        /// <summary>
        /// Выполнение команды с открытием соединения с БД
        /// </summary>
        /// <param name="comm"></param>
        /// <returns>Object</returns>
        protected Object ExecuteScalar(SqlCommand comm)
        {
            return tuneCommand(comm).ExecuteScalar();
        }

        /// <summary>
        /// Выполнение команды с открытием соединения с БД
        /// </summary>
        /// <param name="comm"></param>
        /// <returns>XmlReader</returns>
        protected XmlReader ExecuteXmlReader(SqlCommand comm)
        {
            return tuneCommand(comm).ExecuteXmlReader();
        }

        #endregion

        #region Выполнение адаптеров, Заполнение Датасетов

        private List<SqlCommand> GetCommands(SqlDataAdapter Adapter)
        {
            var lcmd = new List<SqlCommand>();
            if (Adapter.SelectCommand != null)
                lcmd.Add(Adapter.SelectCommand);
            if (Adapter.InsertCommand != null)
                lcmd.Add(Adapter.InsertCommand);
            if (Adapter.UpdateCommand != null)
                lcmd.Add(Adapter.UpdateCommand);
            if (Adapter.DeleteCommand != null)
                lcmd.Add(Adapter.DeleteCommand);
            return lcmd;
        }


        /// <summary>
        /// Заполнение таблиц командой + транзакция
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="Tables"></param>
        protected void FillTables(SqlCommand Command, params DataTable[] Tables)
        {
            (new SqlDataAdapter(tuneCommand(Command))).Fill(0, 0, Tables);
        }


        /// <summary>
        /// Заполнение таблиц адаптером + транзакция
        /// </summary>
        /// <param name="Adapter"></param>
        /// <param name="Tables"></param>
        protected void FillTables(SqlDataAdapter Adapter, params DataTable[] Tables)
        {
            foreach (var comm in GetCommands(Adapter))
                tuneCommand(comm);
            Adapter.Fill(0, 0, Tables);
        }

        /// <summary>
        /// Выполнение Update адаптера с присвоением транзанкции
        /// </summary>
        /// <param name="Adapter">Адаптер с командами обновления</param>
        /// <param name="Table">Таблица для обновления</param>
        protected void UpdateAdapter(SqlDataAdapter Adapter, DataTable Table)
        {
            foreach (var comm in GetCommands(Adapter))
                tuneCommand(comm);
            Adapter.Update(Table);
        }

        /// <summary>
        /// Выполнение Update адаптера с присвоением транзанкции
        /// </summary>
        /// <param name="Adapter">Адаптер с командами обновления</param>
        /// <param name="DataRows">Строки для обновления</param>
        protected void UpdateAdapter(SqlDataAdapter Adapter, DataRow[] DataRows)
        {
            foreach (var comm in GetCommands(Adapter))
                tuneCommand(comm);
            Adapter.Update(DataRows);
        }

        #endregion

        /// <summary>
        /// Dispose(bool disposing)
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            CloseConnection();

            base.Dispose(disposing);
        }

        /// <summary>
        /// Компонент находится в режиме дизайнера
        /// </summary>
        protected Boolean IsDesignMode
        {
            get
            {
                return this.DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
            }
        }
    }
}
