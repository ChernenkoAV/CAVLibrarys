using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Xml.Linq;

namespace Cav.WinService
{
    /// <summary>Менеджер управления виндовыми службами</summary>
    public static class Manager
    {
        /// <summary>Установка в качестве службы.</summary>
        /// <remarks>1. Сборка должна быть .Net-кая. Наверное. Системная обертка к Installutil.exe</remarks>
        /// <remarks>2. Служба будет установлена для файла по указанному пути.</remarks>        
        /// <param name="FileName">Файл для установки.</param>
        public static void InstallAsService(String FileName)
        {
            ManagedInstallerClass.InstallHelper(new[] { FileName });
        }

        /// <summary>Удаление сервиса</summary>
        /// <remarks>Сборка должна быть .Net-кая. Наверное. Системная обертка к Installutil.exe</remarks>
        /// <param name="FileName">Файл службы</param>
        public static void UninstallService(String FileName)
        {
            ManagedInstallerClass.InstallHelper(new[] { "/u", FileName });
        }

        /// <summary>Проверка существования службы по имени</summary>
        /// <param name="ServiceName">Имя службы</param>
        /// <returns>True - служба установлена</returns>
        public static Boolean ServiceExist(String ServiceName)
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);
        }

        /// <summary>Запуск службы</summary>        
        /// <exception cref="ArgumentOutOfRangeException">Если отсутствует служба с указаным именем.</exception>
        /// <param name="ServiceName">Имя службы.</param>
        public static void StartService(String ServiceName)
        {
            if (!ServiceExist(ServiceName))
                throw new ArgumentOutOfRangeException(String.Format("Указаная служба '{0}'не существует", ServiceName));

            ServiceController sc = new ServiceController(ServiceName);

            List<ServiceControllerStatus> stats = new List<ServiceControllerStatus>();
            stats.Add(ServiceControllerStatus.Running);
            stats.Add(ServiceControllerStatus.StartPending);
            stats.Add(ServiceControllerStatus.StopPending);
            stats.Add(ServiceControllerStatus.ContinuePending);
            stats.Add(ServiceControllerStatus.PausePending);

            if (!stats.Contains(sc.Status))
                sc.Start();
        }

        /// <summary>Останов службы</summary>        
        /// <exception cref="ArgumentOutOfRangeException">Если отсутствует служба с указаным именем.</exception>
        /// <exception cref="System.TimeoutException">Если служба не остановилась за указанный промежуток времени</exception>
        /// <param name="ServiceName">Имя службы.</param>
        /// <param name="WaitTimeout">Таймаут ожидания останова</param>
        public static void StopService(String ServiceName, TimeSpan? WaitTimeout = null)
        {
            if (!ServiceExist(ServiceName))
                throw new ArgumentOutOfRangeException("Указаная служба не существует");

            ServiceController sc = new ServiceController(ServiceName);
            if (sc.Status == ServiceControllerStatus.Stopped)
                return;

            sc.Stop();

            if (!WaitTimeout.HasValue)
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
            else
            {
                sc.WaitForStatus(ServiceControllerStatus.Stopped, WaitTimeout.Value);
                if (sc.Status != ServiceControllerStatus.Stopped)
                    throw new System.TimeoutException("Служба не остановилась за указанный промежуток времени");
            }
        }

        /// <summary>Проверка на наличие администраторских прав у текущего процесса.</summary>        
        /// <returns>true - если админ</returns>
        public static Boolean IsAdmin()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>Запуск приложения с администраторскими правами</summary>        
        /// <param name="FileName">Файл приложения для запуска</param>
        /// <param name="Arguments">аргументы приложения</param>
        /// <returns>The Process.</returns>
        public static Process RunAsAdmin(String FileName, String Arguments = null)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();

            processInfo.FileName = FileName;
            processInfo.Arguments = Arguments;
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas"; // здесь вся соль

            return System.Diagnostics.Process.Start(processInfo);
        }

        /// <summary>
        /// Добавление представления в события Windows
        /// </summary>
        /// <param name="source">Наименование источника событий из журнала "Приложения"(Application). Как правило - имя службы</param>
        /// <param name="nameView">Наименование представления для отображения в дереве событий</param>
        /// <param name="descriptionView">Описание представления</param>
        public static void AddEventView(String source, String nameView, String descriptionView)
        {
            String filepath = Path.Combine(@"c:\ProgramData\Microsoft\Event Viewer\Views\", source.ReplaceInvalidPathChars() + ".xml");

            var pathViews = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(pathViews))
                Directory.CreateDirectory(pathViews);

            if (File.Exists(filepath))
                File.Delete(filepath);

            var xml =
            new XDocument(
                new XElement("ViewerConfig",
                    new XElement("QueryConfig",
                        new XElement("QueryNode",
                            new XElement("Name", nameView),
                            new XElement("Description", descriptionView),
                            new XElement("QueryList",
                                new XElement("Query",
                                    new XAttribute("Id", 0),
                                    new XElement("Select",
                                        new XAttribute("Path", "Application"),
                                        $"*[System[Provider[@Name='{source}']]]"
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        );

            xml.Save(filepath);
        }

        ///// <summary>
        ///// Установка аргументов к файлу запуска слжбы. Без проверок. Может отработать корректно, но только один раз
        ///// </summary>
        ///// <param name="serviceName">Имя службы</param>
        ///// <param name="ars">строка с аргуметами</param>
        //public static void SetServiceArgumentsToExeFile(String serviceName, String ars)
        //{
        //    using (var system = Registry.LocalMachine.OpenSubKey("System"))
        //    using (var curContrSet = system.OpenSubKey("CurrentControlSet"))
        //    using (var servs = curContrSet.OpenSubKey("Services"))
        //    using (var serv = servs.OpenSubKey(serviceName, RegistryKeyPermissionCheck.ReadWriteSubTree))
        //    {
        //        string path = serv.GetValue("ImagePath") + " " + ars;
        //        serv.SetValue("ImagePath", path);
        //    }
        //}
    }
}
