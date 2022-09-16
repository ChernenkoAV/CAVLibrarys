using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Cav.ReflectHelpers
{
    /// <summary>
    /// Расширения упрощения вызовов рефлексии
    /// </summary>
    public static class ExtReflection
    {
        /// <summary>
        /// Получения значения свойства у объекта
        /// </summary>
        /// <param name="obj">экземпляр объекта</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <returns></returns>
        public static Object GetPropertyValue(this object obj, String propertyName)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException($"\"{nameof(propertyName)}\" не может быть пустым или содержать только пробел.", nameof(propertyName));

            return obj.GetType().GetProperty(propertyName).GetValue(obj);
        }
        /// <summary>
        /// Получение значения статического свойства / константного поля
        /// </summary>
        /// <param name="asm">Сборка, сожержащая тип</param>
        /// <param name="className">Имя класса</param>
        /// <param name="namePropertyOrField">Имя свойства или поля</param>
        /// <returns></returns>
        public static Object GetStaticOrConstPropertyOrFieldValue(this Assembly asm, String className, String namePropertyOrField)
        {
            if (asm is null)
                throw new ArgumentNullException(nameof(asm));
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException($"\"{nameof(className)}\" не может быть неопределенным или пустым.", nameof(className));
            if (string.IsNullOrWhiteSpace(namePropertyOrField))
                throw new ArgumentException($"\"{nameof(namePropertyOrField)}\" не может быть неопределенным или пустым.", nameof(namePropertyOrField));

            var type = asm.ExportedTypes.Single(x => x.Name == className || x.FullName == className);
            Object res = null;
            var prop = type.GetProperty(namePropertyOrField);

            res = prop != null ? prop.GetValue(null) : type.GetField(namePropertyOrField).GetValue(null);

            return res;
        }

        /// <summary>
        /// Получение значений статических свойств коллекцией
        /// </summary>
        /// <param name="type">Просматреваемый тип</param>
        /// <param name="typeProperty">Тип свойста</param>
        /// <returns></returns>
        public static ReadOnlyCollection<Object> GetStaticPropertys(this Type type, Type typeProperty)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (typeProperty is null)
                throw new ArgumentNullException(nameof(typeProperty));

            var res = new List<object>();

            var flds = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var fld in flds.Where(x => x.PropertyType == typeProperty))
                res.Add(fld.GetValue(null));

            return new ReadOnlyCollection<object>(res);

        }

        /// <summary>
        /// Получение значений статических свойств коллекцией
        /// </summary>
        /// <typeparam name="T">Тип свойства для коллекции</typeparam>
        /// <param name="type">Просматреваемый тип</param>
        /// <returns></returns>
        public static ReadOnlyCollection<T> GetStaticPropertys<T>(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            return new ReadOnlyCollection<T>(type.GetStaticPropertys(typeof(T)).Cast<T>().ToArray());
        }

        /// <summary>
        /// Получение значений статических полей коллекцией
        /// </summary>
        /// <param name="type">Просматреваемый тип</param>
        /// <param name="typeFields">Тип полей для коллекции</param>
        /// <returns></returns>
        public static ReadOnlyCollection<Object> GetStaticFields(this Type type, Type typeFields)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (typeFields is null)
                throw new ArgumentNullException(nameof(typeFields));

            var res = new List<object>();

            var flds = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var fld in flds.Where(x => x.FieldType == typeFields))
                res.Add(fld.GetValue(null));

            return new ReadOnlyCollection<object>(res);

        }

        /// <summary>
        /// Получение значений статических полей коллекцией
        /// </summary>
        /// <typeparam name="T">Тип полей для коллекции</typeparam>
        /// <param name="type">Просматреваемый тип</param>
        /// <returns></returns>
        public static ReadOnlyCollection<T> GetStaticFields<T>(this Type type) =>
            new ReadOnlyCollection<T>(type.GetStaticFields(typeof(T)).Cast<T>().ToArray());

        /// <summary>
        /// Установка значения свойства
        /// </summary>
        /// <param name="obj">экземпляр объекта</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <param name="value">значение</param>
        public static void SetPropertyValue(this object obj, String propertyName, Object value)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            obj.GetType().GetProperty(propertyName).SetValue(obj, value);
        }
        /// <summary>
        /// Создание экземпляра класса
        /// </summary>
        /// <param name="asm">Сборка с типом</param>
        /// <param name="className">Имя класса</param>
        /// <param name="args">Аргументы конструктора</param>
        /// <returns></returns>
        public static Object CreateInstance(this Assembly asm, String className, params object[] args)
        {
            if (asm is null)
                throw new ArgumentNullException(nameof(asm));
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException($"\"{nameof(className)}\" не может быть неопределенным или пустым.", nameof(className));

            var clType = asm.ExportedTypes
                .Single(x => x.Name == className || x.FullName == className);
            return Activator.CreateInstance(clType, args);
        }
        /// <summary>
        /// Получить экземпляр значения перечесления (enum)
        /// </summary>
        /// <param name="asm">сборка, содержащая тип</param>
        /// <param name="enumTypeName">Имя класа перечесления</param>
        /// <param name="valueName">Строковое значение перечесления</param>
        /// <returns></returns>
        public static Object GetEnumValue(this Assembly asm, String enumTypeName, String valueName)
        {
            if (asm is null)
                throw new ArgumentNullException(nameof(asm));
            if (string.IsNullOrWhiteSpace(enumTypeName))
                throw new ArgumentException($"\"{nameof(enumTypeName)}\" не может быть неопределенным или пустым.", nameof(enumTypeName));
            if (string.IsNullOrWhiteSpace(valueName))
                throw new ArgumentException($"\"{nameof(valueName)}\" не может быть неопределенным или пустым.", nameof(valueName));

            var rtType = asm.ExportedTypes
                .Single(x => x.Name == enumTypeName || x.FullName == enumTypeName);

            return Enum.Parse(rtType, valueName);
        }
        /// <summary>
        /// Вызов метода у объекта
        /// </summary>
        /// <param name="obj">экземпляр объекта</param>
        /// <param name="methodName">Имя метода</param>
        /// <param name="arg">Аргументы метода. По ним ищется метод</param>
        /// <returns>Результат выполения</returns>
        public static Object InvokeMethod(this Object obj, String methodName, params object[] arg)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException($"\"{nameof(methodName)}\" не может быть пустым или содержать только пробел.", nameof(methodName));

            var minfo = obj.GetType().GetMethod(methodName, arg.Select(x => x.GetType()).ToArray());
            return minfo.Invoke(obj, arg);
        }
        /// <summary>
        /// Вызов статического метода
        /// </summary>
        /// <param name="asm">Сборка, содержащая тип</param>
        /// <param name="className">Имя класса</param>
        /// <param name="methodName">Имя метода</param>
        /// <param name="args">Аргументы метода. По ним ищется метод</param>
        /// <returns>Результат выполения</returns>
        public static Object InvokeStaticMethod(this Assembly asm, String className, String methodName, params Object[] args)
        {
            if (asm is null)
                throw new ArgumentNullException(nameof(asm));

            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException($"\"{nameof(className)}\" не может быть пустым или содержать только пробел.", nameof(className));

            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException($"\"{nameof(methodName)}\" не может быть пустым или содержать только пробел.", nameof(methodName));

            var rtType = asm.ExportedTypes
                .Single(x => x.Name == className);

            var mi = rtType.GetMethod(methodName, args.Select(x => x.GetType()).ToArray());
            return mi.Invoke(null, args);
        }

        /// <summary>
        /// Получение генерик-типа из генерик-коллекции или массива. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns>Первый генерик-тип в перечеслении</returns>
        public static Type GetEnumeratedType(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            return type.GetElementType() ??
            (typeof(IEnumerable).IsAssignableFrom(type) ? type.GenericTypeArguments.FirstOrDefault() : null);
        }

        /// <summary>
        /// Реализует ли тип коллекцию через интерфейс <see cref="IList"/> (почти все коллекции, но в основном для класса <see cref="List{T}"/>)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsIList(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            return typeof(IList).IsAssignableFrom(type);
        }

        /// <summary>
        /// Распокавать тип из <see cref="Nullable{T}"/>, либо получить тип из коллекции <see cref="IList"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type UnWrapType(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            var res = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsIList())
                res = res.GetGenericArguments().Single();

            return res;
        }

        /// <summary>
        /// Является ли тип "простым" - типы значений (<see cref="Type.IsValueType"/>)/строки (<see cref="String"/>)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSimpleType(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            var res = Nullable.GetUnderlyingType(type) ?? type;

            return res.IsValueType || typeof(string) == res;
        }
    }
}
