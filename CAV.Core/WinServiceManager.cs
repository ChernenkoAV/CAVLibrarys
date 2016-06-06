using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;

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

            if (sc.Status != ServiceControllerStatus.Running)
                sc.Start();
        }

        /// <summary>Останов службы</summary>        
        /// <exception cref="ArgumentOutOfRangeException">Если отсутствует служба с указаным именем.</exception>
        /// <exception cref="TimeoutException">Если служба не остановилась за указанный промежуток времени</exception>
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

        /// <summary>Проверка на наличие администраторских апрв у текущего процесса.</summary>        
        /// <returns>true - если админ</returns>
        public static Boolean IsAdmin()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>Запуск приложения с администраторскими правами</summary>        
        /// <param name="FileName">Файл приложенияч для запуска</param>
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
    }
}
