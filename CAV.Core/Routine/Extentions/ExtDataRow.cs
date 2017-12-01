using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Cav
{
    /// <summary>
    /// Взаимодействие с DataRow(DataRowView)
    /// </summary>
    public static class ExtDataRow
    {
        /// <summary>
        /// Получение значения поля ID в DataRow. Если поля нет - исключение...
        /// </summary>
        /// <param name="drow"></param>
        /// <returns></returns>
        public static Guid GetID(this DataRow drow)
        {
            return (Guid)drow["ID"];
        }

        /// <summary>
        /// Получение значения поля ID в DataRow. Если поля нет - исключение...
        /// </summary>
        /// <param name="drow"></param>
        /// <returns></returns>
        public static Guid? GetParentID(this DataRow drow)
        {
            return drow.GetColumnValue("Parent") as Guid?;
        }

        /// <summary>
        /// Получение значения поля ID в DataRowView. Если поля нет - исключение...
        /// </summary>
        /// <param name="drow"></param>
        /// <returns></returns>
        public static Guid GetID(this DataRowView drow)
        {
            return drow.Row.GetID();
        }

        /// <summary>
        /// Получить значение поля
        /// </summary>
        /// <param name="row">Строка таблицы</param>
        /// <param name="ColumnName">Наименование поля</param>
        /// <returns>Значение (если значения нет, то null)</returns>
        public static object GetColumnValue(this DataRow row, string ColumnName = "ID")
        {
            if (row == null)
                return null;

            var value = row.IsNull(ColumnName) ? null : row[ColumnName];

            if (value is DBNull)
                return null;

            return value;
        }

        /// <summary>
        /// Получиние строки ИД-ков из коллекции DataRow
        /// </summary>        
        /// <param name="rows">Коллекция</param>
        /// <param name="columnName">Колонка для получения значений (Для коллекции DataRow)</param>
        /// <returns>Строка ИД-ков через ','</returns>
        public static String MakeIDsList(this IEnumerable<DataRow> rows, string columnName = "ID")
        {
            return rows
                .Select(x => x.GetColumnValue(columnName))
                .Where(x => x != null && !(x is DBNull))
                .JoinValuesToString();
        }

        // дотНет 3.5 не умеет кастить коллекции. сцучко.... 
        /// <summary>
        /// Получение коллекции значений поля из DataTable
        /// </summary>
        /// <typeparam name="T">Результирующий тип</typeparam>
        /// <param name="Table">Таблица</param>
        /// <param name="ColumnName">Наименование колонки</param>
        /// <returns>Лист значений</returns>
        public static List<T> GetColumnValues<T>(this DataTable Table, String ColumnName = "ID")
        {
            return Table.Rows
                .Cast<DataRow>()
                .GetColumnValues<T>(ColumnName);
        }

        /// <summary>
        /// Получение коллекции значений поля из коллекции строк DataRow
        /// </summary>
        /// <typeparam name="T">Результирующий тип</typeparam>
        /// <param name="ERows">коллекция строк</param>
        /// <param name="ColumnName">Наименование колонки</param>
        /// <returns>Лист значений</returns>
        public static List<T> GetColumnValues<T>(this IEnumerable<DataRow> ERows, String ColumnName = "ID")
        {
            return ERows
                .Select(x => (T)x.GetColumnValue(ColumnName))
                .Distinct()
                .ToList();
        }
    }
}
