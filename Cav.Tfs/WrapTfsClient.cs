using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Cav.ReflectHelpers;

namespace Cav.Tfs
{
    /// <summary>
    /// Допустимые блокирован уровней. Используется в свойстве LockLevel PendingChange
    /// </summary>
    public enum LockLevel
    {
        /// <summary>
        /// Без блокировки
        /// </summary>
        None,
        /// <summary>
        /// Блокировку возврата
        /// </summary>
        Checkin,
        /// <summary>
        /// Блокировка извлечения
        /// </summary>
        CheckOut,
        /// <summary>
        /// Состояние блокировки остается неизменным
        /// </summary>
        Unchanged
    }

    #region объекты обмена
    /// <summary>
    /// Объект, содержащий WorkspaceInfo
    /// </summary>
    public struct WorkspaceInfo
    {
        internal Object WSI { get; set; }
    }
    /// <summary>
    /// Объект, содержащий VersionControlServer
    /// </summary>
    public struct VersionControlServer
    {
        internal Object VCS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий Workspace
    /// </summary>
    public struct Workspace
    {
        internal Object WS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий VersionSpec
    /// </summary>
    public struct VersionSpec
    {
        internal Object VS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий QueryHistoryParameters
    /// </summary>
    public struct QueryHistoryParameters
    {
        internal Object QHP { get; set; }
    }
    /// <summary>
    /// Информация об Changeset
    /// </summary>
    public struct Changeset
    {
        /// <summary>
        /// ChangesetId
        /// </summary>
        public int ChangesetId { get; internal set; }
        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreationDate { get; internal set; }
    }
    /// <summary>
    /// Объект, содержащий PendingChange
    /// </summary>
    public struct PendingChange
    {
        internal Object PC { get; set; }
    }
    /// <summary>
    /// Информация о сопоставлении пути на сервере и локальном пути
    /// </summary>
    public class WorkingFolder
    {
        /// <summary>
        /// Элемент на сервере
        /// </summary>
        public String ServerItem { get; internal set; }
        /// <summary>
        /// Локальный элемент
        /// </summary>
        public String LocalItem { get; internal set; }
        /// <summary>
        /// Флаг, показывающий скрыт ли элемент, учавствующий в сопоставлении()
        /// </summary>
        public bool IsCloaked { get; internal set; }
    }

    #endregion

    /// <summary>
    /// Обертка к сботкам TFS
    /// </summary>
    public class WrapTfs
    {
        private const string tfsClient12 = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\ReferenceAssemblies\v2.0\";
        private const string tfsClient14 = @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\";
        private static String pathTfsdll = null;

        private const string tfsClientDll = "Microsoft.TeamFoundation.Client.dll";
        private const string tfsVersionControlClientDll = "Microsoft.TeamFoundation.VersionControl.Client.dll";
        private const string tfsVersionControlCommonDll = "Microsoft.TeamFoundation.VersionControl.Common.dll";
        private const string tfsVersionControlControlsCommonDLL = "Microsoft.TeamFoundation.VersionControl.Controls.Common.dll";

        private static Assembly tfsClientAssembly = null;
        private static Assembly tfsVersionControlCommonAssembly = null;
        private static Assembly tfsVersionControlClientAssembly = null;
        private static Assembly tfsVersionControlControlsCommonAssembly = null;

        static WrapTfs()
        {
            if (Directory.Exists(tfsClient14))
                pathTfsdll = tfsClient14;
            if (pathTfsdll == null && Directory.Exists(tfsClient12))
                pathTfsdll = tfsClient12;
            if (pathTfsdll == null)
                throw new FileNotFoundException(String.Format("Not found TFS assemblys on path {0}, {1}.", tfsClient14, tfsClient12));

            AppDomain.CurrentDomain.AssemblyResolve += WrapTfs.CurrentDomain_AssemblyResolve;

            tfsClientAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsClientDll)));
            tfsVersionControlClientAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsVersionControlClientDll)));
            tfsVersionControlControlsCommonAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsVersionControlControlsCommonDLL)));
            tfsVersionControlCommonAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsVersionControlCommonDll)));
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var asly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            if (asly != null)
                return asly;

            string assemblyFile = (args.Name.Contains(','))
                ? args.Name.Substring(0, args.Name.IndexOf(','))
                : args.Name;

            assemblyFile += ".dll";
            var path = Path.Combine(pathTfsdll, assemblyFile);
            if (!File.Exists(path))
            {
                var culture = args.Name.Split(new Char[] { ',' }).Where(x => x.Contains("Culture")).FirstOrDefault();
                if (culture == null)
                    throw new FileLoadException($"Not define culture for {args.Name}");
                var targetculture = culture.Split(new char[] { '=', '-' }).Where(x => !x.Contains("Culture")).FirstOrDefault();
                if (targetculture == null)
                    throw new FileLoadException($"Not compute culture for {args.Name}");
                path = Path.Combine(Path.Combine(pathTfsdll, targetculture), assemblyFile);
            }
            return Assembly.LoadFile(path);

        }

        private Object ws = null;
        private Object WorkstationGet()
        {
            if (ws != null)
                return ws;
            ws = tfsVersionControlClientAssembly.GetStaticPropertyValue("Workstation", "Current");
            return ws;
        }
        /// <summary>
        /// Обновить кэш
        /// </summary>
        public void WorkstationReloadCache()
        {
            WorkstationGet().InvokeMethod("ReloadCache");
        }

        private Object TeamProjectCollectionGet(Uri serverUri)
        {
            return tfsClientAssembly.InvokeStaticMethod("TfsTeamProjectCollectionFactory", "GetTeamProjectCollection", serverUri);
        }
        /// <summary>
        /// Получение информации о рабочей области
        /// </summary>
        /// <param name="localFileName">Локальный элемент</param>
        /// <returns>Объект, содержащий информацию о рабочей области</returns>
        public WorkspaceInfo WorkspaceInfoGet(String localFileName)
        {
            return new WorkspaceInfo() { WSI = WorkstationGet().InvokeMethod("GetLocalWorkspaceInfo", localFileName) };
        }
        /// <summary>
        /// Получение экземпляра управления СКВ
        /// </summary>
        /// <param name="workspaceInfo">Объект, содержащий информацию о рабочей области</param>
        /// <returns>экземпляр СКВ</returns>
        public VersionControlServer VersionControlServerGet(WorkspaceInfo workspaceInfo)
        {
            Uri servUri = (Uri)workspaceInfo.WSI.GetPropertyValue("ServerUri");
            return VersionControlServerGet(servUri);
        }
        /// <summary>
        /// Получение экземпляра управления СКВ        
        /// </summary>
        /// <param name="serverUri">Uri к серверу ЕАЫ</param>
        /// <returns>экземпляр СКВ</returns>
        public VersionControlServer VersionControlServerGet(Uri serverUri)
        {
            var tpc = TeamProjectCollectionGet(serverUri);
            Type vcsType = tfsVersionControlClientAssembly.
#if NET40
                GetTypes()
#else
            ExportedTypes
#endif
            .Single(x => x.Name == "VersionControlServer");

            var res = new VersionControlServer();
            res.VCS = tpc.InvokeMethod("GetService", vcsType);

            return res;
        }
        /// <summary>
        /// Получение рабочей области
        /// </summary>
        /// <param name="vcs">СКВ</param>
        /// <param name="wsi">Объект, содержащий информацию о рабочей области</param>
        /// <returns>Рабочая область</returns>
        public Workspace WorkspaceGet(VersionControlServer vcs, WorkspaceInfo wsi)
        {
            var res = new Workspace();
            res.WS = vcs.VCS.InvokeMethod("GetWorkspace", wsi.WSI);
            return res;
        }
        /// <summary>
        /// Получение объекта, содержащего VersionSpec (опеределение конкретной версии)
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <returns></returns>
        public VersionSpec WorkspaceVersionSpecCreate(Workspace ws)
        {
            var res = new VersionSpec();
            res.VS = tfsVersionControlClientAssembly.CreateInstance("WorkspaceVersionSpec", ws.WS);
            return res;
        }
        /// <summary>
        /// Получение истории локального элемента
        /// </summary>
        /// <param name="localFileName">Путь к локальному элементу (файл или папка)</param>
        /// <param name="vs">Объект, содержащий VersionSpec (опеределение конкретной версии)</param>
        /// <returns>Сформированный объект запроса к истории</returns>
        public QueryHistoryParameters QueryHistoryParametersCreate(String localFileName, VersionSpec vs)
        {
            var RecursionTypeFull = tfsVersionControlClientAssembly.GetEnumValue("RecursionType", "Full");
            var res = new QueryHistoryParameters();
            res.QHP = tfsVersionControlClientAssembly.CreateInstance("QueryHistoryParameters", localFileName, RecursionTypeFull);
            res.QHP.SetPropertyValue("ItemVersion", vs.VS);
            res.QHP.SetPropertyValue("VersionEnd", vs.VS);
            res.QHP.SetPropertyValue("MaxResults", 1);
            return res;
        }
        /// <summary>
        /// Получение информации о наборе изменений в соответствии с параметрами пискового запроса
        /// </summary>
        /// <param name="vsc">СКВ</param>
        /// <param name="qhp">Параметры запроса</param>
        /// <returns>Информация о наборе изменений</returns>
        public Changeset ChangesetGet(VersionControlServer vsc, QueryHistoryParameters qhp)
        {
            var cscolobj = ((IEnumerable)vsc.VCS.InvokeMethod("QueryHistory", qhp.QHP)).GetEnumerator();
            object csobj = null;
            if (cscolobj.Current == null)
                cscolobj.MoveNext();
            csobj = cscolobj.Current;

            var res = new Changeset();
            res.ChangesetId = (int)csobj.GetPropertyValue("ChangesetId");
            res.CreationDate = (DateTime)csobj.GetPropertyValue("CreationDate");
            return res;
        }
        /// <summary>
        /// Создание рабочей области
        /// </summary>
        /// <param name="vcs">СКВ</param>
        /// <param name="workspaceName">Имя рабочей области</param>
        /// <param name="workspaceComment">Комментарий к рабочей области</param>
        /// <param name="workspaceLocationOnServer">Локация рабочей области(сервер либо локально)</param>
        /// <returns></returns>
        public Workspace WorkspaceCreate(
            VersionControlServer vcs,
            String workspaceName,
            String workspaceComment,
            Boolean workspaceLocationOnServer)
        {
            var tpc = vcs.VCS.GetPropertyValue("TeamProjectCollection");
            var cwp = tfsVersionControlClientAssembly.CreateInstance("CreateWorkspaceParameters", workspaceName);
            cwp.SetPropertyValue("Comment", workspaceComment);
            cwp.SetPropertyValue("OwnerName", tpc.GetPropertyValue("AuthorizedIdentity").GetPropertyValue("UniqueName"));

            object localion = null;
            if (workspaceLocationOnServer)
                localion = tfsVersionControlCommonAssembly.GetEnumValue("WorkspaceLocation", "Server");
            else
                localion = tfsVersionControlCommonAssembly.GetEnumValue("WorkspaceLocation", "Local");

            cwp.SetPropertyValue("Location", localion);

            var res = new Workspace();
            res.WS = vcs.VCS.InvokeMethod("CreateWorkspace", cwp);
            return res;
        }
        /// <summary>
        /// Сопоставление в рабочей области пути в СКВ и локальным путям
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <param name="serverPath">Путь на сервере СКВ</param>
        /// <param name="localPath">Локальный путь</param>
        public void WorkspaceMap(
            Workspace ws,
            string serverPath,
            string localPath
            )
        {
            ws.WS.InvokeMethod("Map", serverPath, localPath);
        }
        /// <summary>
        /// Удаление рабочей области
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        public void WorkspaceDelete(Workspace ws)
        {
            ws.WS.InvokeMethod("Delete");
        }


        /// <summary>
        /// Добавить файл в рабочую область
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <param name="localPathFile">Полный путь файла, находящийся в папке, замапленой в рабочей области</param>
        public void WorkspaceAddFile(Workspace ws, string localPathFile)
        {
            ws.WS.InvokeMethod("PendAdd", localPathFile);
        }

        /// <summary>
        /// Извлечение файла для редактирования
        /// </summary>
        /// <param name="ws">Рабояа область</param>
        /// <param name="localPathFile">Полный путь файла, находящийся в папке, замапленой в рабочей области</param>
        /// <returns>true - успешно, false - неуспешно</returns>
        public bool WorkspaceCheckOut(Workspace ws, string localPathFile)
        {
            return (int)ws.WS.InvokeMethod("PendEdit", localPathFile) != 0;
        }

        /// <summary>
        /// Возврат изменений в рабочей области
        /// </summary>
        /// <param name="ws">Рабоча область</param>
        /// <param name="commentOnCheckIn">Комментарий для изменения</param>
        /// <param name="numberTasks">Номера задач для чекина</param>
        public void WorkspaceCheckIn(Workspace ws, string commentOnCheckIn, List<int> numberTasks = null)
        {
            var gpc = WorkspaceGetPendingChanges(ws);
            var wscp = tfsVersionControlClientAssembly.CreateInstance("WorkspaceCheckInParameters", gpc.PC, commentOnCheckIn);
            if (numberTasks != null)
            {
                // TODO Получить рабочие элементы
                //wscp.AssociatedWorkItems
            }

            ws.WS.InvokeMethod("CheckIn", wscp);
        }

        private PendingChange WorkspaceGetPendingChanges(Workspace ws)
        {
            var res = new PendingChange();
            res.PC = ws.WS.InvokeMethod("GetPendingChanges");
            return res;
        }
        /// <summary>
        /// Отмена изменений в рабочей области
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        public void WorkspaceUndo(Workspace ws)
        {
            var pc = WorkspaceGetPendingChanges(ws);
            int cnt = (int)pc.PC.GetPropertyValue("Length");
            if (cnt == 0)
                return;
            ws.WS.InvokeMethod("Undo", pc.PC);
        }
        /// <summary>
        /// Получение элементов сопоставления в рабочей области
        /// </summary>
        /// <param name="ws">Опбочая область</param>
        /// <returns>Коллекция, описывающая элементы сопоставления</returns>
        public List<WorkingFolder> WorkspaceFoldersGet(Workspace ws)
        {
            var res = new List<WorkingFolder>();
            var flds = (ws.WS.GetPropertyValue("Folders") as IEnumerable).GetEnumerator();
            while (flds.MoveNext())
            {
                var crnt = flds.Current;
                if (crnt == null)
                    continue;
                var fi = new WorkingFolder();
                fi.ServerItem = (String)crnt.GetPropertyValue("ServerItem");
                fi.LocalItem = (String)crnt.GetPropertyValue("LocalItem");
                fi.IsCloaked = (Boolean)crnt.GetPropertyValue("IsCloaked");
                res.Add(fi);
            }

            return res;
        }
        /// <summary>
        /// Блокировка элемента
        /// </summary>
        /// <param name="ws">рабочая область</param>
        /// <param name="serverPathItem">Пусть на сервере СКВ</param>
        /// <param name="lockLevel">Уровенб блокировки</param>
        /// <returns>Успешна ли блокировка</returns>
        public bool WorkspaceLockFile(Workspace ws, string serverPathItem, LockLevel lockLevel)
        {
            bool res = false;
            try
            {
                var llenum = tfsVersionControlClientAssembly.GetEnumValue("LockLevel", lockLevel.ToString());
                res = (int)ws.WS.InvokeMethod("SetLock", serverPathItem, llenum) != 0;
            }
            catch
            {

            }
            return res;
        }
        /// <summary>
        /// Получение последней версии элементов
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <param name="serverItemPath">путь(элемент) на сервере СКВ</param>
        /// <returns>Количество полученных элементов</returns>
        public long WorkspaceGetLastItem(Workspace ws, String serverItemPath)
        {
            var RecursionTypeFull = tfsVersionControlClientAssembly.GetEnumValue("RecursionType", "Full");
            var ItemSpec = tfsVersionControlClientAssembly.CreateInstance("ItemSpec", serverItemPath, RecursionTypeFull);

            var VersionSpecLatest = tfsVersionControlClientAssembly.GetStaticPropertyValue("VersionSpec", "Latest");

            var GetRequest = tfsVersionControlClientAssembly.CreateInstance("GetRequest", ItemSpec, VersionSpecLatest);

            var GetOptionsType = tfsVersionControlClientAssembly.
#if NET40
                GetTypes()
#else
            ExportedTypes
#endif
            .Single(x => x.Name == "GetOptions");
            int GetOptionsGetAll = (int)tfsVersionControlClientAssembly.GetEnumValue("GetOptions", "GetAll");
            int GetOptionsOverwrit = (int)tfsVersionControlClientAssembly.GetEnumValue("GetOptions", "Overwrite");
            var GetOptionsValue = Enum.ToObject(GetOptionsType, GetOptionsGetAll + GetOptionsOverwrit);
            var GetLastFile = ws.WS.InvokeMethod("Get", GetRequest, GetOptionsValue);

            return (long)GetLastFile.GetPropertyValue("NumFiles");
        }
        /// <summary>
        /// Отобразить диалог выбора проектов TFS. Настроенно на выбор только одгного проекта
        /// </summary>
        /// <param name="parentWindow">Родительское окно</param>
        /// <returns>Uri выбранного проекта. Иначе null</returns>
        public Uri ShowTeamProjectPicker(IWin32Window parentWindow)
        {
            var TeamProjectPickerModeNoProject = tfsClientAssembly.GetEnumValue("TeamProjectPickerMode", "NoProject");

            var tpp = tfsClientAssembly.CreateInstance("TeamProjectPicker", TeamProjectPickerModeNoProject, false);
            var dr = (DialogResult)tpp.InvokeMethod("ShowDialog", parentWindow);
            if (dr != DialogResult.OK)
                return null;

            return (Uri)tpp.GetPropertyValue("SelectedTeamProjectCollection").GetPropertyValue("Uri");
        }
        /// <summary>
        /// Отображение диалога выбора пути на сервере СКВ
        /// </summary>
        /// <param name="parentWindow">Родительское окно</param>
        /// <param name="vcs">СКВ</param>
        /// <param name="initalPath">Путь для инизиализации диалога</param>
        /// <returns>Выбранный путь на сервере. Иначе null.</returns>
        public String ShowDialogChooseServerFolder(IWin32Window parentWindow, VersionControlServer vcs, String initalPath)
        {
            var dcsf = tfsVersionControlControlsCommonAssembly.CreateInstance("DialogChooseServerFolder", vcs.VCS, initalPath);

            dcsf.SetPropertyValue("ShowInTaskbar", false);
            var dr = (DialogResult)dcsf.InvokeMethod("ShowDialog", parentWindow);
            if (dr != DialogResult.OK)
                return null;

            return (String)dcsf.GetPropertyValue("CurrentServerItem");
        }

        //http://stackoverflow.com/questions/25923734/how-can-i-retrieve-a-list-of-workitems-from-tfs-in-c


        //public Object get_querys()
        //{
        //        string selectedProject = this.listProjects.SelectedItem.ToString();
        //        TfsTeamProjectCollection teamProjectCollection = TFSDetail.GetTeamProjectDetails(UrlPath);
        //if (teamProjectCollection != null)
        //{
        //     Project detailsOfTheSelectedProject = null;
        //        WorkItemStore workItemStore =
        //              (WorkItemStore)teamProjectCollection.GetService(typeof(WorkItemStore));

        //        string folder = "My Queries";
        //    var project = workItemStore.Projects[selectedProject];
        //    QueryHierarchy queryHierarchy = project.QueryHierarchy;
        //    var queryFolder = queryHierarchy as QueryFolder;
        //    QueryItem queryItem = queryFolder[folder];
        //    queryFolder = queryItem as QueryFolder;
        //    foreach (var item in queryFolder)
        //    {
        //        listQueries.Items.Add(item.Name);
        //    }
        //}


        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        ////Handles nested query folders    
        //private static Guid FindQuery(QueryFolder folder, string queryName)
        //{
        //    foreach (var item in folder)
        //    {
        //        if (item.Name.Equals(queryName, StringComparison.InvariantCultureIgnoreCase))
        //        {
        //            return item.Id;
        //        }

        //        var itemFolder = item as QueryFolder;
        //        if (itemFolder != null)
        //        {
        //            var result = FindQuery(itemFolder, queryName);
        //            if (!result.Equals(Guid.Empty))
        //            {
        //                return result;
        //            }
        //        }
        //    }
        //    return Guid.Empty;
        //}

        //static void Main(string[] args)
        //{
        //    var collectionUri = new Uri("http://TFS/tfs/DefaultCollection");
        //    var server = new TfsTeamProjectCollection(collectionUri);
        //    var workItemStore = server.GetService<WorkItemStore>();

        //    var teamProject = workItemStore.Projects["TeamProjectName"];

        //    var x = teamProject.QueryHierarchy;
        //    var queryId = FindQuery(x, "QueryNameHere");

        //    var queryDefinition = workItemStore.GetQueryDefinition(queryId);
        //    var variables = new Dictionary<string, string>() { { "project", "TeamProjectName" } };

        //    var result = workItemStore.Query(queryDefinition.QueryText, variables); //https://msdn.microsoft.com/ru-ru/library/bb140400(v=vs.120).aspx
        //}
    }
}


