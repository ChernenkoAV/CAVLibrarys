using System;

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
    }
}
