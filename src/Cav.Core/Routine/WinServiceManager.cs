using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Xml.Linq;

namespace Cav.WinService
{
    /// <summary>Менеджер управления виндовыми службами</summary>    
    public static class ServiceManager
    {
        /// <summary>Проверка существования службы по имени</summary>
        /// <param name="serviceName">Имя службы</param>
        /// <returns>True - служба установлена</returns>
        public static Boolean Exist(String serviceName) => ServiceController.GetServices().Any(s => s.ServiceName == serviceName);

        /// <summary>Запуск службы</summary>        
        /// <exception cref="ArgumentOutOfRangeException">Если отсутствует служба с указаным именем.</exception>
        /// <param name="serviceName">Имя службы.</param>
        public static void Start(String serviceName)
        {
            if (!Exist(serviceName))
                throw new ArgumentException("Указаная служба не существует");

            using (var sc = new ServiceController(serviceName))
                if (!sc.Status.In(
                        ServiceControllerStatus.Running,
                        ServiceControllerStatus.StartPending,
                        ServiceControllerStatus.StopPending,
                        ServiceControllerStatus.ContinuePending))
                    sc.Start();
        }

        /// <summary>Останов службы</summary>        
        /// <exception cref="ArgumentOutOfRangeException">Если отсутствует служба с указаным именем.</exception>
        /// <exception cref="System.TimeoutException">Если служба не остановилась за указанный промежуток времени</exception>
        /// <param name="serviceName">Имя службы.</param>
        /// <param name="waitTimeout">Таймаут ожидания останова</param>
        public static void Stop(String serviceName, TimeSpan? waitTimeout = null)
        {
            if (!Exist(serviceName))
                throw new ArgumentException("указаная служба не существует");

            using (var sc = new ServiceController(serviceName))
            {
                if (sc.Status == ServiceControllerStatus.Stopped)
                    return;

                sc.Stop();

                if (!waitTimeout.HasValue)
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                else
                {
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, waitTimeout.Value);
                    if (sc.Status != ServiceControllerStatus.Stopped)
                        throw new System.TimeoutException("Служба не остановилась за указанный промежуток времени");
                }
            }
        }

        /// <summary>Проверка на наличие администраторских прав у текущего процесса.</summary>        
        /// <returns>true - если админ</returns>
        public static Boolean IsAdmin() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        /// <summary>Запуск приложения с администраторскими правами</summary>        
        /// <param name="fileName">Файл приложения для запуска</param>
        /// <param name="arguments">аргументы приложения</param>
        /// <returns>The Process.</returns>
        public static Process RunAsAdmin(String fileName, String arguments = null)
        {
            var processInfo = new ProcessStartInfo();

            processInfo.FileName = fileName;
            processInfo.Arguments = arguments;
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas"; // здесь вся соль

            return Process.Start(processInfo);
        }

        /// <summary>
        /// Добавление представления в события Windows
        /// </summary>
        /// <param name="source">Наименование источника событий из журнала "Приложения"(Application). Как правило - имя службы</param>
        /// <param name="nameView">Наименование представления для отображения в дереве событий</param>
        /// <param name="descriptionView">Описание представления</param>
        public static void AddEventView(String source, String nameView, String descriptionView)
        {
            var filepath = Path.Combine(@"c:\ProgramData\Microsoft\Event Viewer\Views\", source.ReplaceInvalidPathChars() + ".xml");

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
    }
}
