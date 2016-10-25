using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
        internal static DbType TypeMapDbType(Type sType)
        {
            Type nullableType = Nullable.GetUnderlyingType(sType);
            if (nullableType != null)
                sType = nullableType;
            if (sType.IsEnum)
                return typeMaps[typeof(int)];
            return typeMaps[sType];

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
        public static T SetParam<T>(this T instParam, Expression<Func<T, Object>> setProp, Object value) where T : AdapterParametrs
        {
            return instParam;
        }

        private static object FromField(Type returnType, DbDataReader selreader, String fieldName)
        {
            bool fieldExist = false;
            for (int i = 0; i < selreader.FieldCount; i++)
                if (selreader.GetName(i) == fieldName)
                {
                    fieldExist = true;
                    break;
                }

            if (!fieldExist)
                return GetDefault(returnType);
            Object val = selreader[fieldName];

            if (val is DBNull)
                return GetDefault(returnType);

            Type nullableType = Nullable.GetUnderlyingType(returnType);
            if ((returnType.IsEnum || (nullableType != null && nullableType.IsEnum)) && val.GetType() == typeof(short))
                return (int)(short)val;

            return val;
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}
