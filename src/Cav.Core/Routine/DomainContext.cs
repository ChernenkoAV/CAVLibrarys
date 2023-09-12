using System.Diagnostics;
using System.Reflection;

namespace Cav;

/// <summary>
/// Домен приложения.
/// </summary>
public static class DomainContext
{
    /// <summary>
    /// Путь в AppData для текущего пользователя текущего приложения(перемещаемый)(%APPDATA%\"NameEntryAssembly") (Если отсутствует - то он создается...)
    /// </summary>
    public static string AppDataUserStorageRoaming
    {
        get
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            path = Path.Combine(path, NameEntryAssembly);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Путь в AppData для текущего пользователя текущего приложения(не перемещаемый)(%LOCALAPPDATA%\"NameEntryAssembly") (Если отсутствует - то он создается...)
    /// </summary>
    public static string AppDataUserStorageLocal
    {
        get
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            path = Path.Combine(path, NameEntryAssembly);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Путь в AppData для приложения(%PROGRAMDATA%\"NameEntryAssembly") (Если отсутствует - то он создается...)
    /// </summary>
    public static string AppDataCommonStorage
    {
        get
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            path = Path.Combine(path, NameEntryAssembly);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Путь к папке в %Documents%\"NameEntryAssembly". Если отсутствует - создается
    /// </summary>
    public static string DocumentsPath
    {
        get
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            path = Path.Combine(path, NameEntryAssembly);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Путь к временной папке в %Temp%\"NameEntryAssembly" пользователя. Если отсутствует - создается
    /// </summary>
    public static string TempPathUser
    {
        get
        {
            var tempPath = Path.Combine(Path.GetTempPath(), NameEntryAssembly);
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }

    /// <summary>
    /// Путь к временной папке в %Temp%\"NameEntryAssembly" в системе. Если отсутствует - создается
    /// </summary>
    public static string TempPath
    {
        get
        {
            var tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine)!, NameEntryAssembly);
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }

    /// <summary>
    /// Имя сборки, из которого запущенно приложение (имя exe файла) (ТОЛЬКО ЕСЛИ ЭТО НЕ COM!!!)
    /// При работе в IIS бесмысленно, ибо там всегда процесс w3c. вроде. Какой-то один, короче...
    /// </summary>
    public static string NameEntryAssembly => Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);

    /// <summary>Запуск приложения с администраторскими правами</summary>
    /// <param name="fileName">Файл приложения для запуска</param>
    /// <param name="arguments">аргументы приложения</param>
    public static Process RunAsAdmin(string fileName, string? arguments = null)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas" // здесь вся соль
        };

        return Process.Start(processInfo)!;
    }
}
