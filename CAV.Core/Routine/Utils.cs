﻿using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
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
        /// <param name="Path">Полный путь для удаления</param>
        public static void DeleteDirectory(String Path)
        {
            if (!Directory.Exists(Path))
                return;

            var directory = new DirectoryInfo(Path) { Attributes = FileAttributes.Normal };

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }

        /// <summary>
        /// Посыл информации о в гугловую форму.
        /// </summary>
        /// <param name="GoogleForm">url формы вида "https://docs.google.com/forms/d/ХэшФормы/formResponse"</param>
        /// <param name="ParamValueForm">Словарь с параметрами полей формы вида "имяполя","значение"</param>
        public static void SendInfo(
            String GoogleForm,
            Dictionary<String, String> ParamValueForm)
        {
            if (ParamValueForm == null || ParamValueForm.Count == 0)
                throw new ArgumentNullException("Словарь полей-значений не задан или пуст");

            if (GoogleForm.IsNullOrWhiteSpace())
                throw new ArgumentNullException("Не указанна ссылка на форму");

            var request = WebRequest.Create(GoogleForm);

            String postData = ParamValueForm.Select(x => x.Key + "=" + x.Value).ToArray().JoinValuesToString("&");
            postData = HttpUtility.UrlPathEncode(postData);
            var data = Encoding.UTF8.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var srt = request.GetRequestStream())
                srt.Write(data, 0, data.Length);

            try
            {
                request.GetResponse();
            }
            catch { }
        }

        /// <summary>   Флаг первого запуска приложения ClickOnce </summary>
        /// <returns>true - ели приложение ClickOnce и выполняется впервые. в остальных случаях - false </returns>
        public static Boolean ClickOnceFirstRun()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return false;
            ApplicationDeployment curdep = ApplicationDeployment.CurrentDeployment;
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
        public static object CopyTo<TAncestorType>(this TAncestorType obj, Type heritorType)
            where TAncestorType : class, new()
        {
            if (heritorType == null)
                throw new ArgumentNullException(nameof(heritorType));

            var ancestorType = typeof(TAncestorType);

            if (!ancestorType.IsAssignableFrom(heritorType))
                throw new InvalidCastException($"{heritorType.FullName} не является наследником {ancestorType.FullName}");

            object res = Activator.CreateInstance(heritorType);

            foreach (var ancestorProperty in ancestorType.GetProperties())
                res.SetPropertyValue(ancestorProperty.Name, obj.GetPropertyValue(ancestorProperty.Name));

            return res;
        }

    }
}
