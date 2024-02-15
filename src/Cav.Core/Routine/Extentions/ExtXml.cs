using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.Xsl;

namespace Cav;

/// <summary>
/// Вспомогательные расширения для работы с Xml. Сериализация-десериализация, трансформация, валидация XML
/// </summary>
public static class ExtXml
{

    // кэш сериализаторов. Ато огромная течка памяти
    private static ConcurrentDictionary<string, Lazy<XmlSerializer>> cacheXmlSer = new();

    private static XmlSerializer getSerialize(Type type, XmlRootAttribute? rootAttrib = null)
    {
        var key = type.FullName;

        if (rootAttrib != null)
            key = $"{key}:{rootAttrib.Namespace}:{rootAttrib.ElementName}";

        return cacheXmlSer.GetOrAdd(key!, _ => new Lazy<XmlSerializer>(() => new XmlSerializer(type, rootAttrib), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    /// <summary>
    /// Сериализатор XML
    /// </summary>
    /// <param name="obj">Обьект</param>
    /// <param name="fileName">Файл, куда сохранить</param>
    public static void XMLSerialize<T>(this T? obj, string fileName)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        File.Delete(fileName);
        var xdoc = obj.XMLSerialize();

        using var wr = XmlWriter.Create(fileName);
        xdoc.WriteTo(wr);
    }

    /// <summary>
    /// Сериализатор 
    /// </summary>
    /// <param name="obj">Объект</param>
    /// <returns>Результат сериализации</returns>
    public static XDocument XMLSerialize(this object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var type = obj.GetType();
        var xmlRoot = type.GetCustomAttribute<XmlRootAttribute>();
        var xs = getSerialize(type, xmlRoot);
        var sb = new StringBuilder();

        using (var ms = XmlWriter.Create(sb))
            xs.Serialize(ms, obj);

        return XDocument.Parse(sb.ToString(), LoadOptions.PreserveWhitespace);
    }

    /// <summary>
    /// Десиарелизатор 
    /// </summary>
    /// <typeparam name="T">Тип для десиарелизации</typeparam>
    /// <param name="xDoc">XDocument, из которого десириализовать</param>
    /// <returns>Объект указанного типа</returns>
    public static T? XMLDeserialize<T>(this XContainer? xDoc) => (T?)xDoc.XMLDeserialize(typeof(T?));

    /// <summary>
    /// Десиреализатор
    /// </summary>
    /// <param name="xDoc">XML-документ, содержащий данные для десериализации</param>
    /// <param name="type">Тип</param>
    /// <returns></returns>
    public static object? XMLDeserialize(this XContainer? xDoc, Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (xDoc == null)
            return type.GetDefault();

#pragma warning disable CA1508 // Предотвращение появления неиспользуемого условного кода
        var el = xDoc as XElement ?? (xDoc as XDocument)!.Root;

        if (el == null)
            return type.GetDefault();
#pragma warning restore CA1508 // Предотвращение появления неиспользуемого условного кода

        var xra = new XmlRootAttribute()
        {
            ElementName = el.Name.LocalName,
            Namespace = el.Name.Namespace.NamespaceName
        };

        var xs = getSerialize(type, xra);

        using var sr = new StringReader(xDoc.ToString());
        using var xr = XmlReader.Create(sr);
        return xs.Deserialize(xr);
    }

    /// <summary>
    /// Десиарелизатор. Если XmlElement = null, то вернет default(T).
    /// </summary>
    /// <typeparam name="T">Тип для десиарелизации</typeparam>
    /// <param name="xmlElement">Элемент, из которого десириализовать</param>
    /// <returns>Объект указанного типа или default(T), если XmlElement = null</returns>
    public static T? XMLDeserialize<T>(this XmlElement xmlElement) =>
        xmlElement == null
            ? default
            : XDocument.Parse(xmlElement.OuterXml).XMLDeserialize<T?>();

    /// <summary>
    /// Десиреализатор из строки, содержащей XML.
    /// </summary>
    /// <typeparam name="T">Тип для десиарелизации</typeparam>
    /// <param name="xml">Строка, содержащая XML</param>
    /// <returns>Объект указанного типа или default(T), если строка IsNullOrWhiteSpace</returns>
    public static T? XMLDeserialize<T>(this string xml) => (T?)xml.XMLDeserialize(typeof(T?));

    /// <summary>
    /// Десиреализатор из строки, содержащей XML.
    /// </summary>
    /// <param name="xml">Строка, содержащая XML</param>
    /// <param name="type">Тип</param>
    /// <returns>Объект или default(T), если строка IsNullOrWhiteSpace</returns>
    public static object? XMLDeserialize(this string xml, Type type) =>
        type == null
            ? throw new ArgumentNullException(nameof(type))
            : xml.IsNullOrWhiteSpace()
                ? type.GetDefault()
                : XDocument.Parse(xml).XMLDeserialize(type);

    /// <summary>
    /// Десиарелизатор из файла
    /// </summary>
    /// <typeparam name="T">Тип для десиарелизации</typeparam>
    /// <param name="fileName">Файл, из которого десириализовать</param>
    /// <returns>Объект указанного типа</returns>
    public static T? XMLDeserializeFromFile<T>(this string fileName) => (T?)fileName.XMLDeserializeFromFile(typeof(T?));

    /// <summary>
    /// Десиарелизатор из файла
    /// </summary>
    /// <param name="fileName">Файл, из которого десириализовать</param>
    /// <param name="type">Тип</param>
    /// <returns>Объект</returns>
    public static object? XMLDeserializeFromFile(this string fileName, Type type) =>
        !File.Exists(fileName)
            ? type.GetDefault()
            : XDocument.Load(fileName).XMLDeserialize(type);

    /// <summary>
    /// Преобразование XML
    /// </summary>
    /// <param name="xml">XML для преобразования</param>
    /// <param name="xslt">XSLT-шаблона перобразования </param>
    /// <returns>Результат преобразования</returns>
    public static string XMLTransform(this XContainer xml, XContainer xslt)
    {
        if (xml is null)
            throw new ArgumentNullException(nameof(xml));
        if (xslt is null)
            throw new ArgumentNullException(nameof(xslt));

        var xct = new XslCompiledTransform();
        xct.Load(xslt.CreateReader());

        var res = new StringBuilder();

        using (TextWriter wr = new StringWriter(res))
            xct.Transform(xml.CreateReader(), new XsltArgumentList(), wr);

        return res.ToString();
    }

    /// <summary>
    /// Преобразование XML
    /// </summary>
    /// <param name="xml">XML для преобразования</param>
    /// <param name="xslt">XSLT-шаблона перобразования </param>
    /// <returns>Результат преобразования</returns>
    public static string XMLTransform(this XContainer xml, string xslt) => xml.XMLTransform(XDocument.Parse(xslt));

    /// <summary>
    /// Валидация xml схеме xsd
    /// </summary>
    /// <param name="xml">Строка, содержащяя валидируемый xml</param>
    /// <param name="xsd">Строка, содержащая схему xsd</param>
    /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
    public static string? XMLValidate(this string xml, string xsd) => XDocument.Parse(xml).XMLValidate(xsd);

    /// <summary>
    /// Валидация xml схеме xsd
    /// </summary>
    /// <param name="xml">XDocument, содержащий валидируемый xml</param>
    /// <param name="xsd">Строка, содержащая схему xsd</param>
    /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
    public static string? XMLValidate(this XDocument xml, string xsd) => xml.XMLValidate(XDocument.Parse(xsd));

    /// <summary>
    /// Валидация xml схеме xsd
    /// </summary>
    /// <param name="xml">XElement, содержащий валидируемый xml</param>
    /// <param name="xsd">Строка, содержащая схему xsd</param>
    /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
    public static string? XMLValidate(this XElement xml, string xsd) => xml.XMLValidate(XDocument.Parse(xsd));

    /// <summary>
    /// Валидация xml схеме xsd
    /// </summary>
    /// <param name="xml">XElement, содержащий валидируемый xml</param>
    /// <param name="xsd">XDocument, содержащий схему xsd</param>
    /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
    public static string? XMLValidate(this XElement xml, XDocument xsd) => new XDocument(xml).XMLValidate(xsd);

    /// <summary>
    /// Валидация xml схеме xsd
    /// </summary>
    /// <param name="xml">XDocument, содержащий валидируемый xml</param>
    /// <param name="xsd">XDocument, содержащий схему xsd</param>
    /// <returns>Текст реультатов валидации. Если валидация успешна - null</returns>
    public static string? XMLValidate(this XDocument xml, XDocument xsd)
    {
        string? res = null;

        if (xml is null)
            return res;
        if (xsd is null)
            return res;

        XmlSchema xs;
        using (var sr = new StringReader(xsd.ToString()))
        using (var xr = XmlReader.Create(sr))
            xs = XmlSchema.Read(xr, (a, b) => res += b.Message + Environment.NewLine)!;

#pragma warning disable CA1508 // Предотвращение появления неиспользуемого условного кода
        if (!res.IsNullOrWhiteSpace())
#pragma warning restore CA1508 // Предотвращение появления неиспользуемого условного кода
            throw new XmlSchemaException(res);

        if (xml.Root!.Name.NamespaceName != (xs.TargetNamespace ?? string.Empty))
            throw new XmlSchemaException($"пространство имен '{xml.Root.Name.NamespaceName}' элемента '{xml.Root.Name.LocalName}' не не соответствует целевому пространству имен схемы '{xs.TargetNamespace ?? string.Empty}'");

        var shs = new XmlSchemaSet();
        shs.Add(xs);

        xml.Validate(shs, (a, b) => res += b.Message + Environment.NewLine);
        return res;
    }
}
