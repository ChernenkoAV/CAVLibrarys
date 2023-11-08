using System.Data;
using System.Linq.Expressions;
using Cav.DataAcces;

namespace Cav;

/// <summary>
/// Вспомогательные методы для адаптеров слоя доступа к данным
/// </summary>
public static class HeplerDataAcces
{
    static HeplerDataAcces()
    {
        typeMaps[typeof(byte)] = DbType.Byte;
        typeMaps[typeof(sbyte)] = DbType.SByte;
        typeMaps[typeof(short)] = DbType.Int16;
        typeMaps[typeof(ushort)] = DbType.UInt16;
        typeMaps[typeof(int)] = DbType.Int32;
        typeMaps[typeof(uint)] = DbType.UInt32;
        typeMaps[typeof(long)] = DbType.Int64;
        typeMaps[typeof(ulong)] = DbType.UInt64;
        typeMaps[typeof(float)] = DbType.Single;
        typeMaps[typeof(double)] = DbType.Double;
        typeMaps[typeof(decimal)] = DbType.Decimal;
        typeMaps[typeof(bool)] = DbType.Boolean;
        typeMaps[typeof(string)] = DbType.String;
        typeMaps[typeof(char)] = DbType.StringFixedLength;
        typeMaps[typeof(Guid)] = DbType.Guid;
        typeMaps[typeof(DateTime)] = DbType.DateTime;
        typeMaps[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
        typeMaps[typeof(byte[])] = DbType.Binary;
    }

    private static Dictionary<Type, DbType> typeMaps = new();

    /// <summary>
    /// Возможность сопоставить тип <see cref="Type"/>  с типом <see cref="DbType"/> 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsCanMappedDbType(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsEnum || typeMaps.TryGetValue(type, out var dt);
    }

    /// <summary>
    /// Получение <see cref="DbType"/> по <see cref="Type"/>
    /// </summary>
    /// <param name="sType"></param>
    /// <returns></returns>
    public static DbType TypeMapDbType(Type sType)
    {
        var originalType = sType;
        sType = Nullable.GetUnderlyingType(sType) ?? sType;

        if (sType.IsEnum)
            sType = sType.GetEnumUnderlyingType();

        return !typeMaps.TryGetValue(sType, out var res)
            ? throw new ArgumentException($"Не удалось сопоставить тип {originalType.FullName} с типом DbType")
            : res;
    }

    internal static object? FromField(Type returnType, DataRow dbRow, string fieldName, Delegate conv)
    {
        if (!dbRow.Table.Columns.Contains(fieldName))
            return returnType.GetDefault();

        var val = dbRow[fieldName];

        if (val is DBNull)
            val = returnType.GetDefault();

        returnType = Nullable.GetUnderlyingType(returnType) ?? returnType;

        if (conv == null && val != null && returnType.IsEnum)
            val = Enum.Parse(returnType, val.ToString()!, true);

        if (conv != null && (val != null || returnType.IsArray))
            val = conv.DynamicInvoke(val);

        if (val == null && !IsCanMappedDbType(returnType))
            val = Activator.CreateInstance(returnType, true);

        return val;
    }

#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    internal static T ForParceParams<T>(this T obj, Expression prop, object val) where T : class => obj;

    /// <summary>
    /// Установить значение свойства параметра
    /// </summary>
    /// <typeparam name="T">Класс параметров адаптера</typeparam>
    /// <param name="instParam">"Ссылка" на экземпляр</param>
    /// <param name="setProp">Свойство, которое необходимо настроить</param>
    /// <param name="value">Значения свойства</param>
    /// <returns></returns>
    public static T SetParam<T>(this T instParam, Expression<Func<T, object?>> setProp, object? value) where T : IAdapterParametrs => instParam;
}
