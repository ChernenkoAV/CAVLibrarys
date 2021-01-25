using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Cav.ReflectHelpers;

namespace Cav.Routine.Extentions
{
    /// <summary>
    /// Расширения-хелперы для построения деревьев выражений
    /// </summary>
    public static class ExtExpressionTree
    {
        /// <summary>
        /// Добавление <see cref="XElement"/> с контентом в указанный <see cref="XElement"/>>
        /// </summary>
        /// <param name="instanseXElement">экземляр <see cref="XElement"/>, в который буде производиться добавление дочерненго элемента</param>
        /// <param name="newElementName">имя нового элемента</param>
        /// <param name="xmlNamespace">пространство имен нового элемента</param>
        /// <param name="contentValue">контент тового элемента</param>
        /// <returns></returns>
        public static Expression XElement_AddContentValue(this Expression instanseXElement, string newElementName, string xmlNamespace, Expression contentValue)
        {
            XNamespace ns = xmlNamespace.GetNullIfIsNullOrWhiteSpace() ?? XNamespace.None;

            return Expression.Call(
                        instanseXElement,
                        typeof(XElement).GetMethod(nameof(XElement.Add), new[] { typeof(object) }),
                        Expression.New(
                            typeof(XElement).GetConstructor(new[] { typeof(XName), typeof(object) }),
                            Expression.Convert(Expression.Add(Expression.Constant(ns), Expression.Constant(newElementName)), typeof(XName)),
                            Expression.Convert(contentValue, typeof(object))));
        }

        /// <summary>
        /// Добавление контента в существующий экземпляр <see cref="XElement"/>
        /// </summary>
        /// <param name="instanseXElement">экземляр <see cref="XElement"/>, в который буде производиться добавление контента</param>
        /// <param name="contentValue">контент</param>
        /// <returns></returns>
        public static Expression XElement_AddContent(this Expression instanseXElement, Expression contentValue)
        {
            return Expression.Call(
                        instanseXElement,
                        typeof(XElement).GetMethod(nameof(XElement.Add), new[] { typeof(object) }),
                        Expression.Convert(contentValue, typeof(object)));
        }

        /// <summary>
        /// Создание нового элемента <see cref="XElement"/> с указанным именем и пространсвом имен
        /// </summary>
        /// <param name="newElementName">имя еэлемента</param>
        /// <param name="xmlNamespace">пространство имен</param>
        /// <returns></returns>
        public static Expression XElement_New(this string newElementName, string xmlNamespace = null)
        {
            xmlNamespace = xmlNamespace.GetNullIfIsNullOrWhiteSpace() ?? String.Empty;
            return newElementName.XElementNew(Expression.Convert(Expression.Constant(xmlNamespace), typeof(XNamespace)));
        }

        /// <summary>
        /// Создание нового элемента <see cref="XElement"/> с указанным именем и пространсвом имен
        /// </summary>
        /// <param name="newElementName">имя еэлемента</param>
        /// <param name="xNamespace">Выражение, содержащее пространство имен</param>
        /// <returns></returns>
        public static Expression XElementNew(this string newElementName, Expression xNamespace)
        {
            return Expression.New(typeof(XElement).GetConstructor(new[] { typeof(XName) }), Expression.Convert(Expression.Add(xNamespace, Expression.Constant(newElementName)), typeof(XName)));
        }

        /// <summary>
        /// Реализация выражения цикла обхода коллекци
        /// </summary>
        /// <param name="collection">экземпляр коллекции</param>
        /// <param name="loopVar">переменная, в которую будет присвоен элемент коллекции при итерации</param>
        /// <param name="loopContent">блок, содержащий действия, призводимые при итерации</param>
        /// <returns></returns>
        public static Expression ForEach(this Expression collection, ParameterExpression loopVar, Expression loopContent)
        {
            var elementType = loopVar.Type;
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

            var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator"));
            var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

            // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

            var breakLabel = Expression.Label("LoopBreak");

            var loop = Expression.Block(new[] { enumeratorVar },
                enumeratorAssign,
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Equal(moveNextCall, Expression.Constant(true)),
                        Expression.Block(new[] { loopVar },
                            Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
                            loopContent
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel)
            );

            return loop;
        }

        /// <summary>
        /// Вызов <see cref="Enumerable.Count{TSource}(IEnumerable{TSource})"/> для коллекции
        /// </summary>
        /// <param name="iEnumerableCollection">экземпляр коллекции</param>
        /// <returns></returns>
        public static Expression Count(this Expression iEnumerableCollection)
        {
            var resType = iEnumerableCollection.Type;

            return Expression.Call(
                typeof(Enumerable)
                    .GetMethods()
                    .Single(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 1)
                    .MakeGenericMethod(resType.GetEnumeratedType()),
                iEnumerableCollection);
        }

        /// <summary>
        /// Вызов <see cref="Enumerable.First{TSource}(IEnumerable{TSource})"/> для коллекции
        /// </summary>
        /// <param name="iEnumerableCollection">экземпляр коллекции</param>
        /// <returns></returns>
        public static Expression First(this Expression iEnumerableCollection)
        {
            var resType = iEnumerableCollection.Type;

            return Expression.Call(
                typeof(Enumerable)
                    .GetMethods()
                    .Single(m => m.Name == nameof(Enumerable.First) && m.GetParameters().Length == 1)
                    .MakeGenericMethod(resType.GetEnumeratedType()),
                iEnumerableCollection);
        }

        /// <summary>
        /// Вызов <see cref="XContainer.Elements(XName)"/> для <see cref="XElement"/>
        /// </summary>
        /// <param name="sourceXml">экземпляр <see cref="XElement"/></param>
        /// <param name="termName">терм поиска имени</param>
        /// <returns></returns>
        public static Expression XElement_Elements(this Expression sourceXml, string termName)
        {
            return Expression.Call(
                sourceXml,
                typeof(XElement).GetMethod(nameof(XElement.Elements), new[] { typeof(XName) }),
                Expression.Convert(Expression.Constant(termName), typeof(XName)));
        }

        /// <summary>
        /// Вызов <see cref="XContainer.Element(XName)"/> для <see cref="XElement"/>
        /// </summary>
        /// <param name="sourceXml">экземпляр <see cref="XElement"/></param>
        /// <param name="termName">терм поиска имени</param>
        /// <returns></returns>
        public static Expression XElement_Element(this Expression sourceXml, string termName)
        {
            return Expression.Call(
                sourceXml,
                typeof(XElement).GetMethod(nameof(XElement.Element), new[] { typeof(XName) }),
                Expression.Convert(Expression.Constant(termName), typeof(XName)));
        }

        /// <summary>
        /// Вызов <see cref="List{T}.Add(T)"/> для <see cref="List{T}"/>
        /// </summary>
        /// <param name="listCollection">экземпляр <see cref="List{T}.Add(T)"/></param>
        /// <param name="value">выражение объекта, вставляемого в коллекцию</param>
        /// <returns></returns>
        public static Expression ListAdd(this Expression listCollection, Expression value)
        {
            var resType = listCollection.Type;
            var itemType = resType.GetEnumeratedType();

            if (value.Type != itemType)
                value = Expression.Convert(value, itemType);

            return Expression.Call(
                          listCollection,
                          resType.GetMethod(nameof(IList.Add), new[] { itemType }),
                          value);
        }

    }
}
