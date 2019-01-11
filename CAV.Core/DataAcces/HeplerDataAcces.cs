using Cav.DataAcces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace Cav
{
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

        private static Dictionary<Type, DbType> typeMaps = new Dictionary<Type, DbType>();

        /// <summary>
        /// Возможность сопоставить тип <see cref="Type"/>  с типом <see cref="DbType"/> 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsCanMappedDbType(Type type)
        {
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
                type = nullable;

            if (type.IsEnum)
                return true;
            DbType dt;
            return typeMaps.TryGetValue(type, out dt);
        }

        /// <summary>
        /// Получение <see cref="DbType"/> по <see cref="Type"/>
        /// </summary>
        /// <param name="sType"></param>
        /// <returns></returns>
        public static DbType TypeMapDbType(Type sType)
        {
            Type origanalType = sType;
            Type nullableType = Nullable.GetUnderlyingType(sType);
            if (nullableType != null)
                sType = nullableType;

            if (sType.IsEnum)
                sType = sType.GetEnumUnderlyingType();

            DbType res;

            if (!typeMaps.TryGetValue(sType, out res))
                throw new ArgumentException($"Не удалось сопоставить тип {origanalType.FullName} с типом DbType");

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

        internal static object FromField(Type returnType, DataRow dbRow, String fieldName, Delegate conv)
        {
            if (!dbRow.Table.Columns.Contains(fieldName))
                return returnType.GetDefault();

            Object val = dbRow[fieldName];

            if (val is DBNull)
                val = returnType.GetDefault();

            var nullable = Nullable.GetUnderlyingType(returnType);

            if (val != null && (returnType.IsEnum || (nullable != null && nullable.IsEnum)))
                val = Enum.ToObject(nullable ?? returnType, val);

            if (conv != null && (val != null || returnType.IsArray))
                val = conv.DynamicInvoke(val);

            if (val == null && !HeplerDataAcces.IsCanMappedDbType(nullable ?? returnType))
                val = Activator.CreateInstance(returnType);

            return val;
        }
    }
}
