using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Cav.WinForms
{
    /// <summary>
    /// Утилиты для приложений Windows
    /// </summary>
    //[SupportedOSPlatform("windows")]
    public static class WinAppUtils
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

        /// <summary>
        /// Текущая версия приложения.
        /// </summary>
        /// <returns>ClickOnce или версию AssemblyVersion исполняемого файла</returns>
        public static Version CurrentVersion =>
            ApplicationDeployment.IsNetworkDeployed
                ? (ApplicationDeployment.CurrentDeployment?.CurrentVersion)
                : Assembly.GetEntryAssembly().GetName().Version;

        /// <summary>
        /// Имя приложения (ClickOnce)
        /// </summary>
        public static String ApplicationName => ApplicationDeployment.CurrentDeployment?.UpdatedApplicationFullName;

        /// <summary>
        /// Получение параметров командной строки приложения ClickOnce
        /// </summary>
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

        /// <summary>Флаг первого запуска приложения ClickOnce </summary>
        /// <returns>true - ели приложение ClickOnce и выполняется впервые. в остальных случаях - false </returns>
        public static Boolean ClickOnceFirstRun()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return false;
            var curdep = ApplicationDeployment.CurrentDeployment;
            return curdep != null && curdep.IsFirstRun;
        }

        /// <summary>
        /// Выбор сертификата(ов)
        /// </summary>
        /// <param name="singleCertificate">true - выбор одного сертификата(по умолчанию)</param>
        /// <param name="nameCertificate">Имя сертификата, по которому будет осуществлен поиск в хранилище</param>
        /// <returns>Коллекция сертификатов</returns>
        public static X509Certificate2Collection SelectCertificate(bool singleCertificate = true, String nameCertificate = null)
        {
            using (var store = new X509Store(StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                var scollection = String.IsNullOrEmpty(nameCertificate)
                    ? X509Certificate2UI.SelectFromCollection(
                        store.Certificates,
                        "Выбор сертификата",
                        "Выберите сертификат.",
                        singleCertificate ? X509SelectionFlag.SingleSelection : X509SelectionFlag.MultiSelection)
                    : store.Certificates.Find(
                        X509FindType.FindBySubjectName,
                        nameCertificate,
                        true);
                return scollection;
            }
        }
    }
}
