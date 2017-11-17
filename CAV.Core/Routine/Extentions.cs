using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.Xsl;

namespace Cav
{
    /// <summary>
    /// Вспомогательные расширения
    /// </summary>
    public static class Extentions
    {
        #region Сериализация-десериализация, трансформация, валидация XML

        /// <summary>
        /// Сериализатор XML
        /// </summary>
        /// <param name="o">Обьект</param>
        /// <param name="fileName">Файл, куда сохранить</param>
        public static void XMLSerialize(this object o, string fileName)
        {
            File.Delete(fileName);
            XmlSerializer xs = new XmlSerializer(o.GetType());

            using (Stream ms = File.Create(fileName))
                xs.Serialize(ms, o);
        }

        /// <summary>
        /// Сериализатор 
        /// </summary>
        /// <param name="o">Объект</param>
        /// <returns>Результат сериализации</returns>
        public static XDocument XMLSerialize(this object o)
        {
            XmlSerializer xs = new XmlSerializer(o.GetType());
            StringBuilder sb = new StringBuilder();

            using (XmlWriter ms = XmlTextWriter.Create(sb))
                xs.Serialize(ms, o);

            return XDocument.Parse(sb.ToString(), LoadOptions.PreserveWhitespace);
        }

        /// <summary>
        /// Десиарелизатор 
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="xDoc">XDocument, из которого десириализовать</param>
        /// <returns>Объект указанного типа</returns>
        public static T XMLDeserialize<T>(this XContainer xDoc)
        {
            return (T)xDoc.XMLDeserialize(typeof(T));
        }

        /// <summary>
        /// Десиреализатор
        /// </summary>
        /// <param name="xDoc">XML-документ, содержащий данные для десериализации</param>
        /// <param name="type">Тип</param>
        /// <returns></returns>
        public static Object XMLDeserialize(this XContainer xDoc, Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (xDoc == null)
                return type.GetDefault();

            XElement el = (xDoc as XElement) ?? (xDoc as XDocument).Root;

            if (el == null)
                return type.GetDefault();

            XmlRootAttribute xra = new XmlRootAttribute(el.Name.LocalName);
            xra.Namespace = el.Name.Namespace.NamespaceName;
            XmlSerializer xs = new XmlSerializer(type, xra);

            using (StringReader sr = new StringReader(xDoc.ToString()))
                return xs.Deserialize(sr);
        }

        /// <summary>
        /// Десиарелизатор. Если XmlElement = null, то вернет default(T).
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="xmlElement">Элемент, из которого десириализовать</param>
        /// <returns>Объект указанного типа или default(T), если XmlElement = null</returns>
        public static T XMLDeserialize<T>(this XmlElement xmlElement)
        {
            if (xmlElement == null)
                return default(T);

            return XDocument.Parse(xmlElement.OuterXml).XMLDeserialize<T>();
        }

        /// <summary>
        /// Десиреализатор из строки, содержащей XML.
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="xml">Строка, содержащая XML</param>
        /// <returns>Объект указанного типа или default(T), если строка IsNullOrWhiteSpace</returns>
        public static T XMLDeserialize<T>(this String xml)
        {
            return (T)xml.XMLDeserialize(typeof(T));
        }

        /// <summary>
        /// Десиреализатор из строки, содержащей XML.
        /// </summary>
        /// <param name="xml">Строка, содержащая XML</param>
        /// <param name="type">Тип</param>
        /// <returns>Объект или default(T), если строка IsNullOrWhiteSpace</returns>
        public static Object XMLDeserialize(this String xml, Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (xml.IsNullOrWhiteSpace())
                return type.GetDefault();

            return XDocument.Parse(xml).XMLDeserialize(type);
        }


        /// <summary>
        /// Десиарелизатор из файла
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="fileName">Файл, из которого десириализовать</param>
        /// <returns>Объект указанного типа</returns>
        public static T XMLDeserializeFromFile<T>(this String fileName)
        {
            return (T)fileName.XMLDeserializeFromFile(typeof(T));
        }

        /// <summary>
        /// Десиарелизатор из файла
        /// </summary>
        /// <param name="fileName">Файл, из которого десириализовать</param>
        /// <param name="type">Тип</param>
        /// <returns>Объект</returns>
        public static Object XMLDeserializeFromFile(this String fileName, Type type)
        {
            if (!File.Exists(fileName))
                return type.GetDefault();

            return XDocument.Load(fileName).XMLDeserialize(type);
        }

        /// <summary>
        /// Преобразование XML
        /// </summary>
        /// <param name="xml">XML для преобразования</param>
        /// <param name="xslt">XSLT-шаблона перобразования </param>
        /// <returns>Результат преобразования</returns>
        public static String XMLTransform(this XContainer xml, XContainer xslt)
        {
            XslCompiledTransform xct = new XslCompiledTransform();
            xct.Load(xslt.CreateReader());

            StringBuilder res = new StringBuilder();

            using (TextWriter wr = new StringWriter(res))
            { xct.Transform(xml.CreateReader(), new XsltArgumentList(), wr); }

            return res.ToString();
        }

        /// <summary>
        /// Преобразование XML
        /// </summary>
        /// <param name="xml">XML для преобразования</param>
        /// <param name="xslt">XSLT-шаблона перобразования </param>
        /// <returns>Результат преобразования</returns>
        public static String XMLTransform(this XContainer xml, String xslt)
        {
            return xml.XMLTransform(XDocument.Parse(xslt));
        }

        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="xml">Строка, содержащяя валидируемый xml</param>
        /// <param name="xsd">Строка, содержащая схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this String xml, String xsd)
        {
            return XDocument.Parse(xml).XMLValidate(xsd);
        }

        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="xml">XDocument, содержащий валидируемый xml</param>
        /// <param name="xsd">Строка, содержащая схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this XDocument xml, String xsd)
        {
            return xml.XMLValidate(XDocument.Parse(xsd));
        }

        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="xml">XElement, содержащий валидируемый xml</param>
        /// <param name="xsd">Строка, содержащая схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this XElement xml, String xsd)
        {
            return xml.XMLValidate(XDocument.Parse(xsd));
        }


        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="xml">XElement, содержащий валидируемый xml</param>
        /// <param name="xsd">XDocument, содержащий схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this XElement xml, XDocument xsd)
        {
            return (new XDocument(xml)).XMLValidate(xsd);
        }

        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="xml">XDocument, содержащий валидируемый xml</param>
        /// <param name="xsd">XDocument, содержащий схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this XDocument xml, XDocument xsd)
        {
            XmlSchemaSet shs = new XmlSchemaSet();
            shs.Add("", xsd.CreateReader());
            String res = null;
            xml.Validate(shs, (a, b) => { res += b.Message + Environment.NewLine; });
            return res;
        }

        #endregion

        #region Сериализация-десериализация JSON

        /// <summary>
        /// Сериализация объекта в строку JSON
        /// </summary>
        /// <param name="obj">Объект десиреализации</param>
        /// <returns>Результирующая строка JSON</returns>
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
        /// Десиреализация из строки в JSON формате
        /// </summary>
        /// <typeparam name="T">Тип десериализации</typeparam>
        /// <param name="str">Исходная строка</param>
        /// <returns>Результат десериализации</returns>
        public static T JSONDeserialize<T>(this String str)
        {
            return (T)str.JSONDeserialize(typeof(T));
        }

        /// <summary>
        /// Десиреализация из строки в JSON формате
        /// </summary>        
        /// <param name="str">Исходная строка</param>
        /// <param name="targetType">Тип десериализации</param>
        /// <returns>Результат десериализации</returns>
        public static object JSONDeserialize(this String str, Type targetType)
        {
            if (str.IsNullOrWhiteSpace())
                return targetType.GetDefault();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
                return (new DataContractJsonSerializer(targetType)).ReadObject(ms);
        }

        #endregion

        #region Работа со строками

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
        /// <param name="startIndex">начальная позиция (с нуля)</param>
        /// <param name="length">Число извлекаемых символов (null - до конца строки)</param>
        /// <returns></returns>
        public static String SubString(this String str, int startIndex, int? length = null)
        {
            if (str == null | startIndex < 0 | length < 0)
                return null;

            int lstr = str.Length;
            length = length ?? lstr;

            if (startIndex >= lstr)
                return null;

            if (startIndex + length > lstr)
                length = lstr - startIndex;

            return str.Substring(startIndex, length.Value);
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
        /// Удаление множественных пробелов до одного. Например: было "     ", станет " ". 
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

        #endregion

        #region Развертывание текста исключения
        /// <summary>
        /// Развертывание текста исключения + обработка SqlException
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static String Expand(this Exception ex)
        {
            return
                ex == null
                ? String.Empty
                : (ex is SqlException
                    ? "SqlException №:" + ((SqlException)ex).Number + " " + ex.Message
                    : "Message: " + ex.Message + (ex.StackTrace.IsNullOrWhiteSpace() ? String.Empty : Environment.NewLine + "StackTrace: " + ex.StackTrace)) +
                  Environment.NewLine +
                  ex.InnerException.Expand();
        }

        #endregion

        #region GZip

        /// <summary>
        /// Gzip сжатие массива байт
        /// </summary>
        /// <param name="sourse"></param>
        /// <returns></returns>
        public static byte[] GZipCompress(this byte[] sourse)
        {
            using (MemoryStream result = new MemoryStream())
            {
                using (GZipStream tstream = new GZipStream(result, CompressionMode.Compress))
                    tstream.Write(sourse, 0, sourse.Length);

                return result.ToArray();
            }
        }

        /// <summary>
        /// Распаковка GZip
        /// </summary>
        /// <param name="sourse"></param>
        /// <returns></returns>
        public static byte[] GZipDecompress(this byte[] sourse)
        {
            using (MemoryStream sms = new MemoryStream(sourse))
            using (GZipStream tstream = new GZipStream(sms, CompressionMode.Decompress))
            using (MemoryStream result = new MemoryStream())
            {
                byte[] buffer = new byte[1024];
                int readBytes = 0;

                do
                {
                    readBytes = tstream.Read(buffer, 0, buffer.Length);
                    result.Write(buffer, 0, readBytes);
                } while (readBytes != 0);

                return result.ToArray();
            }
        }


        #endregion

        #region Коньюктирование коллекций в строку

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
        /// Получиние строки ИД-ков
        /// </summary>
        /// <typeparam name="T">Тип коллекции</typeparam>
        /// <param name="rows">Коллекция</param>
        /// <param name="columnName">Колонка для получения значений (Для коллекции DataRow)</param>
        /// <returns>Строка ИД-ков через ','</returns>
        public static String MakeIDsList<T>(this IEnumerable<T> rows, string columnName = "ID")
        {
            if (rows.FirstOrDefault() is DataRow)
                return rows.Cast<DataRow>().GetColumnValues<Guid>(columnName).JoinValuesToString();
            else
                return rows.JoinValuesToString();
        }

        #endregion

        #region Считаем MD5

        /// <summary>
        /// Вычисление MD5-хеша для потока
        /// </summary>
        /// <param name="inputData">Поток</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5Checksum(this Stream inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(inputData)}");
            MD5 md5 = new MD5CryptoServiceProvider();
            return new Guid(md5.ComputeHash(inputData));
        }

        /// <summary>
        /// Вычисление MD5-хеша массива байт
        /// </summary>
        /// <param name="inputData">Массив байт</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5Checksum(this byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(inputData)}");
            using (MemoryStream ms = new MemoryStream(inputData))
                return ComputeMD5Checksum(ms);
        }

        /// <summary>
        /// Вычисление MD5-хеша файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5ChecksumFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace())
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(filePath)}");

            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return ComputeMD5Checksum(fs);
        }

        /// <summary>
        /// Вычисление MD5-хеша строки. Байты берутся UTF8.
        /// </summary>
        /// <param name="str">Входная строка</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5ChecksumString(this string str)
        {
            if (str.IsNullOrWhiteSpace())
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(str)}");

            return Encoding.UTF8.GetBytes(str).ComputeMD5Checksum();
        }

        #endregion

        #region Взаимодействие с DataRow(DataRowView)

        /// <summary>
        /// Получение значения поля ID в DataRow. Если поля нет - исключение...
        /// </summary>
        /// <param name="drow"></param>
        /// <returns></returns>
        public static Guid GetID(this DataRow drow)
        {
            return (Guid)drow["ID"];
        }

        /// <summary>
        /// Получение значения поля ID в DataRow. Если поля нет - исключение...
        /// </summary>
        /// <param name="drow"></param>
        /// <returns></returns>
        public static Guid? GetParentID(this DataRow drow)
        {
            return drow.GetColumnValue("Parent") as Guid?;
        }

        /// <summary>
        /// Получение значения поля ID в DataRowView. Если поля нет - исключение...
        /// </summary>
        /// <param name="drow"></param>
        /// <returns></returns>
        public static Guid GetID(this DataRowView drow)
        {
            return drow.Row.GetID();
        }

        /// <summary>
        /// Получить значение поля
        /// </summary>
        /// <param name="row">Строка таблицы</param>
        /// <param name="ColumnName">Наименование поля</param>
        /// <returns>Значение (если значения нет, то null)</returns>
        public static object GetColumnValue(this DataRow row, string ColumnName = "ID")
        {
            if (row == null)
                return null;

            var value = row.IsNull(ColumnName) ? null : row[ColumnName];

            if (value is DBNull)
                return null;

            return value;
        }


        // дотНет 3.5 не умеет кастить коллекции. сцучко.... 
        /// <summary>
        /// Получение коллекции значений поля из DataTable
        /// </summary>
        /// <typeparam name="T">Результирующий тип</typeparam>
        /// <param name="Table">Таблица</param>
        /// <param name="ColumnName">Наименование колонки</param>
        /// <returns>Лист значений</returns>
        public static List<T> GetColumnValues<T>(this DataTable Table, String ColumnName = "ID")
        {
            return Table.Rows.Cast<DataRow>().GetColumnValues<T>(ColumnName);
        }

        /// <summary>
        /// Получение коллекции значений поля из коллекции строк DataRow
        /// </summary>
        /// <typeparam name="T">Результирующий тип</typeparam>
        /// <param name="ERows">коллекция строк</param>
        /// <param name="ColumnName">Наименование колонки</param>
        /// <returns>Лист значений</returns>
        public static List<T> GetColumnValues<T>(this IEnumerable<DataRow> ERows, String ColumnName = "ID")
        {
            List<T> res = new List<T>();

            foreach (var row in ERows)
            {
                if (row[ColumnName] is DBNull)
                    continue; // Пропускаем DBNull

                if (res.Contains((T)row[ColumnName]))
                    continue; // убираем дубликаты

                res.Add((T)row[ColumnName]);
            }

            return res;
        }

        #endregion

        #region AddRange для коллекций

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

        #endregion

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

        #region Работа с Enum

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

        #endregion


        #region Сериализация-десериализация и шифрование
        /// <summary>
        /// Сериализация объекта и шифрование алгоритмом AES
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="key">Ключ шифрования</param>
        /// <returns>Зашифрованный объект</returns>
        public static byte[] SerializeAesEncrypt(this Object obj, String key)
        {
            if (obj == null)
                return null;

            byte[] keyByte = Encoding.UTF8.GetBytes(key).ComputeMD5Checksum().ToByteArray();
            byte[] data = Encoding.UTF8.GetBytes(obj.JSONSerialize()).GZipCompress();

            var aes = new AesCryptoServiceProvider();
            aes.Key = keyByte;
            aes.IV = keyByte;

            using (ICryptoTransform crtr = aes.CreateEncryptor())
            using (var memres = new MemoryStream())
            using (var crstr = new CryptoStream(memres, crtr, CryptoStreamMode.Write))
            {
                crstr.Write(data, 0, data.Length);
                crstr.FlushFinalBlock();
                data = memres.ToArray();
            }

            return data;
        }

        /// <summary>
        /// Дешифрация алгоритмом AES и десереализация объекта из массива шифра. Работает только после <see cref="Extentions.SerializeAesEncrypt(object, string)"/>
        /// , так как с ключом производятся манипуляции
        /// </summary>
        /// <typeparam name="T">Тип для десериализации</typeparam>
        /// <param name="data">Массив шифрованных данных</param>
        /// <param name="key">Ключь шифрования</param>
        /// <returns></returns>
        public static T DeserializeAesDecrypt<T>(this byte[] data, String key)
        {
            if (data == null)
                return default(T);

            byte[] keyByte = Encoding.UTF8.GetBytes(key).ComputeMD5Checksum().ToByteArray();

            var aes = new AesCryptoServiceProvider();
            aes.Key = keyByte;
            aes.IV = keyByte;

            using (ICryptoTransform crtr = aes.CreateDecryptor())
            using (var memres = new MemoryStream())
            using (var crstr = new CryptoStream(memres, crtr, CryptoStreamMode.Write))
            {
                crstr.Write(data, 0, data.Length);
                crstr.FlushFinalBlock();
                data = memres.ToArray();
            }

            return Encoding.UTF8.GetString(data.GZipDecompress()).JSONDeserialize<T>();
        }

        #endregion

        #region Кварталы даты

        /// <summary>
        /// Получение квартала указанной даты
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int Quarter(this DateTime dateTime)
        {
            return ((dateTime.Month - 1) / 3) + 1;
        }

        /// <summary>
        /// Получение первого дня квартала, в котором находится указанная дата
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime FirstDayQuarter(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, (dateTime.Quarter() * 3) - 2, 1, 0, 0, 0, dateTime.Kind);
        }

        /// <summary>
        /// Получение последнего дня квартала. Время 23:59:59.9999
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime LastDayQuarter(this DateTime dateTime)
        {
            return dateTime.Add(-dateTime.TimeOfDay).AddDays(-dateTime.Day + 1).AddMonths((dateTime.Quarter() * 3) - dateTime.Month + 1).AddMilliseconds(-1);
        }

        #endregion
    }
}