using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Cav
{
    /// <summary>
    /// Работа с Enum
    /// </summary>
#pragma warning disable CA1711 // Идентификаторы не должны иметь неправильных суффиксов
    public static class ExtEnum
#pragma warning restore CA1711 // Идентификаторы не должны иметь неправильных суффиксов
    {
        /// <summary>
        /// Получение значений <see cref="DescriptionAttribute"/> элементов перечесления
        /// </summary>
        /// <param name="value">Значение злемента перечесления</param>
        /// <returns>Содержимое <see cref="DescriptionAttribute"/>, либо, если атрибут отсутствует - ToString() элемента</returns>
        public static string GetEnumDescription(this Enum value)
        {
            if (value is null)
                return null;

            var fi = value.GetType().GetField(value.ToString());

            return fi.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault()?.Description ??
                value.ToString();
        }

        /// <summary>
        /// Получение коллекции значений из <see cref="Enum"/>, помеченного атрибутом  <see cref="FlagsAttribute"/>
        /// </summary>
        /// <remarks>В коллекцию не возвращается элемент со значением <see cref="int"/> = 0.</remarks>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static IEnumerable<T> FlagToList<T>(this T flag) where T : Enum =>
            Enum.GetValues(flag.GetType())
                .Cast<T>()
                .Where(x => Convert.ToUInt64(x) != 0L && flag.HasFlag(x)).ToArray();

        /// <summary>
        /// Получение коллекции значений-описаний (атрибут <see cref="DescriptionAttribute"/>) для типа перечесления.
        /// </summary>
        /// <param name="enumType">Тип перечисления</param>
        /// <param name="skipEmptyDescription">Пропускать значения с незаполненым описанием</param>
        /// <returns>Словарь значений и описаний перечисления</returns>
        public static IDictionary<Enum, String> GetEnumValueDescriptions(this Type enumType, bool skipEmptyDescription = true)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)
                throw new InvalidOperationException($"{enumType.FullName} is not Enum");

            var res = new Dictionary<Enum, String>();

            foreach (Enum enVal in enumType.GetEnumValues())
            {
                var fi = enumType.GetField(enVal.ToString());

                var description = fi.GetCustomAttribute<DescriptionAttribute>()?.Description;

                if (description.IsNullOrWhiteSpace() && skipEmptyDescription)
                    continue;

                res[enVal] = description;
            }

            return res;

        }
    }
}
