using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Cav.ReflectHelpers;

/// <summary>
/// Расширения упрощения вызовов рефлексии
/// </summary>
public static class ExtReflection
{
    /// <summary>
    /// Получения значения свойства у объекта
    /// </summary>
    /// <param name="obj">экземпляр объекта</param>
    /// <param name="propertyName">Имя свойства</param>
    /// <returns></returns>
    public static object? GetPropertyValue(this object obj, string propertyName)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException($"\"{nameof(propertyName)}\" не может быть пустым или содержать только пробел.", nameof(propertyName));

        return obj.GetType().GetProperty(propertyName)!.GetValue(obj);
    }
    /// <summary>
    /// Получение значения статического свойства / константного поля
    /// </summary>
    /// <param name="asm">Сборка, сожержащая тип</param>
    /// <param name="className">Имя класса</param>
    /// <param name="namePropertyOrField">Имя свойства или поля</param>
    /// <returns></returns>
    public static object? GetStaticOrConstPropertyOrFieldValue(this Assembly asm, string className, string namePropertyOrField)
    {
        if (asm is null)
            throw new ArgumentNullException(nameof(asm));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException($"\"{nameof(className)}\" не может быть неопределенным или пустым.", nameof(className));
        if (string.IsNullOrWhiteSpace(namePropertyOrField))
            throw new ArgumentException($"\"{nameof(namePropertyOrField)}\" не может быть неопределенным или пустым.", nameof(namePropertyOrField));

        var type = asm.ExportedTypes.Single(x => x.Name == className || x.FullName == className);
        object? res = null;
        var prop = type.GetProperty(namePropertyOrField);

        res = prop != null ? prop.GetValue(null) : type.GetField(namePropertyOrField)!.GetValue(null);

        return res;
    }

    /// <summary>
    /// Получение значений статических свойств коллекцией
    /// </summary>
    /// <param name="type">Просматреваемый тип</param>
    /// <param name="typeProperty">Тип свойста</param>
    /// <returns></returns>
    public static ReadOnlyCollection<object?> GetStaticPropertys(this Type type, Type typeProperty)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (typeProperty is null)
            throw new ArgumentNullException(nameof(typeProperty));

        var res = new List<object?>();

        var flds = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        foreach (var fld in flds.Where(x => x.PropertyType == typeProperty))
            res.Add(fld.GetValue(null));

        return new ReadOnlyCollection<object?>(res);

    }

    /// <summary>
    /// Получение значений статических свойств коллекцией
    /// </summary>
    /// <typeparam name="T">Тип свойства для коллекции</typeparam>
    /// <param name="type">Просматреваемый тип</param>
    /// <returns></returns>
    public static ReadOnlyCollection<T> GetStaticPropertys<T>(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return new ReadOnlyCollection<T>(type.GetStaticPropertys(typeof(T)).Cast<T>().ToArray());
    }

    /// <summary>
    /// Получение значений статических полей коллекцией
    /// </summary>
    /// <param name="type">Просматреваемый тип</param>
    /// <param name="typeFields">Тип полей для коллекции</param>
    /// <returns></returns>
    public static ReadOnlyCollection<object?> GetStaticFields(this Type type, Type typeFields)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (typeFields is null)
            throw new ArgumentNullException(nameof(typeFields));

        var res = new List<object?>();

        var flds = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        foreach (var fld in flds.Where(x => x.FieldType == typeFields))
            res.Add(fld.GetValue(null));

        return new ReadOnlyCollection<object?>(res);

    }

    /// <summary>
    /// Получение значений статических полей коллекцией
    /// </summary>
    /// <typeparam name="T">Тип полей для коллекции</typeparam>
    /// <param name="type">Просматреваемый тип</param>
    /// <returns></returns>
    public static ReadOnlyCollection<T> GetStaticFields<T>(this Type type) =>
        new(type.GetStaticFields(typeof(T)).Cast<T>().ToArray());

    /// <summary>
    /// Установка значения свойства
    /// </summary>
    /// <param name="obj">экземпляр объекта</param>
    /// <param name="propertyName">Имя свойства</param>
    /// <param name="value">значение</param>
    public static void SetPropertyValue(this object obj, string propertyName, object value)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        obj.GetType().GetProperty(propertyName)!.SetValue(obj, value);
    }
    /// <summary>
    /// Создание экземпляра класса
    /// </summary>
    /// <param name="asm">Сборка с типом</param>
    /// <param name="className">Имя класса</param>
    /// <param name="args">Аргументы конструктора</param>
    /// <returns></returns>
    public static object? CreateInstance(this Assembly asm, string className, params object[] args)
    {
        if (asm is null)
            throw new ArgumentNullException(nameof(asm));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException($"\"{nameof(className)}\" не может быть неопределенным или пустым.", nameof(className));

        var clType = asm.ExportedTypes
            .Single(x => x.Name == className || x.FullName == className);
        return Activator.CreateInstance(clType, args);
    }
    /// <summary>
    /// Получить экземпляр значения перечесления (enum)
    /// </summary>
    /// <param name="asm">сборка, содержащая тип</param>
    /// <param name="enumTypeName">Имя класа перечесления</param>
    /// <param name="valueName">Строковое значение перечесления</param>
    /// <returns></returns>
    public static object GetEnumValue(this Assembly asm, string enumTypeName, string valueName)
    {
        if (asm is null)
            throw new ArgumentNullException(nameof(asm));
        if (string.IsNullOrWhiteSpace(enumTypeName))
            throw new ArgumentException($"\"{nameof(enumTypeName)}\" не может быть неопределенным или пустым.", nameof(enumTypeName));
        if (string.IsNullOrWhiteSpace(valueName))
            throw new ArgumentException($"\"{nameof(valueName)}\" не может быть неопределенным или пустым.", nameof(valueName));

        var rtType = asm.ExportedTypes
            .Single(x => x.Name == enumTypeName || x.FullName == enumTypeName);

        return Enum.Parse(rtType, valueName);
    }
    /// <summary>
    /// Вызов метода у объекта
    /// </summary>
    /// <param name="obj">экземпляр объекта</param>
    /// <param name="methodName">Имя метода</param>
    /// <param name="arg">Аргументы метода. По ним ищется метод</param>
    /// <returns>Результат выполения</returns>
    public static object? InvokeMethod(this object obj, string methodName, params object[] arg)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException($"\"{nameof(methodName)}\" не может быть пустым или содержать только пробел.", nameof(methodName));

        var minfo = obj.GetType().GetMethod(methodName, arg.Select(x => x.GetType()).ToArray());
        return minfo!.Invoke(obj, arg);
    }
    /// <summary>
    /// Вызов статического метода
    /// </summary>
    /// <param name="asm">Сборка, содержащая тип</param>
    /// <param name="className">Имя класса</param>
    /// <param name="methodName">Имя метода</param>
    /// <param name="args">Аргументы метода. По ним ищется метод</param>
    /// <returns>Результат выполения</returns>
    public static object? InvokeStaticMethod(this Assembly asm, string className, string methodName, params object[] args)
    {
        if (asm is null)
            throw new ArgumentNullException(nameof(asm));

        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException($"\"{nameof(className)}\" не может быть пустым или содержать только пробел.", nameof(className));

        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException($"\"{nameof(methodName)}\" не может быть пустым или содержать только пробел.", nameof(methodName));

        var rtType = asm.ExportedTypes
            .Single(x => x.Name == className);

        var mi = rtType.GetMethod(methodName, args.Select(x => x.GetType()).ToArray());
        return mi!.Invoke(null, args);
    }

    /// <summary>
    /// Получение генерик-типа из генерик-коллекции или массива. 
    /// </summary>
    /// <param name="type"></param>
    /// <returns>Первый генерик-тип в перечеслении</returns>
    public static Type? GetEnumeratedType(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return type.GetElementType() ??
        (typeof(IEnumerable).IsAssignableFrom(type) ? type.GenericTypeArguments.FirstOrDefault() : null);
    }

    /// <summary>
    /// Является ли тип <see cref="List{T}"/> или <see cref="HashSet{T}"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsIList(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(HashSet<>));
    }

    /// <summary>
    /// Распаковать тип из <see cref="Nullable{T}"/>, либо получить тип из коллекции.
    /// Если тип НЕ приводится к <see cref="List{T}"/> или <see cref="HashSet{T}"/>, то вернется исходный тип
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Type UnWrapType(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        var res = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsIList() &&
            res.GetGenericArguments().Length == 1)
            res = res.GetGenericArguments().Single();

        return res;
    }

    /// <summary>
    /// Является ли тип "простым" - типы значений (<see cref="Type.IsValueType"/>)/строки (<see cref="string"/>)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSimpleType(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        var res = Nullable.GetUnderlyingType(type) ?? type;

        return res.IsValueType || typeof(string) == res;
    }
}
