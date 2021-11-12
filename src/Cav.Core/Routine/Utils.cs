using System;
using System.Deployment.Application;
using System.IO;

namespace Cav
{
    /// <summary>
    /// Утилиты, которые не пошли расширениями
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Удаление папки и всего, что в ней. Включая файлы с атрибутом ReadOnly
        /// </summary>
        /// <param name="path">Полный путь для удаления</param>
        public static void DeleteDirectory(String path)
        {
            if (!Directory.Exists(path))
                return;

            var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }

        /// <summary>   Флаг первого запуска приложения ClickOnce </summary>
        /// <returns>true - ели приложение ClickOnce и выполняется впервые. в остальных случаях - false </returns>
        [Obsolete("Будет перенесено")]
        public static Boolean ClickOnceFirstRun()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return false;
            var curdep = ApplicationDeployment.CurrentDeployment;
            if (curdep == null)
                return false;
            return curdep.IsFirstRun;
        }
    }
}
