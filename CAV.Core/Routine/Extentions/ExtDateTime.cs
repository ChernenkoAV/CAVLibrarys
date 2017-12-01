using System;

namespace Cav
{
    /// <summary>
    /// Расширения для даты-времени
    /// </summary>
    public static class ExtDateTime
    {
        #region Кварталы даты

        /// <summary>
        /// Получение квартала указанной даты
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int Quarter(this DateTime dateTime)
        {
            return ((dateTime.Month - 1) / 3) + 1;
        }

        /// <summary>
        /// Получение первого дня квартала, в котором находится указанная дата
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime FirstDayQuarter(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, (dateTime.Quarter() * 3) - 2, 1, 0, 0, 0, dateTime.Kind);
        }

        /// <summary>
        /// Получение последнего дня квартала. Время 23:59:59.9999
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime LastDayQuarter(this DateTime dateTime)
        {
            return dateTime.Add(-dateTime.TimeOfDay).AddDays(-dateTime.Day + 1).AddMonths((dateTime.Quarter() * 3) - dateTime.Month + 1).AddMilliseconds(-1);
        }

        #endregion
    }
}
