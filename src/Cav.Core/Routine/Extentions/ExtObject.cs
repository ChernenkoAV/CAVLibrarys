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
    public static object? GetDefault(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return null;
    }

    /// <summary>
    /// Выражение "null если" для типов структур
    /// </summary>
    /// <typeparam name="T">Тип структуры</typeparam>
    /// <param name="exp">Проверяемое выражение</param>
    /// <param name="operand">Операнд сравнения</param>
    /// <returns></returns>
    public static T? NullIf<T>(this T exp, T operand)
        where T : struct =>
#pragma warning disable IDE0004 //: Приведение избыточно. (в 4.8 пока нету)
        exp.Equals(operand) ? null : exp;
#pragma warning restore IDE0004 //: Приведение избыточно. (в 4.8 пока нету)

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

        var elnts = pathProperty.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

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
    /// <param name="arg">Проверяемый аргумент</param>
    /// <param name="args">Перечень значений</param>
    /// <returns>Если аргумент IsNullOrWhiteSpace() результат всегда false</returns>
    public static bool In(this string arg, params string[] args) =>
        !arg.IsNullOrWhiteSpace() && args.Contains(arg);

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

}
