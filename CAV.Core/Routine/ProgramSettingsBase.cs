using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;

namespace Cav.Configuration
{
    /// <summary>
    /// Область сохранения настрек
    /// </summary>
    public enum Area
    {
        /// <summary>
        /// Для пользователя
        /// </summary>
        User,
        /// <summary>
        /// Для приложения (В папке сборки)
        /// </summary>
        App,
        /// <summary>
        /// Общее хранилице для всех пользователей
        /// </summary>
        CommonApp
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ProgramSettingsAreaAttribute : Attribute
    {
        public ProgramSettingsAreaAttribute(Area AreaSetting)
        {
            this.Value = AreaSetting;
        }
        internal Area Value { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ProgramSettingsFileAttribute : Attribute
    {
        public ProgramSettingsFileAttribute(String FileName)
        {
            this.FileName = FileName;
        }
        internal String FileName { get; private set; }
    }

    public abstract class ProgramSettingsBase<T> where T : ProgramSettingsBase<T>
    {
        protected ProgramSettingsBase() { }

        private static T _instance = null;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)Activator.CreateInstance(typeof(T));


#if NET40
                    String filename = ((ProgramSettingsFileAttribute)typeof(T).GetCustomAttributes(typeof(ProgramSettingsFileAttribute), false).FirstOrDefault() ?? new ProgramSettingsFileAttribute(null)).FileName;
#else
                    String filename = (typeof(T).GetCustomAttribute<ProgramSettingsFileAttribute>() ?? new ProgramSettingsFileAttribute(null)).FileName;
#endif
                    if (filename.IsNullOrWhiteSpace())
                        filename = typeof(T).FullName + ".json";

                    filename = filename.ReplaceInvalidPathChars();

                    _instance.fileNameApp = Path.Combine(Path.GetDirectoryName(typeof(T).Assembly.Location), filename);
                    _instance.fileNameUser = Path.Combine(DomainContext.AppDataUserStorage, filename);
                    _instance.fileNameAppCommon = Path.Combine(DomainContext.AppDataCommonStorage, filename);
                    _instance.Reload();
                }

                return _instance;
            }
        }

        private String fileNameApp = null;
        private String fileNameUser = null;
        private String fileNameAppCommon = null;

        private void FromJsonDeserialize(String fileName, PropertyInfo[] prinfs)
        {
            if (!prinfs.Any())
                return;

            if (!File.Exists(fileName))
                return;

            Dictionary<String, String> pvl = null;

            var jss = new JavaScriptSerializer();
            pvl = jss.Deserialize<Dictionary<String, String>>(File.ReadAllText(fileName));

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
#if NET40
                pi.SetValue(this, jss.Deserialize(pv.Value, pi.PropertyType), null);
#else
                pi.SetValue(this, jss.Deserialize(pv.Value, pi.PropertyType));
#endif
            }
        }

        private void ToJsonSerialize(String fileName, Dictionary<String, String> props)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            if (!props.Any())
                return;
            var jss = new JavaScriptSerializer();
            var jsonStr = jss.Serialize(props);

            File.WriteAllText(fileName, jsonStr);
        }

        public void Reload()
        {
            lock (this)
            {
                PropertyInfo[] prinfs = this.GetType().GetProperties();

                foreach (var pinfo in prinfs)
#if NET40
                    pinfo.SetValue(this, pinfo.PropertyType.GetDefault(), null);
#else
                    pinfo.SetValue(this, pinfo.PropertyType.GetDefault());
#endif


                FromJsonDeserialize(fileNameApp,
                    prinfs
                    .Where(pinfo =>
#if NET40
                        pinfo.GetCustomAttributes(typeof(ProgramSettingsAreaAttribute), false).FirstOrDefault() != null &&
                        ((ProgramSettingsAreaAttribute)pinfo.GetCustomAttributes(typeof(ProgramSettingsAreaAttribute), false).First()).Value == Area.App
#else
                        pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() != null && pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().Value == Area.App
#endif

                        ).ToArray()
                    );

                FromJsonDeserialize(fileNameAppCommon,
                    prinfs
                    .Where(pinfo =>
#if NET40
                        pinfo.GetCustomAttributes(typeof(ProgramSettingsAreaAttribute), false).FirstOrDefault() != null &&
                        ((ProgramSettingsAreaAttribute)pinfo.GetCustomAttributes(typeof(ProgramSettingsAreaAttribute), false).First()).Value == Area.CommonApp
#else
                        pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() != null && pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().Value == Area.CommonApp
#endif                        
                        ).ToArray()
                    );

                FromJsonDeserialize(fileNameUser,
                    prinfs
                    .Where(pinfo =>
#if NET40
                        pinfo.GetCustomAttributes(typeof(ProgramSettingsAreaAttribute), false).FirstOrDefault() == null ||
                        ((ProgramSettingsAreaAttribute)pinfo.GetCustomAttributes(typeof(ProgramSettingsAreaAttribute), false).First()).Value == Area.User
#else
                        pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() == null || pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>().Value == Area.User
#endif

                        ).ToArray()
                    );
            }
        }

        public void Save()
        {
            lock (this)
            {
                PropertyInfo[] prinfs = this.GetType().GetProperties();

                Dictionary<String, String> appVal = new Dictionary<String, String>();
                Dictionary<String, String> appCommonVal = new Dictionary<String, String>();
                Dictionary<String, String> userVal = new Dictionary<String, String>();

                var jss = new JavaScriptSerializer();

                foreach (var pinfo in prinfs)
                {
#if NET40
                    Object val = pinfo.GetValue(this, null);
#else
                    Object val = pinfo.GetValue(this);
#endif
                    if (val == null)
                        continue;

#if NET40
                    var psatr = (ProgramSettingsAreaAttribute)pinfo.GetCustomAttributes(typeof(ProgramSettingsAreaAttribute), false).FirstOrDefault() ?? new ProgramSettingsAreaAttribute(Area.User);
#else
                    var psatr = pinfo.GetCustomAttribute<ProgramSettingsAreaAttribute>() ?? new ProgramSettingsAreaAttribute(Area.User);
#endif

                    switch (psatr.Value)
                    {
                        case Area.User:
                            userVal.Add(pinfo.Name, jss.Serialize(val));
                            break;
                        case Area.App:
                            appVal.Add(pinfo.Name, jss.Serialize(val));
                            break;
                        case Area.CommonApp:
                            appCommonVal.Add(pinfo.Name, jss.Serialize(val));
                            break;
                        default:
                            throw new ArgumentException("ProgramSettinsUserAreaAttribute.Value");
                    }
                }

                ToJsonSerialize(fileNameUser, userVal);
                ToJsonSerialize(fileNameAppCommon, appCommonVal);
                ToJsonSerialize(fileNameApp, appVal);
            }
        }
    }
}