using System.Reflection;
using Newtonsoft.Json.Linq;

#pragma warning disable CA1019 // Определите методы доступа для аргументов атрибута
#pragma warning disable CA1000 // Не объявляйте статические члены в универсальных типах
#pragma warning disable CA1003 // Используйте экземпляры обработчика универсальных событий

namespace Cav.Configuration;

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
/// <remarks>
/// Указание области хранения дайла для свойства
/// </remarks>
/// <param name="areaSetting"></param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ProgramSettingsAreaAttribute(Area areaSetting) : Attribute
{
    internal Area AreaSetting => areaSetting;
}

/// <summary>
/// Имя файла. Если не заданно -  typeof(T).FullName + ".json"
/// </summary>
/// <remarks>
/// Указание специфического имени файла хранения настроек
/// </remarks>
/// <param name="fileName"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ProgramSettingsFileAttribute(string fileName) : Attribute
{
    internal string FileName => fileName;
}

/// <summary>
/// Базовый класс для сохранения настроек
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ProgramSettingsBase<T> : IDisposable
    where T : ProgramSettingsBase<T>, new()
{
    /// <summary>
    /// Событие, возникающее при перезагрузке данных. Также вызывается при первичной загрузке.
    /// </summary>
    public event Action<ProgramSettingsBase<T>>? ReloadEvent;

    private static Lazy<T> instance = new(initInstasnce, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Получение объекта настроек
    /// </summary>
    public static T? Instance => instance.Value;
    private static T initInstasnce()
    {
        var instance = Activator.CreateInstance<T>();

        var filename = typeof(T).GetCustomAttribute<ProgramSettingsFileAttribute>()?.FileName!;

        if (filename.IsNullOrWhiteSpace())
            filename = typeof(T).FullName + ".json";

        filename = filename.ReplaceInvalidPathChars()!;

        instance.fileNameApp = Path.Combine(Path.GetDirectoryName(typeof(T).Assembly.Location)!, filename);
        instance.fileNameUserRoaming = Path.Combine(DomainContext.AppDataUserStorageRoaming, filename);
        instance.fileNameUserLocal = Path.Combine(DomainContext.AppDataUserStorageLocal, filename);
        instance.fileNameAppCommon = Path.Combine(DomainContext.AppDataCommonStorage, filename);
        instance.Reload();

        return instance;
    }

    private string? fileNameApp;
    private string? fileNameUserRoaming;
    private string? fileNameUserLocal;
    private string? fileNameAppCommon;

    private ReaderWriterLockSlim loker = new();

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
            {
                pinfo.SetValue(this, pinfo.PropertyType.GetDefault());

                if (pinfo.PropertyType.IsClass && pinfo.PropertyType != typeof(string))
                    pinfo.SetValue(this, Activator.CreateInstance(pinfo.PropertyType));
            }

            var settingsFiles =
                new[] { fileNameApp, fileNameAppCommon, fileNameUserRoaming, fileNameUserLocal }
                .Where(File.Exists)
                .ToList();

            if (!settingsFiles.Any())
                return;

            var joS = JToken.Parse(File.ReadAllText(settingsFiles.First()!));

            var targetJson = new JObject();

            foreach (var jo in settingsFiles
                .SelectMany(x => JObject.Parse(File.ReadAllText(x!)).Children())
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

            var proxyObj = targetJson.ToString().JsonDeserealize<T?>();

            foreach (var pi in prinfs)
            {
                var prpVal = pi.GetValue(proxyObj);

                if (prpVal == null && pi.PropertyType.IsClass && pi.PropertyType != typeof(string))
                    prpVal = Activator.CreateInstance(pi.PropertyType);

                pi.SetValue(this, prpVal);
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
                loker.ExitWriteLock();
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

            var jsInstSetting = instance.Value.JsonSerialize()!;

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

                File.WriteAllText(setFile.File!, jOSets.ToString());
            }
        }
        finally
        {
            loker.ExitWriteLock();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed)
            return;

        loker?.Dispose();

        isDisposed = true;
    }
    private bool isDisposed;

    /// <summary>
    /// 
    /// </summary>
    ~ProgramSettingsBase()
    {
        Dispose(false);
    }
}