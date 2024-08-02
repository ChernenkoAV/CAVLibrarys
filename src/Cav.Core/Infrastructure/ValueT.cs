using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

#pragma warning disable CA1062 // Проверить аргументы или открытые методы

namespace Cav.Infrastructure;

/// <summary>
/// Базовый класс для реализации "типизирванных" типов значений
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>, IComparable
{
    protected static bool equalOperator(ValueObject? left, ValueObject? right) =>
        !(left is null ^ right is null) && (left is null || left.Equals(right));

    protected static bool notEqualOperator(ValueObject? left, ValueObject? right) =>
        !equalOperator(left, right);

    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);

    public bool Equals(ValueObject? other) => Equals((object?)other);

    public static bool operator ==(ValueObject? one, ValueObject? two) =>
        equalOperator(one, two);

    public static bool operator !=(ValueObject? one, ValueObject? two) =>
        notEqualOperator(one, two);

    public virtual int CompareTo(object? obj) => throw new NotImplementedException();

    public static bool operator <(ValueObject left, ValueObject right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(ValueObject left, ValueObject right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(ValueObject left, ValueObject right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(ValueObject left, ValueObject right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;
}

/// <summary>
/// Базовый класс для реализации "типизирванных" типов значений с указанием типа 
/// </summary>
public abstract class ValueObject<T> : ValueObject, IComparable<ValueObject<T>>
{
    protected ValueObject() { }
    protected ValueObject(T? value) => Value = value;

    public T? Value { get; set; }
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string? ToString() => Value?.ToString();
    public int CompareTo(ValueObject<T>? other) =>
        other is null || other.Value is null || Value is not IComparable<T> tvObj
            ? -1
            : tvObj.CompareTo(other.Value);

    public override int CompareTo(object? obj) =>
        obj is not ValueObject<T> other || other.Value is null || Value is not IComparable<T> tvObj
            ? -1
            : tvObj.CompareTo(other.Value);

    public static implicit operator T?(ValueObject<T>? valObj) =>
        valObj is null ? default : valObj.Value;
}

/// <summary>
/// Базовый класс для реализации "типизирванных" значений типа <see cref="int"/>
/// </summary>
public abstract class ValueObjectInt : ValueObject<int>
{
    protected ValueObjectInt() : base() { }
    protected ValueObjectInt(int val) : base(val) { }
}

/// <summary>
/// Базовый класс для реализации "типизирванных" значений типа <see cref="string"/>
/// </summary>
public abstract class ValueObjectString : ValueObject<string>
{
    protected ValueObjectString() : base() { }
    protected ValueObjectString(string? val) : base(val) { }
}

/// <summary>
/// Базовый класс для реализации "типизирванных" значений типа <see cref="Guid"/>
/// </summary>
public abstract class ValueObjectGuid : ValueObject<Guid>
{
    protected ValueObjectGuid() : base() { }
    protected ValueObjectGuid(Guid val) : base(val) { }
}

/// <summary>
/// Конвертор для json сериализации-десериализации (Newtonsoft.Json). Указывается для конечного типа значения.
/// </summary>
/// <typeparam name="T">Конечный тип "типизированного" типа значения</typeparam>
/// <typeparam name="TV">Базовый тип, на основе которого построен "тип значения"</typeparam>
public class JsonValueValueObjectConverter<T, TV> : JsonConverter
    where T : ValueObject<TV>
{
    public override bool CanConvert(Type objectType) => typeof(T) == objectType;
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var val = (TV?)reader.Value;

        if (val == null)
            return null;

        var res = (T)Activator.CreateInstance(objectType, true)!;
        res.Value = val;
        return res;
    }
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
            return;

        writer.WriteValue(((T)value).Value);
    }
}

/// <summary>
/// Кастомный конвертер типов для "типизированных значений строк". Необходим для корректной атоматической работы фабрик,
/// которые могут принимать простые типы на вход и маппить их в параметры (ASP NET Core)
/// </summary>
/// <typeparam name="T">Конечный тип "типизированного" типа значения</typeparam>
public class ValueObjectStringTypeConverter<T> : TypeConverter where T : ValueObjectString
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        Activator.CreateInstance(typeof(T), value as string);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        destinationType == typeof(string)
        ? value?.ToString()
        : throw new NotSupportedException($"конвертация должна производиться только из объекта типа {nameof(String)}");
}

