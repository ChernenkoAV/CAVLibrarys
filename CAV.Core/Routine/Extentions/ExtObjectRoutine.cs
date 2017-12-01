using System;
using Cav.ReflectHelpers;

namespace Cav
{
    /// <summary>
    /// Расширения для работы с объектами
    /// </summary>
    public static class ExtObjectRoutine
    {
        /// <summary>
        /// Получение значения по умолчанию для типа
        /// </summary>
        /// <param name="type">Тип, для которого необходимо получить значение</param>
        /// <returns>Значение по уполчанию</returns>
        public static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        /// <summary>
        /// Выражение "null если" для типов структур
        /// </summary>
        /// <typeparam name="T">Тип структуры</typeparam>
        /// <param name="exp">Проверяемое выражение</param>
        /// <param name="operand">Операнд сравнения</param>
        /// <returns></returns>
        public static Nullable<T> NullIf<T>(this T exp, T operand) where T : struct
        {
            return exp.Equals(operand) ? (T?)null : exp;
        }

        /// <summary>
        /// Получение свойства у объекта. Обработка вложеных объектов
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pathProperty">Путь к свойству вида "PropertyA.PropertyB.PropertyC"</param>
        /// <param name="throwIfObjectIsNull">Вернуть исключение, если вложеный объект = null, либо результат - null</param>
        /// <returns></returns>
        public static object GetPropertyValueNestedObject<T>(this T obj, string pathProperty, bool throwIfObjectIsNull = false) where T : class
        {
            if (pathProperty.IsNullOrWhiteSpace())
                return null;

            var elnts = pathProperty.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            Object res = obj;

            try
            {
                foreach (var el in elnts)
                    res = res.GetPropertyValue(el);
            }
            catch
            {
                if (throwIfObjectIsNull)
                    throw;
                return null;
            }

            return res;
        }
    }
}
