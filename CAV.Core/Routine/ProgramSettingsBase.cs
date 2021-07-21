using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
    public abstract class ProgramSettingsBase<T> where T : ProgramSettingsBase<T>
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

                        String filename = (typeof(T).GetCustomAttribute<ProgramSettingsFileAttribute>() ?? new ProgramSettingsFileAttribute(null)).FileName;

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

        private void toJsonSerialize(String fileName, Dictionary<String, String> props)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            if (!props.Any())
                return;

            File.WriteAllText(fileName, props.JsonSerialize());
        }
        /// <summary>
        /// Перезагрузить настройки
        /// </summary>
        public void Reload()
        {
            lock (this)
            {
                PropertyInfo[] prinfs = this.GetType().GetProperties();

                foreach (var pinfo in prinfs)
                    pinfo.SetValue(this, pinfo.PropertyType.GetDefault());

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

            if (ReloadEvent != null)
                ReloadEvent(this);
        }
        /// <summary>
        /// Сохранить настройки
        /// </summary>
        public void Save()
        {
            lock (this)
            {
                PropertyInfo[] prinfs = this.GetType().GetProperties();

                Dictionary<String, String> appVal = new Dictionary<String, String>();
                Dictionary<String, String> appCommonVal = new Dictionary<String, String>();
                Dictionary<String, String> userRoamingVal = new Dictionary<String, String>();
                Dictionary<String, String> userLocalVal = new Dictionary<String, String>();


                foreach (var pinfo in prinfs)
                {
                    Object val = pinfo.GetValue(this);

                    if (val == null)
                        continue;

                    var psatr = pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() ?? new ProgramSettingsAreaAttribute(Area.UserLocal);

                    var jOVal = val.JsonSerialize();
                    switch (psatr.Value)
                    {
                        case Area.UserLocal:
                            userLocalVal.Add(pinfo.Name, jOVal);
                            break;
                        case Area.UserRoaming:
                            userRoamingVal.Add(pinfo.Name, jOVal);
                            break;
                        case Area.App:
                            appVal.Add(pinfo.Name, jOVal);
                            break;
                        case Area.CommonApp:
                            appCommonVal.Add(pinfo.Name, jOVal);
                            break;
                        default:
                            throw new ArgumentException("ProgramSettinsUserAreaAttribute.Value");
                    }
                }

                toJsonSerialize(fileNameUserRoaming, userRoamingVal);
                toJsonSerialize(fileNameUserLocal, userLocalVal);
                toJsonSerialize(fileNameAppCommon, appCommonVal);
                toJsonSerialize(fileNameApp, appVal);
            }
        }
    }
}