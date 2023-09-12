namespace Cav;

/// <summary>
/// Расширения работы сос троками
/// </summary>
public static class ExtString
{
    /// <summary>
    /// Усечение начальных и конечных пробелов и преводов кареток. Если строка null или состояла из пробелов и/или переводов кареток - вернет null.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string? Trim2(this string? str)
    {
        if (str == null)
            return null;

        str = str.Trim(new[] { ' ', '\r', '\n' });
        return str.GetNullIfIsNullOrWhiteSpace();
    }

    /// <summary>
    /// Возврат null, если IsNullOrWhiteSpace. 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string? GetNullIfIsNullOrWhiteSpace(this string? str) => str.IsNullOrWhiteSpace() ? null : str;

    /// <summary>
    /// true, если строка null, пустая или содержт только пробелы. Только это метод структуры String, а тут расширение....
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Извлекает подстроку из данного экземпляра. Подстрока начинается с указанной позиции и имеет указанную длину.
    /// Поведение максимально приближенно к SUBSTRING из T-SQL (без исключений)
    /// </summary>
    /// <param name="str">Исходный экземпляр строки</param>
    /// <param name="start">начальная позиция (с нуля)</param>
    /// <param name="length">Число извлекаемых символов (null - до конца строки)</param>
    /// <returns></returns>
    public static string? SubString(this string? str, int start, int? length = null)
    {
        if (str == null | start < 0 | length < 0)
            return null;

        var lstr = str!.Length;
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
    /// <param name="filePath">Путь, имя файла, путь файла</param>
    /// <param name="replasmentChar">Символ для замены. Если символ является запрещенным, то он приводится в подчеркиванию: "_"</param>
    /// <returns></returns>
    public static string? ReplaceInvalidPathChars(this string filePath, char replasmentChar = '_')
    {
        if (filePath.IsNullOrWhiteSpace())
            return null;

        var invchars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray();

        if (invchars.Contains(replasmentChar))
            replasmentChar = '_';

        foreach (var ic in invchars)
            filePath = filePath.Replace(ic, '_');

        return filePath;
    }

    /// <summary>
    /// Совпадение(вхождение) строк с реплейсом пробелов (регистронезависимонезависимо)
    /// </summary>
    /// <param name="str"></param>
    /// <param name="testString">Искомый текст</param>
    /// <param name="fullMatch">Искать полное совпадение</param>
    /// <returns></returns>
    public static bool MatchString(this string? str, string? testString, bool fullMatch = false)
    {
        if (str == null & testString == null)
            return true;

        if (str == null | testString == null)
            return false;

        str = str.ReplaceDoubleSpace()!.Trim().ToUpperInvariant();
        testString = testString.ReplaceDoubleSpace()!.Trim().ToUpperInvariant();

        return fullMatch ? str == testString : str.Contains(testString);
    }

    /// <summary>
    /// Удаление множественных пробелов до одного. Например: было "XXXX", станет "X". 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string? ReplaceDoubleSpace(this string? str)
    {
        if (str.IsNullOrWhiteSpace())
            return null;
        while (str!.Contains("  "))
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
    public static string? Stuff(this string? str, int start, int length, string replaceWith) =>
        str == null || start < 0 || length < 0
            ? null
            : str.Length < start
                ? str
                : str.SubString(0, start) + replaceWith + str.SubString(start + length);

    /// <summary>
    /// Замена символов на указанное значение
    /// </summary>
    /// <param name="str">Исходная строка</param>
    /// <param name="chars">Перечень символов для замены в виде строки</param>
    /// <param name="newValue">Значение, на которое заменяется символ</param>
    /// <returns>Измененная строка</returns>
    public static string? Replace2(this string? str, string chars, string newValue)
    {
        if (string.IsNullOrEmpty(chars))
            return str;

        if (str == null)
            return str;

        string? res = null;

        foreach (var charSource in str.ToArray())
            res = res + (chars.IndexOf(charSource) > -1 ? newValue : charSource.ToString());

        return res;
    }

    /// <summary>
    /// Выражение "null если" для строк
    /// </summary>
    /// <param name="exp">Проверяемое выражение</param>
    /// <param name="operand">Операнд сравнения</param>
    /// <returns></returns>
    public static string? NullIf(this string? exp, string? operand) => exp == operand ? null : exp;

    /// <summary>
    /// Повтор IFNULL() из T-SQL для строки
    /// </summary>
    /// <param name="val">Проверяемое значение. Проверка производится c помощью <see cref="IsNullOrWhiteSpace"/></param>
    /// <param name="operand">Значение подстановки</param>
    /// <returns></returns>
    public static string? IfNull(this string? val, string? operand) => val.IsNullOrWhiteSpace() ? operand : val;

    /// <summary>
    /// Удаление папки и всего, что в ней. Включая файлы с атрибутом ReadOnly
    /// </summary>
    /// <param name="path">Полный путь для удаления</param>
    public static void DeleteDirectory(this string path)
    {
        if (!Directory.Exists(path))
            return;

        var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

        foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            info.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }

    /// <summary>
    /// Удаление из начала строи совпадающей строки. Если исходная строка или терм замены <see cref="string.Empty"/> или null - возвращается исходная строка
    /// </summary>
    /// <param name="str">Исхоная строка</param>
    /// <param name="termVal">Терм замены</param>
    /// <returns></returns>
    public static string? TrimStart2(this string? str, string? termVal)
    {
        if (string.IsNullOrEmpty(termVal))
            return str;

        if (string.IsNullOrEmpty(str))
            return str;

        if (str!.IndexOf(termVal) != 0)
            return str;
#pragma warning disable IDE0057 // Substring можно упростить
        return str.Substring(termVal!.Length);
#pragma warning restore IDE0057 // Substring можно упростить
    }
}
