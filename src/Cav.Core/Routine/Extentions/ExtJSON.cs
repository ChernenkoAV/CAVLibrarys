using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cav.Json;
using Newtonsoft.Json;

namespace Cav
{
    /// <summary>
    /// Сериализация-десериализация JSON средствами .Net | NewtonSoft
    /// </summary>
    public static class ExtJson
    {
        /// <summary>
        /// Json сериализация. null не выводятся. Пустые <see cref="IList"/> тождественны null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static String JsonSerialize(this Object obj)
        {
            if (obj == null)
                return null;

            if (obj is IEnumerable iEn && !iEn.GetEnumerator().MoveNext())
                return null;

            return JsonConvert.SerializeObject(obj, GenericJsonSerializerSetting.Instance);
        }

        /// <summary>
        /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        public static T JsonDeserealize<T>(this String s) => (T)s.JsonDeserealize(typeof(T));

        /// <summary>
        /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        public static object JsonDeserealize(this String s, Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (s.IsNullOrWhiteSpace())
            {
                if (type.IsArray)
                    return Array.CreateInstance(type.GetElementType(), 0);

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return Array.CreateInstance(type.GetGenericArguments().Single(), 0);

                if (typeof(IList).IsAssignableFrom(type))
                    return Activator.CreateInstance(type);

                return type.GetDefault();
            }

            return JsonConvert.DeserializeObject(s, type, GenericJsonSerializerSetting.Instance);
        }

        /// <summary>
        /// Json десериализация из файла. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// Если файла нет - десиреализует, как пустую строку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="type">целевой тип десериализации</param>
        public static object JsonDeserealizeFromFile(this String filePath, Type type)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException($"\"{nameof(filePath)}\" не может быть пустым или содержать только пробел.", nameof(filePath));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            String s = null;

            if (File.Exists(filePath))
                s = File.ReadAllText(filePath);

            if (s.IsNullOrWhiteSpace())
            {
                if (type.IsArray)
                    return Array.CreateInstance(type.GetElementType(), 0);

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return Array.CreateInstance(type.GetGenericArguments().Single(), 0);

                if (typeof(IList).IsAssignableFrom(type))
                    return Activator.CreateInstance(type);

                return type.GetDefault();
            }

            return JsonConvert.DeserializeObject(s, type, GenericJsonSerializerSetting.Instance);
        }

        /// <summary>
        /// Json десериализация из файла. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// Если файла нет - десиреализует, как пустую строку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        public static T JsonDeserealizeFromFile<T>(this String filePath) => (T)filePath.JsonDeserealizeFromFile(typeof(T));

        /// <summary>
        /// Копирование объекта через сериалиpацию-десериализацию
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Copy<T>(this T source) where T : class, new()
        {
            if (source == null)
                return null;

            return source.JsonSerialize().JsonDeserealize<T>();
        }
    }
}
