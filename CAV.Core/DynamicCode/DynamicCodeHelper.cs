using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.CSharp;

namespace Cav.DynamicCode
{
    /// <summary>
    /// Класс для работы с генерацией и компиляцией CS кода
    /// </summary>
    public static class DynamicCodeHelper
    {
        /// <summary>
        /// Генерация C# кода из Xsd
        /// </summary>
        /// <param name="xsd">Исходный xsd</param>
        /// <param name="namespaseType">Пространство имен для целевого типа</param>
        /// <param name="imports">Коллекция необходимых схем (в частности, импорты)</param>
        /// <returns></returns>
        public static StringBuilder GenerateCodeFromXsd(XDocument xsd, String namespaseType, params XDocument[] imports)
        {
            if (namespaseType.IsNullOrWhiteSpace())
                throw new ArgumentException("Не указано пространство имен");

            if (xsd == null)
                throw new ArgumentNullException("xsd");

            XmlSchema xsdSchema;

            using (var xr = xsd.CreateReader())
                xsdSchema = XmlSchema.Read(xr, null);

            XmlSchemas xsdSet = new XmlSchemas();
            xsdSet.Add(xsdSchema);

            foreach (var item in imports)
                using (var xr = xsd.CreateReader())
                    xsdSet.Add(XmlSchema.Read(xr, null));

            xsdSet.Compile(null, true);
            XmlSchemaImporter importer = new XmlSchemaImporter(xsdSet);

            CodeNamespace ns = new CodeNamespace(namespaseType);
            XmlCodeExporter exp = new XmlCodeExporter(ns);

            // Iterate schema items (top-level elements only) and generate code for each
            foreach (XmlSchemaObject item in xsdSchema.Items)
            {
                if (item is XmlSchemaElement)
                {
                    // Import the mapping first
                    XmlTypeMapping map = importer.ImportTypeMapping(
                      new XmlQualifiedName(((XmlSchemaElement)item).Name,
                      xsdSchema.TargetNamespace));
                    // Export the code finally
                    exp.ExportTypeMapping(map);
                }
            }

            // Code generator to build code with.
            var generator = new CSharpCodeProvider();

            StringBuilder codeType = new StringBuilder();
            var cdg = new CodeGeneratorOptions()
            {
                BracingStyle = "C"
            };


            using (var tw = new StringWriter(codeType))
                generator.GenerateCodeFromNamespace(ns, tw, cdg);

            codeType.AppendLine();

            return codeType;
        }

        /// <summary>
        /// Компиляция кода и загрузка полученной сборки в текущий домен приложения
        /// </summary>
        /// <param name="code">код</param>
        /// <param name="referencedAssembly">Референсные сборки для компиляции</param>
        /// <param name="outputAssembly">Путь к имени файла. null - генерация в памяти.</param>
        /// <returns></returns>
        public static Assembly CompileCode(
            StringBuilder code,
            string[] referencedAssembly = null,
            String outputAssembly = null)
        {
            if (code == null || code.ToString().IsNullOrWhiteSpace())
                throw new ArgumentNullException("code");
            //компиляция сборки
            var provider = new CSharpCodeProvider();

            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = outputAssembly.IsNullOrWhiteSpace();
            if (!outputAssembly.IsNullOrWhiteSpace())
                parameters.OutputAssembly = outputAssembly;
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            if (referencedAssembly != null)
                foreach (var item in referencedAssembly)
                    parameters.ReferencedAssemblies.Add(item);

            var cr = provider.CompileAssemblyFromSource(parameters, code.ToString());
            if (cr.Errors.HasErrors)
            {
                String msgtxt = cr.Errors.Cast<CompilerError>()
                    .Select(x => String.Format("{0} ({1}:{2}): {3}", x.ErrorNumber, x.Line, x.Column, x.ErrorText))
                    .JoinValuesToString(Environment.NewLine);
                throw new InvalidOperationException(msgtxt);
            }

            return cr.CompiledAssembly;
        }
    }
}

