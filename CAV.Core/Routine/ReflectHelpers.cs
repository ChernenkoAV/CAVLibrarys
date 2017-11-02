using System;
using System.Linq;
using System.Reflection;

namespace Cav.ReflectHelpers
{
    /// <summary>
    /// Расширения упрощения вызовов рефлексии
    /// </summary>
    public static class ReflectHelpers
    {
        /// <summary>
        /// Получения значения свойства у объекта
        /// </summary>
        /// <param name="obj">экземпляр объекта</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <returns></returns>
        public static Object GetPropertyValue(this object obj, String propertyName)
        {
            return obj.GetType().GetProperty(propertyName).
#if NET40
            GetValue(obj, null);
#else
            GetValue(obj);
#endif
        }
        /// <summary>
        /// Получение значения статического свойства
        /// </summary>
        /// <param name="asm">Сборка, сожержащая тип</param>
        /// <param name="className">Имя класса</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <returns></returns>
        public static Object GetStaticPropertyValue(this Assembly asm, String className, String propertyName)
        {
            return asm.
#if NET40
            GetExportedTypes()
#else
            ExportedTypes
#endif            
            .Single(x => x.Name == className || x.FullName == className).GetProperty(propertyName).
#if NET40
            GetValue(null, null);
#else
            GetValue(null);
#endif            
        }
        /// <summary>
        /// Установка значения свойства
        /// </summary>
        /// <param name="obj">экземпляр объекта</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <param name="value">значение</param>
        public static void SetPropertyValue(this object obj, String propertyName, Object value)
        {
            obj.GetType().GetProperty(propertyName).
#if NET40
            SetValue(obj, value, null);
#else
            SetValue(obj, value);
#endif
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
            Type clType = asm.
#if NET40
            GetExportedTypes()
#else
            ExportedTypes
#endif            
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
            Type rtType = asm.
#if NET40
            GetExportedTypes()
#else
            ExportedTypes
#endif            
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
            if (arg == null)
                arg = new object[0];
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
            Type rtType = asm.
#if NET40
            GetExportedTypes()
#else
            ExportedTypes
#endif
            .Single(x => x.Name == className);

            var mi = rtType.GetMethod(methodName, args.Select(x => x.GetType()).ToArray());
            return mi.Invoke(null, args);
        }
    }
}
