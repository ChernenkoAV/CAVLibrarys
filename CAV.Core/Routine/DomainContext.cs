using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
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

        internal static String defaultNameConnection = "DefaultNameConnectionForConnectionCollectionOnDomainContext";

        /// <summary>
        /// Коллекция настроек соединения с БД
        /// </summary>
        private static Dictionary<String, SettingConnection> dcsb = new Dictionary<string, SettingConnection>();
        private struct SettingConnection
        {
            public String ConnectionString { get; set; }
            public Type ConnectionType { get; set; }
        }

        /// <summary>
        /// Инициализация подключения к БД SqlServer.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="dbName"></param>
        /// <param name="login"></param>
        /// <param name="pass"></param>
        /// <param name="integratedSecurity">IntegratedSecurity</param>
        /// <param name="MARS">MultipleActiveResultSets</param>
        /// <param name="applicationName">Наименование приожения</param>
        /// <param name="connectionName">има подключения для коллекции</param>
        /// <param name="pooling">Добавлять подключение в пул</param>
        /// <returns>Сформированная строка соединения</returns>
        public static string InitConnection(
            String server,
            String dbName,
            String login = null,
            String pass = null,
            Boolean integratedSecurity = false,
            Boolean MARS = false,
            String applicationName = null,
            String connectionName = null,
            Boolean pooling = false)
        {
            var scsb = new SqlConnectionStringBuilder();
            scsb.DataSource = server;
            scsb.InitialCatalog = dbName;
            scsb.IntegratedSecurity = integratedSecurity;
            scsb.MultipleActiveResultSets = MARS;
            scsb["LANGUAGE"] = "Russian";
            scsb.ConnectTimeout = 5;
            scsb.Pooling = pooling;

            if (!login.IsNullOrWhiteSpace())
                scsb.UserID = login;
            if (!pass.IsNullOrWhiteSpace())
                scsb.Password = pass;
            if (!applicationName.IsNullOrWhiteSpace())
                scsb.ApplicationName = applicationName;

            if (connectionName.IsNullOrWhiteSpace())
                connectionName = defaultNameConnection;

            InitConnection(
                connectionString: scsb.ToString(),
                connectionName: connectionName);

            return scsb.ToString();
        }

        /// <summary>
        /// Настройка подключения к БД SQl Server. Проверка соединения.
        /// </summary>
        /// <param name="connectionString">Строка подключения</param>
        /// <param name="pooling">Добавлять подключение в пул</param>
        /// <param name="connectionName">Имя подключения для коллекции</param>
        public static void InitConnection(
            String connectionString,
            Boolean pooling = false,
            String connectionName = null)
        {
            if (connectionName.IsNullOrWhiteSpace())
                connectionName = defaultNameConnection;

            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connectionString);
            scsb["LANGUAGE"] = "Russian";
            scsb.ConnectTimeout = 5;
            scsb.Pooling = pooling;

            InitConnection<SqlConnection>(connectionString, connectionName);
        }

        /// <summary>
        /// Инициализация нового соединения с БД указанного типа. Проверка соединения с сервером.
        /// </summary>
        /// <typeparam name="TConnection">Тип - наследник DbConnection</typeparam>
        /// <param name="connectionString">Строка соединения</param>
        /// <param name="connectionName">Имя подключения</param>
        public static void InitConnection<TConnection>(
            String connectionString,
            String connectionName = null)
            where TConnection : DbConnection
        {
            if (connectionName.IsNullOrWhiteSpace())
                connectionName = defaultNameConnection;

            using (DbConnection conn = (DbConnection)Activator.CreateInstance(typeof(TConnection), connectionString))
                conn.Open();

            var setCon = new SettingConnection()
            {
                ConnectionString = connectionString,
                ConnectionType = typeof(TConnection)
            };

            if (dcsb.ContainsKey(connectionName))
                dcsb[connectionName] = setCon;
            else
                dcsb.Add(connectionName, setCon);
        }

        /// <summary>
        /// Получение экземпляра открытого соединения с БД
        /// </summary>
        /// <param name="connectionName">Имя соединения в коллекции</param>
        /// <returns></returns>
        public static DbConnection Connection(String connectionName = null)
        {
            if (connectionName.IsNullOrWhiteSpace())
                connectionName = defaultNameConnection;

            if (!dcsb.ContainsKey(connectionName))
                throw new Exception("Соединение с БД не настроено");

            var setCon = dcsb[connectionName];
            var connection = (DbConnection)Activator.CreateInstance(setCon.ConnectionType, setCon.ConnectionString);
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
        /// <returns>ClickOnce или версию AssemblyVersion исполняемого файла</returns>
        public static Version CurrentVersion
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    ApplicationDeployment curdep = ApplicationDeployment.CurrentDeployment;
                    if (curdep != null)
                        return curdep.CurrentVersion;
                }

                return Assembly.GetEntryAssembly().GetName().Version;
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
        /// Логин, под которым прилогинены к БД. Работает только для Sql Server.
        /// </summary>
        /// <param name="ConnectionName">Имя соедиения</param>
        public static String UserLogin(String ConnectionName = null)
        {
            if (ConnectionName.IsNullOrWhiteSpace())
                ConnectionName = defaultNameConnection;

            if (!dcsb.ContainsKey(ConnectionName))
                throw new Exception("Не настроено соединение с БД");

            var setCom = dcsb[ConnectionName];
            if (setCom.ConnectionType != typeof(SqlConnection))
                return null;
            var sqlConBuild = new SqlConnectionStringBuilder(setCom.ConnectionString);

            return sqlConBuild.UserID;
        }

        #endregion
    }
}
