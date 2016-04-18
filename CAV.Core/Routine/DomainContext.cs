using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
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
    public static partial class DomainContext
    {
        #region Для запуска программы в одном экземпляре (На базе поиска процесса)

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private const int WS_SHOWNORMAL = 1;

        /// <summary>
        /// Поиск процесса запущенного приложения
        /// </summary>
        /// <param name="SetForeground">Если приложение имеет окна, то вывести окно вперед</param>
        /// <returns>true - приложение найдено(можно не запускать вторую копию), false - приложение не найдено.</returns>
        public static Boolean FindStartedProgram(Boolean SetForeground = true)
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            Process findedProcess = null;
            //Loop through the running processes in with the same name
            foreach (Process process in processes)
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

            if (SetForeground && findedProcess.MainWindowHandle != IntPtr.Zero)
            {
                //Make sure the window is not minimized or maximized
                ShowWindowAsync(findedProcess.MainWindowHandle, WS_SHOWNORMAL);
                //Set the real intance to foreground window
                SetForegroundWindow(findedProcess.MainWindowHandle);
            }

            return true;
        }

        #endregion

        #region Статический КонекшнСтринг

        private static String defaultNameConnection = "DefaultNameConnectionForConnectionCollectionOnDomainContext";

        /// <summary>
        /// Коллекция строк соединения с SQL Server
        /// </summary>
        private static Dictionary<String, SqlConnectionStringBuilder> dcsb = new Dictionary<string, SqlConnectionStringBuilder>();

        /// <summary>
        /// Инициализация подключения к БД.
        /// </summary>
        /// <param name="Server"></param>
        /// <param name="DBName"></param>
        /// <param name="Login"></param>
        /// <param name="Pass"></param>
        /// <param name="IntegratedSecurity">IntegratedSecurity</param>
        /// <param name="MARS">MultipleActiveResultSets</param>
        /// <param name="ApplicationName">Наименование приожения</param>
        /// <param name="SqlConnectionName">има подключения для коллекции</param>
        /// <returns>Сформированная строка соединения</returns>
        public static string InitConnection(
            String Server,
            String DBName,
            String Login = null,
            String Pass = null,
            Boolean IntegratedSecurity = false,
            Boolean MARS = false,
            String ApplicationName = null,
            String SqlConnectionName = null)
        {
            var scsb = new SqlConnectionStringBuilder();
            scsb.DataSource = Server;
            scsb.InitialCatalog = DBName;
            scsb.IntegratedSecurity = IntegratedSecurity;
            scsb.MultipleActiveResultSets = MARS;

            if (!Login.IsNullOrWhiteSpace())
                scsb.UserID = Login;
            if (!Pass.IsNullOrWhiteSpace())
                scsb.Password = Pass;
            if (!ApplicationName.IsNullOrWhiteSpace())
                scsb.ApplicationName = ApplicationName;

            if (SqlConnectionName.IsNullOrWhiteSpace())
                SqlConnectionName = defaultNameConnection;

            InitConnection(
                ConnectionString: scsb.ToString(),
                SqlConnectionName: SqlConnectionName);

            return scsb.ToString();
        }

        /// <summary>
        /// Настройка подключения к БД. Проверка соединения.
        /// </summary>
        /// <param name="ConnectionString">Строка подключения</param>
        /// <param name="SqlConnectionName">има подключения для коллекции</param>
        public static void InitConnection(
            String ConnectionString,
            String SqlConnectionName = null)
        {
            if (SqlConnectionName.IsNullOrWhiteSpace())
                SqlConnectionName = defaultNameConnection;

            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(ConnectionString);
            scsb["LANGUAGE"] = "Russian";
            scsb.ConnectTimeout = 5;
            scsb.Pooling = false;
            using (var connection = new SqlConnection(scsb.ToString()))
                connection.Open();

            if (dcsb.ContainsKey(SqlConnectionName))
                dcsb[SqlConnectionName] = scsb;
            else
                dcsb.Add(SqlConnectionName, scsb);
        }

        /// <summary>
        /// Получение экземпляра открытого соединения с БД
        /// </summary>
        /// <param name="SqlConnectionName">Имя соединения в коллекции</param>
        /// <returns></returns>
        public static SqlConnection Connection(String SqlConnectionName = null)
        {
            if (SqlConnectionName.IsNullOrWhiteSpace())
                SqlConnectionName = defaultNameConnection;

            if (!dcsb.ContainsKey(SqlConnectionName))
                throw new Exception("Соединение с БД не настроено");

            var connection = new SqlConnection(dcsb[SqlConnectionName].ToString());
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Имена соединений, присутствующие в коллекции
        /// </summary>
        public static ReadOnlyCollection<String> NamesConnectionOnCollection
        {
            get
            {
                return new ReadOnlyCollection<string>(dcsb.Keys.ToArray());
            }
        }

        #endregion

        #region Environment

        /// <summary>
        /// Получение параметров командной строки приложения ClickOnce
        /// </summary>
        public static string[] ProgramArguments
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return new string[0];

                var adata = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (adata == null)
                    return new string[0];

                var larg = adata.ToList();

                if (larg.Count == 0)
                    return new string[0];

                //Другой нюанс заключается в том, что местоположение файла передается в формате URI, 
                //как в file:///c:\MyApp\MyFile.testDoc. 
                //Это значит, что для получения действительного пути к файлу и очистке от защищенных пробелов (которые в URI транслируются в символ %20) понадобится следующий код:
                for (int i = 0; i < larg.Count - 1; i++)
                {

                    if (!larg[i].StartsWith("file:///"))
                        continue;
                    Uri fileUri = new Uri(larg[i]);
                    larg[i] = Uri.UnescapeDataString(fileUri.AbsolutePath);
                    //После этого можно проверить существование файла и открыть его, как обычно.                    
                }

                return larg.ToArray();
            }
        }

        /// <summary>
        /// Путь в AppData для текущего пользователя текущего приложения(%AppData%\"NameEntryAssembly") (Если отсутствует - то он создается...)
        /// </summary>
        public static String AppDataUserStorage
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                // Косяк в XP
                if (path.Contains(@"C:\Windows\system32"))
                    path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

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
                string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                path = Path.Combine(path, NameEntryAssembly);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>
        /// Путь к временной папке в %Temp%\"NameEntryAssembly". Если отсутствует - создается
        /// </summary>
        public static String TempPath
        {
            get
            {
                String tempPath = Path.Combine(Path.GetTempPath(), NameEntryAssembly);
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);
                return tempPath;
            }
        }

        /// <summary>
        /// Имя сборки, из которого запущенно приложение (имя exe файла) (ТОЛЬКО ЕСЛИ ЭТО НЕ COM!!!)
        /// При работе в IIS бесмысленно, ибо там всегда процесс w3c. вроде. Какой-то один, короче...
        /// </summary>
        public static String NameEntryAssembly
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            }
        }

        /// <summary>
        /// Текущая версия приложения.
        /// </summary>
        /// <returns>null - если оно не развернуто ClickOnce</returns>
        public static Version CurrentVersion
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return null;
                ApplicationDeployment curdep = ApplicationDeployment.CurrentDeployment;
                if (curdep == null)
                    return null;
                return curdep.CurrentVersion;
            }
        }

        /// <summary>
        /// Имя приложения
        /// </summary>
        public static String ApplicationName
        {
            get
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                    return null;
                ApplicationDeployment curdep = ApplicationDeployment.CurrentDeployment;
                if (curdep == null)
                    return null;
                return curdep.UpdatedApplicationFullName;
            }
        }

        /// <summary>
        /// Логин, под которым прилогинены к БД
        /// </summary>
        /// <param name="SqlConnectionName">Имя соедиения</param>
        public static String UserLogin(String SqlConnectionName = null)
        {
            if (SqlConnectionName.IsNullOrWhiteSpace())
                SqlConnectionName = defaultNameConnection;

            if (!dcsb.ContainsKey(SqlConnectionName))
                throw new Exception("Не настроено соединение с БД");

            return dcsb[SqlConnectionName].UserID;
        }

        #endregion
    }
}
