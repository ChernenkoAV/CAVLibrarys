using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.Xsl;

namespace Cav
{
    /// <summary>
    /// Вспомогательные расширения для работы с Xml. Сериализация-десериализация, трансформация, валидация XML
    /// </summary>
    public static class ExtXml
    {

        // кэш сериализаторов. Ато огромная течка памяти
        private static ConcurrentDictionary<String, XmlSerializer> cacheXmlSer = new ConcurrentDictionary<string, XmlSerializer>();

        private static XmlSerializer getSerialize(Type type, XmlRootAttribute rootAttrib = null)
        {
            var key = type.FullName;

            if (rootAttrib != null)
                key = $"{key}:{rootAttrib.Namespace}:{rootAttrib.ElementName}";

            return cacheXmlSer.GetOrAdd(key, new XmlSerializer(type, rootAttrib));
        }

        /// <summary>
        /// Сериализатор XML
        /// </summary>
        /// <param name="o">Обьект</param>
        /// <param name="fileName">Файл, куда сохранить</param>
        public static void XMLSerialize(this object o, string fileName)
        {
            File.Delete(fileName);
            var xs = getSerialize(o.GetType());

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
            var xs = getSerialize(o.GetType());
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

            XmlRootAttribute xra = new XmlRootAttribute()
            {
                ElementName = el.Name.LocalName,
                Namespace = el.Name.Namespace.NamespaceName
            };

            var xs = getSerialize(type, xra);

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
    }
}
