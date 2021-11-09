using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using Cav.ReflectHelpers;

namespace Cav
{
    /// <summary>
    /// Утилиты, которые не пошли расширениями
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Удаление папки и всего, что в ней. Включая файлы с атрибутом ReadOnly
        /// </summary>
        /// <param name="path">Полный путь для удаления</param>
        public static void DeleteDirectory(String path)
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
        /// Посыл информации о в гугловую форму.
        /// </summary>
        /// <param name="googleForm">url формы вида "https://docs.google.com/forms/d/ХэшФормы/formResponse"</param>
        /// <param name="paramValueForm">Словарь с параметрами полей формы вида "имяполя","значение"</param>
        [Obsolete("Будет удалено", true)]
        public static void SendInfo(
            String googleForm,
            Dictionary<String, String> paramValueForm) => throw new NotImplementedException();

        /// <summary>   Флаг первого запуска приложения ClickOnce </summary>
        /// <returns>true - ели приложение ClickOnce и выполняется впервые. в остальных случаях - false </returns>
        [Obsolete("Будет перенесено")]
        public static Boolean ClickOnceFirstRun()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return false;
            var curdep = ApplicationDeployment.CurrentDeployment;
            if (curdep == null)
                return false;
            return curdep.IsFirstRun;
        }

        /// <summary>
        /// Неглубокое клонирование экземпляра класса в тип-наследник.
        /// </summary>        
        /// <typeparam name="TAncestorType">Тип предка</typeparam>
        /// <typeparam name="THeritorType">Тип наследника</typeparam>
        /// <param name="obj">Исходный объект</param>
        /// <returns></returns>
        [Obsolete("Будет удалено. Используйте копирование через json-сериализацию/десериализацию Copy<>()")]
        public static THeritorType CopyTo<TAncestorType, THeritorType>(TAncestorType obj)
            where TAncestorType : class, new()
            where THeritorType : class, TAncestorType, new()
        {
            if (obj == null)
                return null;

            return (THeritorType)obj.CopyTo(typeof(THeritorType));
        }

        /// <summary>
        /// Неглубокое клонирование экземпляра класса в тип-наследник.
        /// </summary>
        /// <typeparam name="TAncestorType">Тип предка</typeparam>
        /// <param name="obj">Исходный объект</param>
        /// <param name="heritorType">Результирующий тип</param>
        /// <returns></returns>
        [Obsolete("Будет удалено. Используйте копирование через json-сериализацию/десериализацию Copy<>()")]
        public static object CopyTo<TAncestorType>(this TAncestorType obj, Type heritorType)
            where TAncestorType : class, new()
        {
            if (heritorType == null)
                throw new ArgumentNullException(nameof(heritorType));

            var ancestorType = typeof(TAncestorType);

            if (!ancestorType.IsAssignableFrom(heritorType))
                throw new InvalidCastException($"{heritorType.FullName} не является наследником {ancestorType.FullName}");

            var res = Activator.CreateInstance(heritorType);

            foreach (var ancestorProperty in ancestorType.GetProperties())
                res.SetPropertyValue(ancestorProperty.Name, obj.GetPropertyValue(ancestorProperty.Name));

            return res;
        }
    }
}
