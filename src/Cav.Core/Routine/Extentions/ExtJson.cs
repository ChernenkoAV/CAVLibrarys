﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Cav.Json;
using Newtonsoft.Json;

namespace Cav
{
    /// <summary>
    /// Сериализация-десериализация JSON средствами NewtonSoft
    /// </summary>
    public static class ExtJson
    {
        private static GenericJsonSerializerSetting getJsetting(
            StreamingContextStates state,
            object additional = null) =>
                state != 0
                    ? new GenericJsonSerializerSetting(state, additional)
                    : GenericJsonSerializerSetting.Instance;

        /// <summary>
        /// Json сериализация (c наполением контекста). null не выводятся. Пустые <see cref="IEnumerable"/> тождественны null.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="state">Заданное состояние контекста</param>
        /// <param name="additional">Любые дополнительные сведения</param>
        /// <returns></returns>
        public static String JsonSerialize(this Object obj, StreamingContextStates state, object additional = null) =>
            obj == null
                ? null
                : obj is IEnumerable ien && !ien.GetEnumerator().MoveNext()
                    ? null
                    : JsonConvert.SerializeObject(obj, getJsetting(state, additional));

        /// <summary>
        /// Json сериализация. null не выводятся. Пустые <see cref="IEnumerable"/> тождественны null.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static String JsonSerialize(this Object obj) => obj.JsonSerialize(0, null);

        /// <summary>
        /// Json десериализация (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <param name="state">Заданное состояние контекста</param>
        /// <param name="additional">Любые дополнительные сведения</param>
        public static T JsonDeserealize<T>(this String s,
            StreamingContextStates state,
            object additional = null) => (T)s.JsonDeserealize(typeof(T), state, additional);

        /// <summary>
        /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        public static T JsonDeserealize<T>(this String s) => (T)s.JsonDeserealize(typeof(T), 0, null);

        /// <summary>
        /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        /// <param name="s">Исходная строка</param>
        /// <param name="type">Целевой тип</param>
        public static object JsonDeserealize(this String s, Type type) => s.JsonDeserealize(type, 0, null);

        /// <summary>
        /// Json десериализация (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        /// <param name="s">Исходная строка</param>
        /// <param name="type">Целевой тип</param>
        /// <param name="state">Заданное состояние контекста</param>
        /// <param name="additional">Любые дополнительные сведения</param>
        public static object JsonDeserealize(this String s,
        Type type,
        StreamingContextStates state,
        object additional = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (s.IsNullOrWhiteSpace())
            {
                if (type.IsArray)
                    return Array.CreateInstance(type.GetElementType(), 0);

                // Для всяких HashList и тд
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                    type.GetGenericArguments().Length == 1)
                    return Array.CreateInstance(type.GetGenericArguments().Single(), 0);

                //Для Dictionary<> и тд
                if (typeof(IEnumerable).IsAssignableFrom(type) &&
                    type.GetConstructor(Array.Empty<Type>()) != null)
                    return Activator.CreateInstance(type);

                return type.GetDefault();
            }

            return JsonConvert.DeserializeObject(s, type, getJsetting(state, additional));
        }

        /// <summary>
        /// Json десериализация из файла. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// Если файла нет - десиреализует, как пустую строку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="type">целевой тип десериализации</param>
        public static object JsonDeserealizeFromFile(this String filePath, Type type) => filePath.JsonDeserealizeFromFile(type, 0, null);

        /// <summary>
        /// Json десериализация из файла (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// Если файла нет - десиреализует, как пустую строку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="type">целевой тип десериализации</param>
        /// <param name="state">Заданное состояние контекста</param>
        /// <param name="additional">Любые дополнительные сведения</param>
        public static object JsonDeserealizeFromFile(this String filePath,
            Type type,
            StreamingContextStates state,
            object additional = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException($"\"{nameof(filePath)}\" не может быть пустым или содержать только пробел.", nameof(filePath));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            string s = null;

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

            return JsonConvert.DeserializeObject(s, type, getJsetting(state, additional));
        }

        /// <summary>
        /// Json десериализация из файла. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// Если файла нет - десиреализует, как пустую строку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        public static T JsonDeserealizeFromFile<T>(this String filePath) => filePath.JsonDeserealizeFromFile<T>(0, null);

        /// <summary>
        /// Json десериализация из файла (c наполением контекста). возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// Если файла нет - десиреализует, как пустую строку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="state">Заданное состояние контекста</param>
        /// <param name="additional">Любые дополнительные сведения</param>
        public static T JsonDeserealizeFromFile<T>(this String filePath,
            StreamingContextStates state,
            object additional = null) => (T)filePath.JsonDeserealizeFromFile(typeof(T), state, additional);

        /// <summary>
        /// Копирование объекта через сериализацию-десериализацию
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
