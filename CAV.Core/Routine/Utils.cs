using System;
using System.Deployment.Application;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Web;

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
        /// <param name="Path">Полный путь для удаления</param>
        public static void DeleteDirectory(String Path)
        {
            var directory = new DirectoryInfo(Path) { Attributes = FileAttributes.Normal };

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }

        /// <summary>Посыл информации о компьютере в гугловую форму </summary>        
        /// <remarks>12.05.2016</remarks>
        public static void SendInfo()
        {
            String info = Environment.MachineName + " " + Environment.UserName + " " + Environment.OSVersion.VersionString + " " + Environment.OSVersion.Version.ToString();
            var xx = new ManagementClass("Win32_Processor");
            foreach (var pr in xx.GetInstances())
                info += " ProcessorId=" + pr["ProcessorId"];

            String gform = @"https://docs.google.com/forms/d/1dNoWy70qq1PrXjK9reYElS3YoZhgghFk0dGVHkBDd1c/formResponse";
            String tBoxName = "entry.1848206053";
            String tBoxPOName = "entry.327074483";

            var request = WebRequest.Create(gform);
            String postData = tBoxName + "=" + info + "&" + tBoxPOName + "=" + DomainContext.ApplicationName;
            postData = HttpUtility.UrlPathEncode(postData);
            var data = Encoding.UTF8.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var srt = request.GetRequestStream())
                srt.Write(data, 0, data.Length);

            try
            {
                request.GetResponse();
            }
            catch { }
        }

        /// <summary>   Флаг первого запуска приложения ClickOnce </summary>
        /// <returns>true - ели приложение ClickOnce и выполняется впервые. в остальных случаях - false </returns>
        public static Boolean ClickOnceFirstRun()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return false;
            ApplicationDeployment curdep = ApplicationDeployment.CurrentDeployment;
            if (curdep == null)
                return false;
            return curdep.IsFirstRun;
        }

    }
}
