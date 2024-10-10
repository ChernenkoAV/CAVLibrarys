using System.Diagnostics.CodeAnalysis;
using Cav.ReflectHelpers;

namespace Cav;

/// <summary>
/// Расширения для работы с объектами
/// </summary>
public static class ExtObject
{
    /// <summary>
    /// Получение значения по умолчанию для типа
    /// </summary>
    /// <param name="type">Тип, для которого необходимо получить значение</param>
    /// <returns>Значение по уполчанию</returns>
    public static object? GetDefault(this Type type) =>
        type is null
            ? throw new ArgumentNullException(nameof(type))
            : type.IsValueType
                ? Activator.CreateInstance(type)
                : null;

    /// <summary>
    /// Выражение "null если" для типов структур
    /// </summary>
    /// <typeparam name="T">Тип структуры</typeparam>
    /// <param name="exp">Проверяемое выражение</param>
    /// <param name="operand">Операнд сравнения</param>
    /// <returns></returns>
    public static T? NullIf<T>(this T exp, T operand)
        where T : struct =>
        exp.Equals(operand) ? null : exp;

    /// <summary>
    /// Повтор IFNULL() из T-SQL для структур
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="val">Проверяемое значение</param>
    /// <param name="operand">Значение подстановки</param>
    /// <returns></returns>
    public static T IfNull<T>(this T? val, T operand)
        where T : struct =>
        val ?? operand;

    private static readonly char[] separator = ['.'];

    /// <summary>
    /// Получение свойства у объекта. Обработка вложеных объектов
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="pathProperty">Путь к свойству вида "PropertyA.PropertyB.PropertyC"</param>
    /// <param name="throwIfObjectIsNull">Вернуть исключение, если вложеный объект = null, либо результат - null</param>
    /// <returns></returns>
    public static object? GetPropertyValueNestedObject<T>(this T obj, string pathProperty, bool throwIfObjectIsNull = false) where T : class
    {
        if (pathProperty.IsNullOrWhiteSpace())
            return null;

        var elnts = pathProperty.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        object? res = obj;

        try
        {
            foreach (var el in elnts)
                res = res!.GetPropertyValue(el);
        }
        catch
        {
            if (throwIfObjectIsNull)
                throw;
            return null;
        }

        return res;
    }

    #region In

    /// <summary>
    /// Проверка вхождения значения в перечень
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arg">Проверяемый аргумент</param>
    /// <param name="args">Перечень значений</param>
    /// <returns></returns>
    public static bool In<T>(this T arg, params T[] args)
        where T : struct =>
        args.Contains(arg);

    /// <summary>
    /// Проверка на вхождение значения в перечень (для Nullable-типов)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arg">Проверяемый аргумент</param>
    /// <param name="args">Перечень значений</param>
    /// <returns></returns>
    public static bool In<T>(this T? arg, params T?[] args)
        where T : struct =>
        arg.HasValue && args.Contains(arg);

    #endregion

    #region Between

    #region int

    /// <summary>
    /// Принадлежность диапазону. Аналог BETWEEN в SQL.
    /// </summary>
    /// <param name="val">Значение</param>
    /// <param name="left">Левая граница диапазона</param>
    /// <param name="right">Правая граница диапазона</param>
    /// <returns></returns>
    public static bool Between(this int val, int left, int right) =>
        val >= left && val <= right;

    /// <summary>
    /// Принадлежность диапазону. Аналог BETWEEN в SQL - если любой параметр предиката <see langword="null"/> - результат <see langword="false"/>.
    /// </summary>
    /// <param name="val">Значение</param>
    /// <param name="left">Левая граница диапазона</param>
    /// <param name="right">Правая граница диапазона</param>
    /// <returns></returns>
    public static bool Between(this int? val, int? left, int? right) =>
        val is not null && left is not null && right is not null &&
        val.Value.Between(left.Value, right.Value);

    #endregion

    #region datetime

    /// <summary>
    /// Принадлежность диапазону. Аналог BETWEEN в SQL.
    /// </summary>
    /// <param name="val">Значение</param>
    /// <param name="left">Левая граница диапазона</param>
    /// <param name="right">Правая граница диапазона</param>
    /// <param name="truncateTime">Усечение времени и сравнение только дат</param>
    /// <returns></returns>
    public static bool Between(this DateTime val, DateTime left, DateTime right, bool truncateTime = true) =>
        truncateTime
        ? val.Date >= left.Date && val <= right.Date
        : val >= left && val <= right;

    /// <summary>
    /// Принадлежность диапазону. Аналог BETWEEN в SQL - если любой параметр предиката <see langword="null"/> - результат <see langword="false"/>.
    /// </summary>
    /// <param name="val">Значение</param>
    /// <param name="left">Левая граница диапазона</param>
    /// <param name="right">Правая граница диапазона</param>
    /// <param name="truncateTime">Усечение времени и сравнение только дат</param>
    /// <returns></returns>
    public static bool Between(this DateTime? val, DateTime? left, DateTime? right, bool truncateTime = true) =>
        val is not null && left is not null && right is not null &&
        val.Value.Between(left.Value, right.Value, truncateTime);

    #endregion

    #endregion

    /// <summary>
    /// Техническая проверка на <see langword="null"/>. Для строки еще на <see cref="string.IsNullOrWhiteSpace(string)"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="test"></param>
    /// <param name="paramName">Имя параметра или иной текст для исключения</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException" />
    public static T ChekNull<T>(this T? test, string? paramName) =>
        test == null || (test is string strTest && strTest.IsNullOrWhiteSpace())
            ? throw new ArgumentNullException(paramName)
            : test;

    /// <summary>
    /// Техническая проверка на <see langword="null"/>. Для строки еще на <see cref="string.IsNullOrWhiteSpace(string)"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="test"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Имя параметра или иной текст для исключения</exception>
    public static async Task<T> ChekNullAsync<T>([NotNull] this Task<T?> test, string? paramName) => (await test.ConfigureAwait(false)).ChekNull(paramName);

}
