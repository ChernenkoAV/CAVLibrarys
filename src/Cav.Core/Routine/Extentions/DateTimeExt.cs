namespace Cav;

/// <summary>
/// Расширения для даты-времени
/// </summary>
public static class DateTimeExt
{
    #region Кварталы даты

    /// <summary>
    /// Получение квартала указанной даты
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static int Quarter(this DateTime dateTime) => ((dateTime.Month - 1) / 3) + 1;

    /// <summary>
    /// Получение первого дня квартала, в котором находится указанная дата
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateTime FirstDayQuarter(this DateTime dateTime) => new(dateTime.Year, (dateTime.Quarter() * 3) - 2, 1, 0, 0, 0, dateTime.Kind);

    /// <summary>
    /// Получение последнего дня квартала. Время 23:59:59.9999
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateTime LastDayQuarter(this DateTime dateTime) =>
        dateTime.Add(-dateTime.TimeOfDay).AddDays(-dateTime.Day + 1).AddMonths((dateTime.Quarter() * 3) - dateTime.Month + 1).AddMilliseconds(-1);

    #endregion

    /// <summary>
    /// Усечение даты-времени (скопированно с https://stackoverflow.com/questions/1004698/how-to-truncate-milliseconds-off-of-a-net-datetime)
    /// </summary>
    /// <param name="self">Экземпляр <see cref="DateTime"/></param>
    /// <param name="resolution">Точность, до которой усечь</param>
    /// <returns></returns>
    public static DateTime Truncate(this DateTime self, DateTimeTruncateResolution resolution = DateTimeTruncateResolution.Second) =>
        resolution switch
        {
            DateTimeTruncateResolution.Year => new DateTime(self.Year, 1, 1, 0, 0, 0, 0, self.Kind),
            DateTimeTruncateResolution.Month => new DateTime(self.Year, self.Month, 1, 0, 0, 0, self.Kind),
            DateTimeTruncateResolution.Day => new DateTime(self.Year, self.Month, self.Day, 0, 0, 0, self.Kind),
            DateTimeTruncateResolution.Hour => self.AddTicks(-(self.Ticks % TimeSpan.TicksPerHour)),
            DateTimeTruncateResolution.Minute => self.AddTicks(-(self.Ticks % TimeSpan.TicksPerMinute)),
            DateTimeTruncateResolution.Second => self.AddTicks(-(self.Ticks % TimeSpan.TicksPerSecond)),
            DateTimeTruncateResolution.Millisecond => self.AddTicks(-(self.Ticks % TimeSpan.TicksPerMillisecond)),
            _ => throw new NotImplementedException(resolution.ToString()),
        };

    #region Возраст

    #region ExistsAge

    /// <summary>
    /// Проверка, есть ли между датами указанное количество полных лет
    /// </summary>
    /// <param name="date1">Дата 1</param>
    /// <param name="date2">Дата 2</param>
    /// <param name="years">Проверяемое количество полных лет</param>
    /// <returns></returns>
    public static bool ExistsAge(this DateTime date1, DateTime date2, int years)
    {
        var tdl = date1.Date;
        var tdg = date2.Date;

        if (tdl > tdg)
            (tdl, tdg) = (tdg, tdl);

        return tdl.AddYears(years) <= tdg;
    }

    /// <summary>
    /// Проверка, есть ли между датами указанное количество полных лет
    /// </summary>
    /// <param name="date1">Дата 1</param>
    /// <param name="date2">Дата 2</param>
    /// <param name="years">Проверяемое количество полных лет</param>
    /// <returns>false, если одна из дат = null</returns>
    public static bool ExistsAge(this DateTime? date1, DateTime date2, int years) =>
        date1.HasValue && date1.Value.ExistsAge(date2, years);

    /// <summary>
    /// Проверка, есть ли между датами указанное количество полных лет
    /// </summary>
    /// <param name="date1">Дата 1</param>
    /// <param name="date2">Дата 2</param>
    /// <param name="years">Проверяемое количество полных лет</param>
    /// <returns>false, если одна из дат = null</returns>
    public static bool ExistsAge(this DateTime? date1, DateTime? date2, int years) =>
        date2.HasValue && date1.ExistsAge(date2.Value, years);

    /// <summary>
    /// Проверка, есть ли между датами указанное количество полных лет
    /// </summary>
    /// <param name="date1">Дата 1</param>
    /// <param name="date2">Дата 2</param>
    /// <param name="years">Проверяемое количество полных лет</param>
    /// <returns>false, если одна из дат = null</returns>
    public static bool ExistsAge(this DateTime date1, DateTime? date2, int years) =>
        date2.HasValue && date1.ExistsAge(date2.Value, years);

    #endregion

    #region FullAge

    /// <summary>
    /// Количество полных лет на дату.
    /// </summary>
    /// <param name="date1">Дата 1</param>
    /// <param name="date2">Дата 2</param>
    /// <returns>Количество полных лет</returns>
    public static int FullAge(this DateTime date1, DateTime date2)
    {
        var tdl = date1.Date;
        var tdg = date2.Date;

        if (tdl > tdg)
            (tdl, tdg) = (tdg, tdl);

        var res = tdg.Year - tdl.Year;

        if (!tdl.ExistsAge(tdg, res))
            res--;

        return res;
    }

    /// <summary>
    /// Количество полных лет на дату
    /// </summary>
    /// <param name="date1">Дата 1. Если параметр <see langword="null"/> - результат <see langword="null"/></param> 
    /// <param name="date2">Дата 2</param>
    /// <returns>Количество полных лет</returns>
    public static int? FullAge(this DateTime? date1, DateTime date2) => date1?.FullAge(date2);

    /// <summary>
    /// Количество полных лет на дату
    /// </summary>
    /// <param name="date1">Дата 1</param>
    /// <param name="date2">Дата 2. Если параметр <see langword="null"/> - результат <see langword="null"/></param>
    /// <returns>Количество полных лет</returns>
    public static int? FullAge(this DateTime date1, DateTime? date2) => ((DateTime?)date1).FullAge(date2);

    /// <summary>
    /// Количество полных лет на дату. Если один из параметров <see langword="null"/> - результат <see langword="null"/>
    /// </summary>
    /// <param name="date1">Дата 1</param>
    /// <param name="date2">Дата 2</param>
    /// <returns>Количество полных лет</returns>
    public static int? FullAge(this DateTime? date1, DateTime? date2) => date2 is null ? null : date1?.FullAge(date2.Value);

    #endregion

    #endregion

    #region Первый и последний день месяца

    /// <summary>
    /// Первый день месяца
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime FirstDayMonth(this DateTime date) => new(date.Year, date.Month, 1);

    /// <summary>
    /// Первый день месяца
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime? FirstDayMonth(this DateTime? date) => date?.FirstDayMonth();

    /// <summary>
    /// Последний день месяца
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime LastDayMonth(this DateTime date) => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    /// <summary>
    /// Последний день месяца
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime? LastDayMonth(this DateTime? date) => date?.LastDayMonth();

    #endregion
}

/// <summary>
/// Точность, до которой усечь экземпляр <see cref="DateTime"/>
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