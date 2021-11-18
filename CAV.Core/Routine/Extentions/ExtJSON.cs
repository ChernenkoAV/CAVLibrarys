using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
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
        /// Сериализация объекта в строку JSON (NET)
        /// </summary>
        /// <param name="obj">Объект десиреализации</param>
        /// <returns>Результирующая строка JSON</returns>
        [Obsolete("Будет удалено. Используйте с префиксом Json")]
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
        /// Десиреализация из строки в JSON формате (NET)
        /// </summary>
        /// <typeparam name="T">Тип десериализации</typeparam>
        /// <param name="str">Исходная строка</param>
        /// <returns>Результат десериализации</returns>
        [Obsolete("Будет удалено. Используйте с префиксом Json")]
        public static T JSONDeserialize<T>(this String str)
        {
            return (T)str.JSONDeserialize(typeof(T));
        }

        /// <summary>
        /// Десиреализация из строки в JSON формате (NET)
        /// </summary>        
        /// <param name="str">Исходная строка</param>
        /// <param name="targetType">Тип десериализации</param>
        /// <returns>Результат десериализации</returns>
        [Obsolete("Будет удалено. Используйте с префиксом Json")]
        public static object JSONDeserialize(this String str, Type targetType)
        {
            if (str.IsNullOrWhiteSpace())
                return targetType.GetDefault();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
                return new DataContractJsonSerializer(targetType).ReadObject(ms);
        }

        /// <summary>
        /// Json сериализация. null не выводятся. Пустые <see cref="IEnumerable"/> тождественны null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static String JsonSerialize(this Object obj) =>
            obj == null
                ? null
                : obj is IEnumerable ien && !ien.GetEnumerator().MoveNext()
                    ? null
                    : JsonConvert.SerializeObject(obj, GenericJsonSerializerSetting.Instance);

        /// <summary>
        /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        public static T JsonDeserealize<T>(this String s)
        {
            return (T)s.JsonDeserealize(typeof(T));
        }

        /// <summary>
        /// Json десериализация. возврат: Если тип реализует <see cref="IList"/> - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// </summary>
        public static object JsonDeserealize(this String s, Type type)
        {
            if (s.IsNullOrWhiteSpace())
            {
                if (type.IsArray)
                    return Array.CreateInstance(type.GetElementType(), 0);

                if (typeof(IEnumerable).IsAssignableFrom(type) &&
                    type.GetConstructor(Array.Empty<Type>()) != null)
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
            String s = null;

            if (File.Exists(filePath))
                s = File.ReadAllText(filePath);

            return s.JsonDeserealize(type);
        }

        /// <summary>
        /// Json десериализация из файла. возврат: Если тип реализует <see cref="IEnumerable"/> c простым конструктором - пустую коллекцию(что б в коде не проверять на null и сразу юзать foreach)
        /// Если файла нет - десиреализует, как пустую строку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        public static T JsonDeserealizeFromFile<T>(this String filePath)
        {
            return (T)filePath.JsonDeserealizeFromFile(typeof(T));
        }

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
