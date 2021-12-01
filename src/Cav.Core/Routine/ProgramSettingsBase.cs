﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json.Linq;

#pragma warning disable CA1019 // Определите методы доступа для аргументов атрибута
#pragma warning disable CA1000 // Не объявляйте статические члены в универсальных типах
#pragma warning disable CA1003 // Используйте экземпляры обработчика универсальных событий

namespace Cav.Configuration
{
    /// <summary>
    /// Область сохранения настрек
    /// </summary>
    public enum Area
    {
        /// <summary>
        /// Для пользователя (не перемещаемый)
        /// </summary>
        UserLocal,
        /// <summary>
        /// Для пользователя (перемещаемый)
        /// </summary>
        UserRoaming,
        /// <summary>
        /// Для приложения (В папке сборки)
        /// </summary>
        App,
        /// <summary>
        /// Общее хранилице для всех пользователей
        /// </summary>
        CommonApp
    }

    /// <summary>
    /// Обрасть сохранения для свойства. Если не задано, то - <see cref="Area.UserLocal"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ProgramSettingsAreaAttribute : Attribute
    {
        /// <summary>
        /// Указание области хранения дайла для свойства
        /// </summary>
        /// <param name="areaSetting"></param>
        public ProgramSettingsAreaAttribute(Area areaSetting) => AreaSetting = areaSetting;
        internal Area AreaSetting { get; private set; }
    }

    /// <summary>
    /// Имя файла. Если не заданно -  typeof(T).FullName + ".json"
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ProgramSettingsFileAttribute : Attribute
    {
        /// <summary>
        /// Указание специфического имени файла хранения настроек
        /// </summary>
        /// <param name="fileName"></param>
        public ProgramSettingsFileAttribute(String fileName) => FileName = fileName;
        internal String FileName { get; private set; }
    }

    /// <summary>
    /// Базовый класс для сохранения настроек
    /// </summary>
    /// <typeparam name="T"></typeparam>
#pragma warning disable CA1063 // Правильно реализуйте IDisposable
    public abstract class ProgramSettingsBase<T> : IDisposable
#pragma warning restore CA1063 // Правильно реализуйте IDisposable
        where T : ProgramSettingsBase<T>, new()
    {
        /// <summary>
        /// Событие, возникающее при перезагрузке данных. Также вызывается при первичной загрузке.
        /// </summary>
        public event Action<ProgramSettingsBase<T>> ReloadEvent;

        /// <summary>
        /// Ограничение конструктора
        /// </summary>
        internal ProgramSettingsBase() { }

        private static Lazy<T> instance = new Lazy<T>(initInstasnce, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Получение объекта настроек
        /// </summary>
        public static T Instance => instance.Value;
        private static T initInstasnce()
        {
            var instance = (T)Activator.CreateInstance(typeof(T));

            var filename = typeof(T).GetCustomAttribute<ProgramSettingsFileAttribute>()?.FileName;

            if (filename.IsNullOrWhiteSpace())
                filename = typeof(T).FullName + ".json";

            filename = filename.ReplaceInvalidPathChars();

            instance.fileNameApp = Path.Combine(Path.GetDirectoryName(typeof(T).Assembly.Location), filename);
            instance.fileNameUserRoaming = Path.Combine(DomainContext.AppDataUserStorageRoaming, filename);
            instance.fileNameUserLocal = Path.Combine(DomainContext.AppDataUserStorageLocal, filename);
            instance.fileNameAppCommon = Path.Combine(DomainContext.AppDataCommonStorage, filename);
            instance.Reload();

            return instance;
        }

        private String fileNameApp;
        private String fileNameUserRoaming;
        private String fileNameUserLocal;
        private String fileNameAppCommon;

        private ReaderWriterLockSlim loker = new ReaderWriterLockSlim();

        private void fromJsonDeserialize(String fileName, PropertyInfo[] prinfs)
        {
            if (!prinfs.Any())
                return;

            if (!File.Exists(fileName))
                return;

            var filebody = File.ReadAllText(fileName);

            var pvl = fileName.JsonDeserealizeFromFile<Dictionary<String, String>>();

            if (pvl == null)
            {
                File.Delete(fileName);
                return;
            }

            foreach (var pv in pvl)
            {
                var pi = prinfs.FirstOrDefault(x => x.Name == pv.Key);
                if (pi == null)
                    continue;

                pi.SetValue(this, pv.Value.JsonDeserealize(pi.PropertyType));
            }
        }

        /// <summary>
        /// Перезагрузить настройки
        /// </summary>
        public void Reload()
        {
            loker.EnterWriteLock();

            try
            {
                var prinfs = GetType().GetProperties();

                foreach (var pinfo in prinfs)
                    pinfo.SetValue(this, pinfo.PropertyType.GetDefault());

                var settingsFiles =
                    new[] { fileNameApp, fileNameAppCommon, fileNameUserRoaming, fileNameUserLocal }
                    .Where(x => File.Exists(x))
                    .ToList();

                if (!settingsFiles.Any())
                    return;

                var joS = JToken.Parse(File.ReadAllText(settingsFiles.First()));

                if (joS.Type == JTokenType.Array)
                {
                    // Если внутри массив - значит json старого формата.
                    fromJsonDeserialize(fileNameApp,
                        prinfs
                        .Where(pinfo =>
                            pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() != null && pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().AreaSetting == Area.App
                            ).ToArray()
                        );

                    fromJsonDeserialize(fileNameAppCommon,
                        prinfs
                        .Where(pinfo =>
                            pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() != null && pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().AreaSetting == Area.CommonApp
                            ).ToArray()
                        );

                    fromJsonDeserialize(fileNameUserRoaming,
                        prinfs
                        .Where(pinfo =>
                            pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() == null || pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().AreaSetting == Area.UserRoaming
                            ).ToArray()
                        );

                    fromJsonDeserialize(fileNameUserLocal,
                    prinfs
                    .Where(pinfo =>
                            pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() == null || pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().AreaSetting == Area.UserLocal
                        ).ToArray()
                    );
                }
                else
                {
                    var targetJson = new JObject();

                    foreach (var jo in settingsFiles
                        .SelectMany(x => JObject.Parse(File.ReadAllText(x)).Children())
                        .Select(x => (JProperty)x)
                        .GroupBy(x => x.Name)
                        .ToArray())
                    {
                        if (jo.Count() == 1)
                        {
                            targetJson.Add(jo.Single());
                            continue;
                        }

                        var prop = prinfs.FirstOrDefault(x => x.Name == jo.Key);

                        if (prop == null)
                            continue;

                        foreach (var joitem in jo)
                        {
                            try
                            {
                                joitem.Value.ToString().JsonDeserealize(prop.PropertyType);
                                targetJson.Add(joitem);
                                break;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }

                    var proxyObj = targetJson.ToString().JsonDeserealize<T>();

                    foreach (var pi in prinfs)
                        pi.SetValue(this, pi.GetValue(proxyObj));
                }
            }
            finally
            {
                try
                {
                    ReloadEvent?.Invoke(this);
                }
                finally
                {
                    loker.ExitReadLock();
                }
            }
        }
        /// <summary>
        /// Сохранить настройки
        /// </summary>
        public void Save()
        {
            loker.EnterWriteLock();

            try
            {

                var settingsFiles = new[] {
                    new { Area = Area.App, File =  fileNameApp } ,
                    new { Area = Area.CommonApp, File =  fileNameAppCommon} ,
                    new { Area = Area.UserRoaming, File =  fileNameUserRoaming} ,
                    new { Area = Area.UserLocal, File =  fileNameUserLocal } };

                var allProps = GetType()
                    .GetProperties()
                    .Select(x => new { PropertyName = x.Name, Area = x.GetCustomAttribute<ProgramSettingsAreaAttribute>()?.AreaSetting ?? Area.UserLocal })
                    .ToList();

                var jsInstSetting = instance.JsonSerialize();

                foreach (var setFile in settingsFiles)
                {
                    if (File.Exists(setFile.File))
                        File.Delete(setFile.File);

                    var jOSets = JObject.Parse(jsInstSetting);

                    var curProps = allProps.Where(x => x.Area == setFile.Area).Select(x => x.PropertyName).ToList();

                    foreach (var cldItem in jOSets.Children().ToArray())
                    {
                        if (!curProps.Contains(cldItem.Path))
                            jOSets.Remove(cldItem.Path);
                    }

                    if (!jOSets.Children().Any())
                        continue;

                    File.WriteAllText(setFile.File, jOSets.ToString());
                }
            }
            finally
            {
                loker.ExitWriteLock();
            }
        }

#pragma warning disable CA1063 // Правильно реализуйте IDisposable
#pragma warning disable CA1816 // Методы Dispose должны вызывать SuppressFinalize
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        public void Dispose() => loker?.Dispose();
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
#pragma warning restore CA1816 // Методы Dispose должны вызывать SuppressFinalize
#pragma warning restore CA1063 // Правильно реализуйте IDisposable
    }
}