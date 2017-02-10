using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Cav.DataAcces;

namespace Cav
{
    /// <summary>
    /// Вспомогательные методы для адаптеров слоя доступа к данным
    /// </summary>
    public static class HeplerDataAcces
    {
        static HeplerDataAcces()
        {
            TypeMaps[typeof(byte)] = DbType.Byte;
            TypeMaps[typeof(sbyte)] = DbType.SByte;
            TypeMaps[typeof(short)] = DbType.Int16;
            TypeMaps[typeof(ushort)] = DbType.UInt16;
            TypeMaps[typeof(int)] = DbType.Int32;
            TypeMaps[typeof(uint)] = DbType.UInt32;
            TypeMaps[typeof(long)] = DbType.Int64;
            TypeMaps[typeof(ulong)] = DbType.UInt64;
            TypeMaps[typeof(float)] = DbType.Single;
            TypeMaps[typeof(double)] = DbType.Double;
            TypeMaps[typeof(decimal)] = DbType.Decimal;
            TypeMaps[typeof(bool)] = DbType.Boolean;
            TypeMaps[typeof(string)] = DbType.String;
            TypeMaps[typeof(char)] = DbType.StringFixedLength;
            TypeMaps[typeof(Guid)] = DbType.Guid;
            TypeMaps[typeof(DateTime)] = DbType.DateTime;
            TypeMaps[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
            TypeMaps[typeof(byte[])] = DbType.Binary;
        }

        internal static Dictionary<Type, DbType> TypeMaps = new Dictionary<Type, DbType>();
        internal static DbType TypeMapDbType(Type sType)
        {
            Type origanalType = sType;
            Type nullableType = Nullable.GetUnderlyingType(sType);
            if (nullableType != null)
                sType = nullableType;

            if (sType.IsEnum)
                sType = sType.GetEnumUnderlyingType();

            DbType res;

            if (!TypeMaps.TryGetValue(sType, out res))
                throw new ArgumentException($"Не удалось сопоставить тип {origanalType.FullName} с типом для параметра команды");

            return res;
        }
        internal static T ForParceParams<T>(this T obj, Expression prop, Object val) where T : class
        {
            return obj;
        }

        /// <summary>
        /// Установить значение свойства параметра
        /// </summary>
        /// <typeparam name="T">Класс параметров адаптера</typeparam>
        /// <param name="instParam">"Ссылка" на экземпляр</param>
        /// <param name="setProp">Свойство, которое необходимо настроить</param>
        /// <param name="value">Значения свойства</param>
        /// <returns></returns>
        public static T SetParam<T>(this T instParam, Expression<Func<T, Object>> setProp, Object value) where T : IAdapterParametrs
        {
            return instParam;
        }

        internal static object FromField(Type returnType, DataRow dbRow, String fieldName, Func<object, object> conv)
        {
            if (!dbRow.Table.Columns.Contains(fieldName))
                return returnType.GetDefault();

            Object val = dbRow[fieldName];

            if (val is DBNull)
                val = returnType.GetDefault();

            if (conv != null && val != null)
                val = conv(val);

            return val;
        }
    }
}
