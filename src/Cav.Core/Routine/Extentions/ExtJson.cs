using System.Collections;
using System.Runtime.Serialization;
using Cav.Json;
using Newtonsoft.Json;

namespace Cav;

#pragma warning disable SYSLIB0050 // Тип или член устарел

/// <summary>
/// Сериализация-десериализация JSON средствами NewtonSoft
/// </summary>
public static class ExtJson
{
    private static GenericJsonSerializerSetting getJsetting(
        StreamingContextStates state,
        object? additional = null) =>
            state != 0
                ? new GenericJsonSerializerSetting(state, additional)
                : GenericJsonSerializerSetting.Instance;

    /// <summary>
    /// Json сериализация (c наполением контекста). null не выводятся. Пустые <see cref="IEnumerable"/> тождественны null.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="state">Заданное состояние контекста</param>
    /// <param name="additional">Любые дополнительные сведения</param>
    /// <param name="pretty">форматировать вывод</param>
    /// <returns></returns>
    public static string? JsonSerialize(this object? obj, StreamingContextStates state, object? additional = null, bool pretty = false) =>
        obj is null
            ? null
            : obj is IEnumerable ien && !ien.GetEnumerator().MoveNext()
                ? null
                : JsonConvert.SerializeObject(
                    obj,
                    pretty ? Formatting.Indented : Formatting.None,
                    getJsetting(state, additional));

    /// <summary>
    /// Json сериализация. null не выводятся. Пустые <see cref="IEnumerable"/> тождественны null.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string? JsonSerialize(this object? obj) => obj.JsonSerialize(0, null);
    /// <summary>
    /// Json сериализация. null не выводятся. Пустые <see cref="IEnumerable"/> тождественны null.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="pretty">форматировать вывод</param>
    /// <returns></returns>
    public static string? JsonSerialize(this object? obj, bool pretty) => obj.JsonSerialize(0, null, pretty);

    /// <summary>
    /// Json десериализация (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="s"></param>
    /// <param name="state">Заданное состояние контекста</param>
    /// <param name="additional">Любые дополнительные сведения</param>
    public static T? JsonDeserealize<T>(this string s,
        StreamingContextStates state,
        object? additional = null) => (T?)s.JsonDeserealize(typeof(T), state, additional);

    /// <summary>
    /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="s"></param>
    public static T? JsonDeserealize<T>(this string? s) => (T?)s.JsonDeserealize(typeof(T), 0, null);

    /// <summary>
    /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// </summary>
    /// <param name="s">Исходная строка</param>
    /// <param name="type">Целевой тип</param>
    public static object? JsonDeserealize(this string? s, Type type) => s.JsonDeserealize(type, 0, null);

    /// <summary>
    /// Json десериализация (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// </summary>
    /// <param name="s">Исходная строка</param>
    /// <param name="type">Целевой тип</param>
    /// <param name="state">Заданное состояние контекста</param>
    /// <param name="additional">Любые дополнительные сведения</param>
    public static object? JsonDeserealize(
        this string? s,
        Type type,
        StreamingContextStates state,
        object? additional = null) =>
            type is null
                ? throw new ArgumentNullException(nameof(type))
                : s.IsNullOrWhiteSpace()
                    ? type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) && type != typeof(string))
                        ? Array.CreateInstance(type.GetElementType() ?? type.GetGenericArguments().Single(), 0)
                        : // Для всяких HashList и тд
                            (
                                type.GetInterfaces().Any(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)) &&
                                type.GetGenericArguments().Length == 1 &&
                                type != typeof(string)
                            )
                            ||
                            //Для Dictionary<> и тд
                            (typeof(IEnumerable).IsAssignableFrom(type) && type.GetConstructor([]) != null)
                                ? Activator.CreateInstance(type)
                                : type.GetDefault()
                    : JsonConvert.DeserializeObject(s!, type, getJsetting(state, additional));

    /// <summary>
    /// Json десериализация из файла. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// Если файла нет - десиреализует, как пустую строку.
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <param name="type">целевой тип десериализации</param>
    public static object? JsonDeserealizeFromFile(this string filePath, Type type) => filePath.JsonDeserealizeFromFile(type, 0, null);

    /// <summary>
    /// Json десериализация из файла (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// Если файла нет - десиреализует, как пустую строку.
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <param name="type">целевой тип десериализации</param>
    /// <param name="state">Заданное состояние контекста</param>
    /// <param name="additional">Любые дополнительные сведения</param>
    public static object? JsonDeserealizeFromFile(this string filePath,
        Type type,
        StreamingContextStates state,
        object? additional = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException($"\"{nameof(filePath)}\" не может быть пустым или содержать только пробел.", nameof(filePath));

        if (type is null)
            throw new ArgumentNullException(nameof(type));

        string? s = null;

        if (File.Exists(filePath))
            s = File.ReadAllText(filePath);

        return s.JsonDeserealize(type, state, additional);
    }

    /// <summary>
    /// Json десериализация из файла. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// Если файла нет - десиреализует, как пустую строку.
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    public static T? JsonDeserealizeFromFile<T>(this string filePath) => filePath.JsonDeserealizeFromFile<T>(0, null);

    /// <summary>
    /// Json десериализация из файла (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
    /// Если файла нет - десиреализует, как пустую строку.
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <param name="state">Заданное состояние контекста</param>
    /// <param name="additional">Любые дополнительные сведения</param>
    public static T? JsonDeserealizeFromFile<T>(this string filePath,
        StreamingContextStates state,
        object? additional = null) => (T?)filePath.JsonDeserealizeFromFile(typeof(T), state, additional);

    /// <summary>
    /// Копирование объекта через json сериализацию-десериализацию
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static TSource? Copy<TSource>(this TSource? source)
        where TSource : class, new() =>
        source.CopyTo<TSource>();

    /// <summary>
    /// Копирование объекта через json сериализацию-десериализацию в целевой тип
    /// </summary>
    /// <typeparam name="TTarget">Целевой тип</typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static TTarget? CopyTo<TTarget>(this object? source)
        where TTarget : class, new()
        => source?.JsonSerialize().JsonDeserealize<TTarget>();
}
