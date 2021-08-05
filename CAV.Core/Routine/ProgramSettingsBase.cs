using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

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
        /// <param name="AreaSetting"></param>
        public ProgramSettingsAreaAttribute(Area AreaSetting)
        {
            this.Value = AreaSetting;
        }
        internal Area Value { get; private set; }
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
        /// <param name="FileName"></param>
        public ProgramSettingsFileAttribute(String FileName)
        {
            this.FileName = FileName;
        }
        internal String FileName { get; private set; }
    }

    /// <summary>
    /// Базовый класс для сохранения настроек
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Реорганизация структуры хранения в файлах. В следующей версии будет несовместимо.")]
    public abstract class ProgramSettingsBase<T> where T : ProgramSettingsBase<T>, new()
    {
        /// <summary>
        /// Событие, возникающее при перезагрузке данных. Также вызывается при первичной загрузке.
        /// </summary>
        public event Action<ProgramSettingsBase<T>> ReloadEvent;
        /// <summary>
        /// Создание экземпляра объекта настроек
        /// </summary>
        protected ProgramSettingsBase()
        {
            if (internalCreate)
                return;
            throw new TypeAccessException("Для получения экземпляра воспользуйтесь свойством Instance");
        }

        private static T _instance = null;
        private static Boolean internalCreate = false;
        private static Object lockObj = new object();
        /// <summary>
        /// Получение объекта настроек
        /// </summary>
        public static T Instance
        {
            get
            {

                if (_instance != null)
                    return _instance;

                lock (lockObj)
                {
                    if (_instance == null)
                    {
                        internalCreate = true;

                        _instance = (T)Activator.CreateInstance(typeof(T));

                        internalCreate = false;

                        String filename = (typeof(T).GetCustomAttribute<ProgramSettingsFileAttribute>())?.FileName;

                        if (filename.IsNullOrWhiteSpace())
                            filename = typeof(T).FullName + ".json";

                        filename = filename.ReplaceInvalidPathChars();

                        _instance.fileNameApp = Path.Combine(Path.GetDirectoryName(typeof(T).Assembly.Location), filename);
                        _instance.fileNameUserRoaming = Path.Combine(DomainContext.AppDataUserStorageRoaming, filename);
                        _instance.fileNameUserLocal = Path.Combine(DomainContext.AppDataUserStorageLocal, filename);
                        _instance.fileNameAppCommon = Path.Combine(DomainContext.AppDataCommonStorage, filename);
                        _instance.Reload();
                    }
                }

                return _instance;
            }
        }

        private String fileNameApp = null;
        private String fileNameUserRoaming = null;
        private String fileNameUserLocal = null;
        private String fileNameAppCommon = null;

        private void fromJsonDeserialize(String fileName, PropertyInfo[] prinfs)
        {
            if (!prinfs.Any())
                return;

            if (!File.Exists(fileName))
                return;

            var filebody = File.ReadAllText(fileName);

            Dictionary<String, String> pvl = null;

            try
            {
                pvl = fileName.JsonDeserealizeFromFile<Dictionary<String, String>>();
            }
            catch
            {
                pvl = filebody.JSONDeserialize<Dictionary<String, String>>();
            }

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

                var val = pv.Value;

                object targetObj = null;

                try
                {
                    targetObj = val.JsonDeserealize(pi.PropertyType);
                }
                catch
                {
                    targetObj = val.JSONDeserialize(pi.PropertyType);
                }


                pi.SetValue(this, targetObj);
            }
        }

        /// <summary>
        /// Перезагрузить настройки
        /// </summary>
        public void Reload()
        {
            try
            {
                lock (this)
                {
                    PropertyInfo[] prinfs = this.GetType().GetProperties();

                    foreach (var pinfo in prinfs)
                        pinfo.SetValue(this, pinfo.PropertyType.GetDefault());

                    var settingsFiles =
                        new[] { fileNameApp, fileNameAppCommon, fileNameUserRoaming, fileNameUserLocal }
                        .Where(x => File.Exists(x))
                        .ToList();

                    if (!settingsFiles.Any())
                        return;

                    var joS = JContainer.Parse(File.ReadAllText(settingsFiles.First()));

                    if (joS.Type == JTokenType.Array)
                    {
                        // Если внутри массив - значит json старого формата.
                        fromJsonDeserialize(fileNameApp,
                            prinfs
                            .Where(pinfo =>
                                pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() != null && pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().Value == Area.App
                                ).ToArray()
                            );

                        fromJsonDeserialize(fileNameAppCommon,
                            prinfs
                            .Where(pinfo =>
                                pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() != null && pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().Value == Area.CommonApp
                                ).ToArray()
                            );

                        fromJsonDeserialize(fileNameUserRoaming,
                            prinfs
                            .Where(pinfo =>
                                pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() == null || pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().Value == Area.UserRoaming
                                ).ToArray()
                            );

                        fromJsonDeserialize(fileNameUserLocal,
                        prinfs
                        .Where(pinfo =>
                                pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() == null || pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().Value == Area.UserLocal
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

                        internalCreate = true;
                        var proxyObj = targetJson.ToString().JsonDeserealize<T>();
                        internalCreate = false;

                        foreach (var pi in prinfs)
                            pi.SetValue(this, pi.GetValue(proxyObj));
                    }
                }
            }
            finally
            {
                ReloadEvent?.Invoke(this);
            }
        }
        /// <summary>
        /// Сохранить настройки
        /// </summary>
        public void Save()
        {
            lock (this)
            {

                var settingsFiles = new[] {
                    new { Area = Area.App, File =  fileNameApp } ,
                    new { Area = Area.CommonApp, File =  fileNameAppCommon} ,
                    new { Area = Area.UserRoaming, File =  fileNameUserRoaming} ,
                    new { Area = Area.UserLocal, File =  fileNameUserLocal } };

                var allProps = this
                    .GetType()
                    .GetProperties()
                    .Select(x => new { PropertyName = x.Name, Area = x.GetCustomAttribute<ProgramSettingsAreaAttribute>()?.Value ?? Area.UserLocal })
                    .ToList();

                var jsInstSetting = _instance.JsonSerialize();

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
        }
    }
}