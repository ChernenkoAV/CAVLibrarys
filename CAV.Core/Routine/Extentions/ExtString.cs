﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cav
{
    /// <summary>
    /// Расширения работы сос троками
    /// </summary>
    public static class ExtString
    {
        [ThreadStatic]
        private static Regex rexp = null;
        [ThreadStatic]
        private static String prn = null;
        /// <summary>
        /// Соответствие поисковому шаблону (формируется в регулярку)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static Boolean MatchText(this String text, String pattern)
        {
            if (pattern.IsNullOrWhiteSpace())
                return true;

            if (rexp == null || pattern != prn)
            {
                prn = pattern;
                rexp = new Regex(pattern.FormatRegexPattern(), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            return rexp.Matches(text).Count > 0;
        }

        /// <summary>
        /// Форматирование строки для передачи в качестве паттерна для регулярного выражения.
        /// Применять для передачи в хранимые процедуры, в которых используется Regex
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static String FormatRegexPattern(this String text)
        {
            if (text.IsNullOrWhiteSpace())
                return null;
            String res = text.Replace("^", "\\^").Replace(".", "\\.").Replace("$", "\\$").Replace("?", ".{1}").Replace("*", ".*");
            MatchCollection mc = (new Regex(@"\(.+?\)", RegexOptions.IgnoreCase | RegexOptions.Singleline)).Matches(text);
            foreach (Match item in mc)
                res = res.Replace(item.Value, item.Value.Replace(" ", @"\W+").Replace("(", @"(\b").Replace(")", @"\b)"));
            return res.Replace(" ", @"\W+");
        }

        /// <summary>
        /// Усечение начальных и конечных пробелов. Если строка null или состояла из пробелов - вернет null.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String Trim2(this String str)
        {
            if (str == null)
                return null;
            str = str.Trim();
            return str.GetNullIfIsNullOrWhiteSpace();
        }

        /// <summary>
        /// Возврат null, если IsNullOrWhiteSpace. 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String GetNullIfIsNullOrWhiteSpace(this String str)
        {
            if (str.IsNullOrWhiteSpace())
                return null;
            return str;
        }

        /// <summary>
        /// true, если строка null, пустая или содержт только пробелы. Только это метод структуры String, а тут расширение....
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Boolean IsNullOrWhiteSpace(this String str)
        {
            return String.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Извлекает подстроку из данного экземпляра. Подстрока начинается с указанной позиции и имеет указанную длину.
        /// Поведение максимально приближенно к SUBSTRING из T-SQL (без исключений)
        /// </summary>
        /// <param name="str">Исходный экземпляр строки</param>
        /// <param name="start">начальная позиция (с нуля)</param>
        /// <param name="length">Число извлекаемых символов (null - до конца строки)</param>
        /// <returns></returns>
        public static String SubString(this String str, int start, int? length = null)
        {
            if (str == null | start < 0 | length < 0)
                return null;

            int lstr = str.Length;
            length = length ?? lstr;

            if (start >= lstr)
                return null;

            if (start + length > lstr)
                length = lstr - start;

            return str.Substring(start, length.Value);
        }


        /// <summary>
        /// Замена символов, запрещенных в пути и имени файла, на указанный символ.
        /// </summary>
        /// <param name="FilePath">Путь, имя файла, путь файла</param>
        /// <param name="ReplasmentChar">Символ для замены. Если символ является запрещенным, то он приводится в подчеркиванию: "_"</param>
        /// <returns></returns>
        public static String ReplaceInvalidPathChars(this String FilePath, char ReplasmentChar = '_')
        {
            if (FilePath.IsNullOrWhiteSpace())
                return null;

            char[] invchars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray();

            if (invchars.Contains(ReplasmentChar))
                ReplasmentChar = '_';

            foreach (char ic in invchars)
                FilePath = FilePath.Replace(ic, '_');

            return FilePath;
        }


        /// <summary>
        /// Совпадение(вхождение) строк с реплейсом пробелов (регистронезависимонезависимо)
        /// </summary>
        /// <param name="str"></param>
        /// <param name="Pattern">Искомый текст</param>
        /// <param name="FullMatch">Искать полное совпадение</param>
        /// <returns></returns>
        public static Boolean MatchText(this String str, String Pattern, Boolean FullMatch = false)
        {
            if (str == null & Pattern == null)
                return true;

            if (str == null | Pattern == null)
                return false;

            str = str.Replace(" ", "").ToLowerInvariant();
            Pattern = Pattern.Replace(" ", "").ToLowerInvariant();

            Boolean res;
            if (FullMatch)
                res = str.Equals(Pattern);
            else
                res = str.Contains(Pattern);

            return res;
        }

        /// <summary>
        /// Удаление множественных пробелов до одного. Например: было "XXXX", станет "X". 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String ReplaceDoubleSpace(this String str)
        {
            if (str.IsNullOrWhiteSpace())
                return null;
            while (str.Contains("  "))
                str = str.Replace("  ", " ");
            return str;
        }

        /// <summary>
        /// "Наполнить" строку строкой. Если исходная строка содержит только пробелы - вернет null.
        /// Если индекс старта больше исходной строки - вернет исходную строку
        /// </summary>
        /// <param name="str">Исходная строка</param>
        /// <param name="start">Индекс, откуда вставлять строку замещения. Начинается с 0</param>
        /// <param name="length">Длина замещающей </param>
        /// <param name="replaceWith">Строка для наполнения</param>
        /// <returns></returns>
        public static String Stuff(this String str, int start, int length, string replaceWith)
        {
            if (str == null || start < 0 || length < 0)
                return null;

            if (str.Length < start)
                return str;

            return str.SubString(0, start) + replaceWith + str.SubString(start + length);
        }

        /// <summary>
        /// Замена символов на указанное значение
        /// </summary>
        /// <param name="str">Исходная строка</param>
        /// <param name="chars">Перечень символов для замены в виде строки</param>
        /// <param name="newValue">Значение, на которое заменяется символ</param>
        /// <returns>Измененная строка</returns>
        public static String Replace2(this String str, string chars, string newValue)
        {
            if (chars == null && chars == string.Empty)
                return str;

            if (str == null)
                return str;

            String res = null;

            foreach (var charSource in str.ToArray())
                res = res + (chars.IndexOf(charSource) > -1 ? newValue : charSource.ToString());

            return res;
        }
    }
}
