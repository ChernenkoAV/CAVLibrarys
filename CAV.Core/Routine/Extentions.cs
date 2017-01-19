using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        /// <param name="FileName">Файл, куда сохранить</param>
        public static void XMLSerialize(this object o, string FileName)
        {
            File.Delete(FileName);
            XmlSerializer xs = new XmlSerializer(o.GetType());

            using (Stream ms = File.Create(FileName))
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
        /// <param name="XDoc">XDocument, из которого десириализовать</param>
        /// <returns>Объект указанного типа</returns>
        public static T XMLDeserialize<T>(this XDocument XDoc)
        {
            if (XDoc == null)
                return default(T);

            XmlRootAttribute xra = new XmlRootAttribute(XDoc.Root.Name.LocalName);
            xra.Namespace = XDoc.Root.Name.Namespace.NamespaceName;
            XmlSerializer xs = new XmlSerializer(typeof(T), xra);

            using (StringReader sr = new StringReader(XDoc.ToString()))
                return (T)xs.Deserialize(sr);
        }

        /// <summary>
        /// Десиарелизатор 
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="XElm">XElement, из которого десириализовать</param>
        /// <returns>Объект указанного типа</returns>
        public static T XMLDeserialize<T>(this XElement XElm)
        {
            return XDocument.Parse(XElm.ToString()).XMLDeserialize<T>();
        }

        /// <summary>
        /// Десиарелизатор. Если XmlElement = null, то вернет default(T).
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="XElm">XmlElement, из которого десириализовать</param>
        /// <returns>Объект указанного типа или default(T), если XmlElement = null</returns>
        public static T XMLDeserialize<T>(this XmlElement XElm)
        {
            if (XElm == null)
                return default(T);
            return XDocument.Parse(XElm.OuterXml).XMLDeserialize<T>();
        }

        /// <summary>
        /// Десиреализатор из строки, содержащей XML.
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="xml">Строка, содержащая XML</param>
        /// <returns>Объект указанного типа или default(T), если строка IsNullOrWhiteSpace</returns>
        public static T XMLDeserialize<T>(this String xml)
        {
            if (xml.IsNullOrWhiteSpace())
                return default(T);

            return XDocument.Parse(xml).XMLDeserialize<T>();
        }


        /// <summary>
        /// Десиарелизатор из файла
        /// </summary>
        /// <typeparam name="T">Тип для десиарелизации</typeparam>
        /// <param name="FileName">Файл, из которого десириализовать</param>
        /// <returns>Объект указанного типа</returns>
        public static T XMLDeserializeFromFile<T>(this String FileName)
        {
            if (!File.Exists(FileName))
                return default(T);

            XDocument xdoc = XDocument.Load(FileName);

            XmlRootAttribute xra = new XmlRootAttribute(xdoc.Root.Name.LocalName);
            xra.Namespace = xdoc.Root.Name.Namespace.NamespaceName;
            XmlSerializer xs = new XmlSerializer(typeof(T), xra);

            using (StringReader sr = new StringReader(xdoc.ToString()))
                return (T)xs.Deserialize(sr);
        }

        /// <summary>
        /// Преобразование XML
        /// </summary>
        /// <param name="XML">XML для преобразования</param>
        /// <param name="XSLT">XSLT-шаблона перобразования </param>
        /// <returns>Результат преобразования</returns>
        public static String XMLTransform(this XDocument XML, XDocument XSLT)
        {
            XslCompiledTransform xct = new XslCompiledTransform();
            xct.Load(XSLT.CreateReader());

            StringBuilder res = new StringBuilder();

            using (TextWriter wr = new StringWriter(res))
            { xct.Transform(XML.CreateReader(), new XsltArgumentList(), wr); }

            return res.ToString();
        }

        /// <summary>
        /// Преобразование XML
        /// </summary>
        /// <param name="XML">XML для преобразования</param>
        /// <param name="XSLT">XSLT-шаблона перобразования </param>
        /// <returns>Результат преобразования</returns>
        public static String XMLTransform(this XDocument XML, String XSLT)
        {
            return XML.XMLTransform(XDocument.Parse(XSLT));
        }

        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="XML">Строка, содержащяя валидируемый xml</param>
        /// <param name="XSD">Строка, содержащая схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this String XML, String XSD)
        {
            return XDocument.Parse(XML).XMLValidate(XSD);
        }

        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="XML">XDocument, содержащий валидируемый xml</param>
        /// <param name="XSD">Строка, содержащая схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this XDocument XML, String XSD)
        {
            return XML.XMLValidate(XDocument.Parse(XSD));
        }

        /// <summary>
        /// Валидация xml схеме xsd
        /// </summary>
        /// <param name="XML">XDocument, содержащий валидируемый xml</param>
        /// <param name="XSD">XDocument, содержащий схему xsd</param>
        /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
        public static String XMLValidate(this XDocument XML, XDocument XSD)
        {
            XmlSchemaSet shs = new XmlSchemaSet();
            shs.Add("", XSD.CreateReader());
            String res = null;
            XML.Validate(shs, (a, b) => { res += b.Message + Environment.NewLine; });
            return res;
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
        /// <param name="Distinct">Только уникальные значения</param>
        /// <returns>Значения разделенные разделителем</returns>
        public static string JoinValuesToString<T>(this IEnumerable<T> values, string separator = ",", Boolean Distinct = true)
        {
            if (values == null)
                return null;

            if (values.Count() == 0)
                return null;

            var vals = values;
            if (Distinct)
                vals = values.Distinct();

            return string.Join(separator, vals.Select(x => x.ToString()).ToArray());
        }

        /// <summary>
        /// Получиние строки ИД-ков
        /// </summary>
        /// <typeparam name="T">Тип коллекции</typeparam>
        /// <param name="rows">Коллекция</param>
        /// <param name="ColumnName">Колонка для получения значений (Для коллекции DataRow)</param>
        /// <returns>Строка ИД-ков через ','</returns>
        public static String MakeIDsList<T>(this IEnumerable<T> rows, string ColumnName = "ID")
        {
            if (rows.FirstOrDefault() is DataRow)
                return rows.Cast<DataRow>().GetColumnValues<Guid>(ColumnName).JoinValuesToString();
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
                throw new ArgumentNullException("ComputeMD5Checksum:inputData == null");
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
                throw new ArgumentNullException("ComputeMD5Checksum:inputData == null");
            using (MemoryStream ms = new MemoryStream(inputData))
                return ComputeMD5Checksum(ms);
        }

        /// <summary>
        /// Вычисление MD5-хеша файла
        /// </summary>
        /// <param name="FilePath">Путь к файлу</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5Checksum(this string FilePath)
        {
            using (FileStream fs = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return ComputeMD5Checksum(fs);
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
    }
}