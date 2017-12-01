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
    public static class ExtEnum
    {
        /// <summary>
        /// Получение значений <see cref="DescriptionAttribute"/> элементов перечесления
        /// </summary>
        /// <param name="value">Значение злемента перечесления</param>
        /// <returns>Содержимое <see cref="DescriptionAttribute"/>, либо, если атрибут отсутствует - ToString() элемента</returns>
        public static string GetEnumDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        /// <summary>
        /// Получение коллекции значений из <see cref="Enum"/>, помеченного атрибутом  <see cref="FlagsAttribute"/>
        /// </summary>
        /// <remarks>В коллекцию не возвращается элемент со значением <see cref="int"/> = 0.</remarks>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static IEnumerable<Enum> FlagToList(this Enum flag)
        {
            return Enum.GetValues(flag.GetType())
                                 .Cast<Enum>()
                                 .Where(m => Convert.ToUInt64(m) != 0L && flag.HasFlag(m));
        }
    }
}
