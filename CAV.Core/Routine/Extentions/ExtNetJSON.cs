using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Cav
{
    /// <summary>
    /// Сериализация-десериализация JSON средствами .Net
    /// </summary>
    public static class ExtNetJSON
    {
        /// <summary>
        /// Сериализация объекта в строку JSON
        /// </summary>
        /// <param name="obj">Объект десиреализации</param>
        /// <returns>Результирующая строка JSON</returns>
        public static string JSONSerialize(this Object obj)
        {
            if (obj == null)
                return null;

            using (var ms = new MemoryStream())
            {
                var dcs = new DataContractJsonSerializer(obj.GetType());
                dcs.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Десиреализация из строки в JSON формате
        /// </summary>
        /// <typeparam name="T">Тип десериализации</typeparam>
        /// <param name="str">Исходная строка</param>
        /// <returns>Результат десериализации</returns>
        public static T JSONDeserialize<T>(this String str)
        {
            return (T)str.JSONDeserialize(typeof(T));
        }

        /// <summary>
        /// Десиреализация из строки в JSON формате
        /// </summary>        
        /// <param name="str">Исходная строка</param>
        /// <param name="targetType">Тип десериализации</param>
        /// <returns>Результат десериализации</returns>
        public static object JSONDeserialize(this String str, Type targetType)
        {
            if (str.IsNullOrWhiteSpace())
                return targetType.GetDefault();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
                return (new DataContractJsonSerializer(targetType)).ReadObject(ms);
        }
    }
}
