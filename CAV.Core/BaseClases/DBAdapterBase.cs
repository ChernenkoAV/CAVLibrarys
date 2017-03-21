using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Transactions;
using System.Xml;

namespace Cav.BaseClases
{
    /// <summary>
    /// Базовый класс для создания адаптеров к БД
    /// </summary>
    [Obsolete("Скоро будет удалено. Используйте DataAccesBase и его наследников", true)]
    // TODO Удалить
    public class DBAdapterBase : Component
    {
        /// <summary>
        /// Имя соединения для использования адаптером
        /// </summary>
        protected String ConnectionName = null;

        private DbConnection connection = null;

        /// <summary>
        /// Получение открытого соединения с БД 
        /// </summary>
        protected DbConnection Connection
        {
            get
            {
                if (Transaction.Current != null && connection != null)
                    return connection;

                if (connection != null)
                    return connection;

                connection = DomainContext.Connection(ConnectionName);
                return connection;
            }
        }

        /// <summary>
        /// Закрытие соединения. Обязательно вызывать в конце выполнения методов работы с БД
        /// Иначе будут утечки памяти на соединения с БД.
        /// </summary>        
        public void CloseConnection()
        {
            if (Transaction.Current != null)
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

        /// <summary>
        /// Настройка команды перед использованием
        /// </summary>
        /// <param name="comm"></param>
        /// <returns></returns>
        private DbCommand tuneCommand(DbCommand comm)
        {
            comm.CommandTimeout = 0;
            comm.Connection = this.Connection;
            return comm;
        }


        #region Выполнение команд

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
        /// <returns>DbDataReader</returns>
        protected DbDataReader ExecuteReader(DbCommand comm)
        {
            return tuneCommand(comm).ExecuteReader();
        }

        /// <summary>
        /// Выполнение команды с открытием соединения с БД
        /// </summary>
        /// <param name="comm"></param>
        /// <returns>Object</returns>
        protected Object ExecuteScalar(DbCommand comm)
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
            return ((SqlCommand)tuneCommand(comm)).ExecuteXmlReader();
        }

        #endregion

        #region Выполнение адаптеров, Заполнение Датасетов

        private List<DbCommand> GetCommands(DbDataAdapter Adapter)
        {
            var lcmd = new List<DbCommand>();
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
        /// Заполнение таблиц командой
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="Tables"></param>
        protected void FillTables(SqlCommand Command, params DataTable[] Tables)
        {
            (new SqlDataAdapter((SqlCommand)tuneCommand(Command))).Fill(0, 0, Tables);
        }


        /// <summary>
        /// Заполнение таблиц адаптером
        /// </summary>
        /// <param name="Adapter"></param>
        /// <param name="Tables"></param>
        protected void FillTables(DbDataAdapter Adapter, params DataTable[] Tables)
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
        protected void UpdateAdapter(DbDataAdapter Adapter, DataTable Table)
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
        protected void UpdateAdapter(DbDataAdapter Adapter, DataRow[] DataRows)
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
