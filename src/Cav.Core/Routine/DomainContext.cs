using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Cav
{
    /// <summary>
    /// Домен приложения.
    /// </summary>
    public static class DomainContext
    {
        #region Для запуска программы в одном экземпляре (На базе поиска процесса)

        [DllImport("User32.dll")]
#pragma warning disable CA5392 // Используйте атрибут DefaultDllImportSearchPaths для P/Invokes.
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
#pragma warning restore CA5392 // Используйте атрибут DefaultDllImportSearchPaths для P/Invokes.
#pragma warning disable IDE1006 // Стили именования
        private const int WS_SHOWNORMAL = 1;
#pragma warning restore IDE1006 // Стили именования

        /// <summary>
        /// Поиск процесса запущенного приложения
        /// </summary>
        /// <param name="setForeground">Если приложение имеет окна, то вывести окно вперед</param>
        /// <returns>true - приложение найдено(можно не запускать вторую копию), false - приложение не найдено.</returns>
        [Obsolete("Будет перенесено")]
        public static Boolean FindStartedProgram(Boolean setForeground = true)
        {
            var current = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(current.ProcessName);
            Process findedProcess = null;
            //Loop through the running processes in with the same name
            foreach (var process in processes)
            {
                if (process.Id == current.Id)
                    continue;
                //Ignore the current process

                //Make sure that the process is running from the exe file.
                if (process.MainModule.FileName == current.MainModule.FileName)
                {
                    //Return the other process instance.
                    findedProcess = process;
                    break;
                }
            }

            if (findedProcess == null)
                return false;

            if (setForeground && findedProcess.MainWindowHandle != IntPtr.Zero)
            {
                //Make sure the window is not minimized or maximized
                ShowWindowAsync(findedProcess.MainWindowHandle, WS_SHOWNORMAL);
                //Set the real intance to foreground window
                SetForegroundWindow(findedProcess.MainWindowHandle);
            }

            return true;
        }

        #endregion

        #region Environment

        /// <summary>
        /// Получение параметров командной строки приложения ClickOnce
        /// </summary>
        [Obsolete("Будет перенесено")]
        public static IEnumerable<string> ProgramArguments
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return Array.Empty<string>();

                var adata = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (adata == null)
                    return Array.Empty<string>();

                var larg = adata.ToList();

                if (larg.Count == 0)
                    return Array.Empty<string>();

                //Другой нюанс заключается в том, что местоположение файла передается в формате URI, 
                //как в file:///c:\MyApp\MyFile.testDoc. 
                //Это значит, что для получения действительного пути к файлу и очистке от защищенных пробелов (которые в URI транслируются в символ %20) понадобится следующий код:
                for (var i = 0; i < larg.Count - 1; i++)
                {

                    if (!larg[i].StartsWith("file:///"))
                        continue;
                    var fileUri = new Uri(larg[i]);
                    larg[i] = Uri.UnescapeDataString(fileUri.AbsolutePath);
                    //После этого можно проверить существование файла и открыть его, как обычно.
                }

                return larg.ToArray();
            }
        }

        /// <summary>
        /// Путь в AppData для текущего пользователя текущего приложения(перемещаемый)(%APPDATA%\"NameEntryAssembly") (Если отсутствует - то он создается...)
        /// </summary>
        public static String AppDataUserStorageRoaming
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
        public static String AppDataUserStorageLocal
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
        public static String AppDataCommonStorage
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
        public static String DocumentsPath
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
        public static String TempPathUser
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
        public static String TempPath
        {
            get
            {
                var tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine), NameEntryAssembly);
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);
                return tempPath;
            }
        }

        /// <summary>
        /// Имя сборки, из которого запущенно приложение (имя exe файла) (ТОЛЬКО ЕСЛИ ЭТО НЕ COM!!!)
        /// При работе в IIS бесмысленно, ибо там всегда процесс w3c. вроде. Какой-то один, короче...
        /// </summary>
        public static String NameEntryAssembly => Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// Текущая версия приложения.
        /// </summary>
        /// <returns>ClickOnce или версию AssemblyVersion исполняемого файла</returns>
        [Obsolete("Будет перенесено")]
        public static Version CurrentVersion
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    var curdep = ApplicationDeployment.CurrentDeployment;
                    if (curdep != null)
                        return curdep.CurrentVersion;
                }

                return Assembly.GetEntryAssembly().GetName().Version;
            }
        }

        /// <summary>
        /// Имя приложения
        /// </summary>
        [Obsolete("Будет перенесено")]
        public static String ApplicationName
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return null;
                var curdep = ApplicationDeployment.CurrentDeployment;
                if (curdep == null)
                    return null;
                return curdep.UpdatedApplicationFullName;
            }
        }

        #endregion
    }
}
