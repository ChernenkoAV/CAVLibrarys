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

        /// <summary>
        /// Усечение даты-времени (скопированно с https://stackoverflow.com/questions/1004698/how-to-truncate-milliseconds-off-of-a-net-datetime)
        /// </summary>
        /// <param name="self">Экземпляр <see cref="System.DateTime"/></param>
        /// <param name="resolution">Точность, до которой усечь</param>
        /// <returns></returns>
        public static DateTime Truncate(this DateTime self, DateTimeTruncateResolution resolution = DateTimeTruncateResolution.Second)
        {
            switch (resolution)
            {
                case DateTimeTruncateResolution.Year:
                    return new DateTime(self.Year, 1, 1, 0, 0, 0, 0, self.Kind);
                case DateTimeTruncateResolution.Month:
                    return new DateTime(self.Year, self.Month, 1, 0, 0, 0, self.Kind);
                case DateTimeTruncateResolution.Day:
                    return new DateTime(self.Year, self.Month, self.Day, 0, 0, 0, self.Kind);
                case DateTimeTruncateResolution.Hour:
                    return self.AddTicks(-(self.Ticks % TimeSpan.TicksPerHour));
                case DateTimeTruncateResolution.Minute:
                    return self.AddTicks(-(self.Ticks % TimeSpan.TicksPerMinute));
                case DateTimeTruncateResolution.Second:
                    return self.AddTicks(-(self.Ticks % TimeSpan.TicksPerSecond));
                case DateTimeTruncateResolution.Millisecond:
                    return self.AddTicks(-(self.Ticks % TimeSpan.TicksPerMillisecond));
                default:
                    throw new ArgumentException(nameof(resolution));
            }
        }
    }

    /// <summary>
    /// Точность, до которой усечь экземпляр <see cref="System.DateTime"/>
    /// </summary>
    public enum DateTimeTruncateResolution
    {
        /// <summary>
        /// Год
        /// </summary>
        Year,
        /// <summary>
        /// Месяц
        /// </summary>
        Month,
        /// <summary>
        /// День
        /// </summary>
        Day,
        /// <summary>
        /// Час
        /// </summary>
        Hour,
        /// <summary>
        /// Минута
        /// </summary>
        Minute,
        /// <summary>
        /// Секунда
        /// </summary>
        Second,
        /// <summary>
        /// Миллисекунда
        /// </summary>
        Millisecond
    }
}