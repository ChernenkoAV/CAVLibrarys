using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace Cav
{
    /// <summary>
    /// Робота с коллекциями
    /// </summary>
    public static class ExtCollection
    {
        /// <summary>
        /// Соеденяет значения в коллекции с заданым разделителем
        /// </summary>
        /// <typeparam name="T">Тип идентификатора</typeparam>
        /// <param name="values">Значения</param>
        /// <param name="separator">Разделитель</param>
        /// <param name="distinct">Только уникальные значения</param>
        /// <param name="format">Формат преобразования к строке каждого объекта в коллекции(по умолчанию "{0}")</param>
        /// <returns>Значения разделенные разделителем</returns>
        public static string JoinValuesToString<T>(
            this IEnumerable<T> values,
            string separator = ",",
            Boolean distinct = true,
            String format = null)
        {
            if (values == null)
                return null;

            if (values.Count() == 0)
                return null;

            var vals = values;
            if (distinct)
                vals = values.Distinct();

            if (!typeof(T).IsValueType)
                vals = vals.Where(x => x != null).ToArray();

            format = format.GetNullIfIsNullOrWhiteSpace() ?? "{0}";

            return string.Join(separator, vals.Select(x => String.Format(format, x)).ToArray());
        }

        /// <summary>
        /// AddRange для коллекций, в которых этого расширения(метода) нет
        /// </summary>
        /// <param name="cT">Collection</param>
        /// <param name="collection">Коллекция для вставки</param>
        public static void AddRange<T>(this Collection<T> cT, IEnumerable<T> collection)
        {
            foreach (var item in collection)
                cT.Add(item);
        }
    }
}
