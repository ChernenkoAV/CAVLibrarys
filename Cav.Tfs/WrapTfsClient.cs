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

    /// <summary>
    /// Тип элемента, выбранного в диалоге выбора элемента
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// Папка
        /// </summary>
        Folder,
        /// <summary>
        /// Файл
        /// </summary>
        File,
        /// <summary>
        /// Какой-то другой
        /// </summary>
        Any
    }

    #region объекты обмена

    /// <summary>
    /// Представление рабочего элемента
    /// </summary>
    public class WorkItem
    {
        internal WorkItem() { }
        /// <summary>
        /// ИД
        /// </summary>
        public int ID { get; internal set; }
        /// <summary>
        /// Заголовок рабочего элемента
        /// </summary>
        public String Title { get; internal set; }
    }

    /// <summary>
    /// Выбранный элемент на сервере
    /// </summary>
    public sealed class SelItem
    {
        internal SelItem() { }
        /// <summary>
        /// Серверный путь
        /// </summary>
        public String Path { get; internal set; }
        /// <summary>
        /// Тип элемента
        /// </summary>
        public ItemType ItemType { get; internal set; }
    }

    /// <summary>
    /// Элемент в представлении (дереве) запросов рабочих элементов
    /// </summary>
    public class QueryItemNode
    {
        internal QueryItemNode()
        {
            ChildNodes = new List<QueryItemNode>();
        }

        /// <summary>
        /// Элемент является каталогом
        /// </summary>
        public Boolean IsFolder
        {
            get
            {
                return !QueryID.HasValue;
            }
        }
        /// <summary>
        /// Имя элемента
        /// </summary>
        public String Name { get; internal set; }
        internal String ProjectName { get; set; }
        /// <summary>
        /// Дочерние элементы
        /// </summary>
        public List<QueryItemNode> ChildNodes { get; private set; }
        internal Guid? QueryID { get; set; }

        /// <summary>
        /// получение имени
        /// </summary>
        /// <returns>Имя</returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Объект, содержащий WorkspaceInfo
    /// </summary>
    public interface IWorkspaceInfo { }
    internal class WorkspaceInfo : IWorkspaceInfo
    {
        internal Object WSI { get; set; }
    }

    /// <summary>
    /// Объект, содержащий VersionControlServer
    /// </summary>
    public interface IVersionControlServer { }
    internal class VersionControlServer : IVersionControlServer
    {
        internal Object VCS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий Workspace
    /// </summary>
    public interface IWorkspace { }
    internal class Workspace : IWorkspace
    {
        internal Object WS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий VersionSpec
    /// </summary>
    public interface IVersionSpec { }
    internal class VersionSpec : IVersionSpec
    {
        internal Object VS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий QueryHistoryParameters
    /// </summary>
    public interface IQueryHistoryParameters { }
    internal class QueryHistoryParameters : IQueryHistoryParameters
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
    public interface IPendingChange { }
    internal class PendingChange : IPendingChange
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
        private const string tfsVersionControlControlsCommonDll = "Microsoft.TeamFoundation.VersionControl.Controls.Common.dll";
        private const string tfsVersionControlControlsDll = "Microsoft.TeamFoundation.VersionControl.Controls.dll";
        private const string tfsWorkItemTrackingClientDll = "Microsoft.TeamFoundation.WorkItemTracking.Client.dll";

        private static Assembly tfsClientAssembly = null;
        private static Assembly tfsVersionControlCommonAssembly = null;
        private static Assembly tfsVersionControlClientAssembly = null;
        private static Assembly tfsVersionControlControlsCommonAssembly = null;
        private static Assembly tfsVersionControlControlsAssembly = null;
        private static Assembly tfsWorkItemTrackingClientAssembly = null;

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
            tfsVersionControlControlsCommonAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsVersionControlControlsCommonDll)));
            tfsVersionControlControlsAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsVersionControlControlsDll)));
            tfsVersionControlCommonAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsVersionControlCommonDll)));
            tfsWorkItemTrackingClientAssembly = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(pathTfsdll, tfsWorkItemTrackingClientDll)));
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
        public IWorkspaceInfo WorkspaceInfoGet(String localFileName)
        {
            return new WorkspaceInfo() { WSI = WorkstationGet().InvokeMethod("GetLocalWorkspaceInfo", localFileName) };
        }
        /// <summary>
        /// Получение экземпляра управления СКВ
        /// </summary>
        /// <param name="workspaceInfo">Объект, содержащий информацию о рабочей области</param>
        /// <returns>экземпляр СКВ</returns>
        public IVersionControlServer VersionControlServerGet(IWorkspaceInfo workspaceInfo)
        {
            Uri servUri = (Uri)((WorkspaceInfo)workspaceInfo).WSI.GetPropertyValue("ServerUri");
            return VersionControlServerGet(servUri);
        }
        /// <summary>
        /// Получение экземпляра управления СКВ        
        /// </summary>
        /// <param name="serverUri">Uri к серверу TFS</param>
        /// <returns>экземпляр СКВ</returns>
        public IVersionControlServer VersionControlServerGet(Uri serverUri)
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
        public IWorkspace WorkspaceGet(IVersionControlServer vcs, IWorkspaceInfo wsi)
        {
            var res = new Workspace();
            res.WS = ((VersionControlServer)vcs).VCS.InvokeMethod("GetWorkspace", ((WorkspaceInfo)wsi).WSI);
            return res;
        }
        /// <summary>
        /// Получение объекта, содержащего VersionSpec (опеределение конкретной версии)
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <returns></returns>
        public IVersionSpec WorkspaceVersionSpecCreate(IWorkspace ws)
        {
            var res = new VersionSpec();
            res.VS = tfsVersionControlClientAssembly.CreateInstance("WorkspaceVersionSpec", ((Workspace)ws).WS);
            return res;
        }
        /// <summary>
        /// Получение истории локального элемента
        /// </summary>
        /// <param name="localFileName">Путь к локальному элементу (файл или папка)</param>
        /// <param name="vs">Объект, содержащий VersionSpec (опеределение конкретной версии)</param>
        /// <returns>Сформированный объект запроса к истории</returns>
        public IQueryHistoryParameters QueryHistoryParametersCreate(String localFileName, IVersionSpec vs)
        {
            var RecursionTypeFull = tfsVersionControlClientAssembly.GetEnumValue("RecursionType", "Full");
            var res = new QueryHistoryParameters();
            res.QHP = tfsVersionControlClientAssembly.CreateInstance("QueryHistoryParameters", localFileName, RecursionTypeFull);
            res.QHP.SetPropertyValue("ItemVersion", ((VersionSpec)vs).VS);
            res.QHP.SetPropertyValue("VersionEnd", ((VersionSpec)vs).VS);
            res.QHP.SetPropertyValue("MaxResults", 1);
            return res;
        }
        /// <summary>
        /// Получение информации о наборе изменений в соответствии с параметрами пискового запроса
        /// </summary>
        /// <param name="vsc">СКВ</param>
        /// <param name="qhp">Параметры запроса</param>
        /// <returns>Информация о наборе изменений</returns>
        public Changeset ChangesetGet(IVersionControlServer vsc, IQueryHistoryParameters qhp)
        {
            var cscolobj = ((IEnumerable)((VersionControlServer)vsc).VCS.InvokeMethod("QueryHistory", ((QueryHistoryParameters)qhp).QHP)).GetEnumerator();
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
        public IWorkspace WorkspaceCreate(
            IVersionControlServer vcs,
            String workspaceName,
            String workspaceComment,
            Boolean workspaceLocationOnServer)
        {
            var tpc = ((VersionControlServer)vcs).VCS.GetPropertyValue("TeamProjectCollection");
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
            res.WS = ((VersionControlServer)vcs).VCS.InvokeMethod("CreateWorkspace", cwp);
            return res;
        }
        /// <summary>
        /// Сопоставление в рабочей области пути в СКВ и локальным путям
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <param name="serverPath">Путь на сервере СКВ</param>
        /// <param name="localPath">Локальный путь</param>
        public void WorkspaceMap(
            IWorkspace ws,
            string serverPath,
            string localPath
            )
        {
            ((Workspace)ws).WS.InvokeMethod("Map", serverPath, localPath);
        }
        /// <summary>
        /// Удаление рабочей области
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        public void WorkspaceDelete(IWorkspace ws)
        {
            ((Workspace)ws).WS.InvokeMethod("Delete");
        }


        /// <summary>
        /// Добавить файл в рабочую область
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <param name="localPathFile">Полный путь файла, находящийся в папке, замапленой в рабочей области</param>
        public void WorkspaceAddFile(IWorkspace ws, string localPathFile)
        {
            ((Workspace)ws).WS.InvokeMethod("PendAdd", localPathFile);
        }

        /// <summary>
        /// Извлечение файла для редактирования
        /// </summary>
        /// <param name="ws">Рабояа область</param>
        /// <param name="localPathFile">Полный путь файла, находящийся в папке, замапленой в рабочей области</param>
        /// <returns>true - успешно, false - неуспешно</returns>
        public bool WorkspaceCheckOut(IWorkspace ws, string localPathFile)
        {
            return (int)((Workspace)ws).WS.InvokeMethod("PendEdit", localPathFile) != 0;
        }

        /// <summary>
        /// Возврат изменений в рабочей области
        /// </summary>
        /// <param name="ws">Рабоча область</param>
        /// <param name="commentOnCheckIn">Комментарий для изменения</param>
        /// <param name="numberTasks">Номера задач для чекина</param>
        public void WorkspaceCheckIn(IWorkspace ws, string commentOnCheckIn, List<int> numberTasks = null)
        {
            var gpc = WorkspaceGetPendingChanges(ws);
            var wscp = tfsVersionControlClientAssembly.CreateInstance("WorkspaceCheckInParameters", gpc.PC, commentOnCheckIn);
            if (numberTasks != null)
            {
                // TODO Получить рабочие элементы
                //wscp.AssociatedWorkItems
            }

            ((Workspace)ws).WS.InvokeMethod("CheckIn", wscp);
        }

        private PendingChange WorkspaceGetPendingChanges(IWorkspace ws)
        {
            var res = new PendingChange();
            res.PC = ((Workspace)ws).WS.InvokeMethod("GetPendingChanges");
            return res;
        }
        /// <summary>
        /// Отмена изменений в рабочей области
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        public void WorkspaceUndo(IWorkspace ws)
        {
            var pc = WorkspaceGetPendingChanges(ws);
            int cnt = (int)pc.PC.GetPropertyValue("Length");
            if (cnt == 0)
                return;
            ((Workspace)ws).WS.InvokeMethod("Undo", pc.PC);
        }
        /// <summary>
        /// Получение элементов сопоставления в рабочей области
        /// </summary>
        /// <param name="ws">Опбочая область</param>
        /// <returns>Коллекция, описывающая элементы сопоставления</returns>
        public List<WorkingFolder> WorkspaceFoldersGet(IWorkspace ws)
        {
            var res = new List<WorkingFolder>();
            var flds = ((Workspace)ws).WS.GetPropertyValue("Folders") as IEnumerable;
            foreach (var crnt in flds)
            {
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
        public bool WorkspaceLockFile(IWorkspace ws, string serverPathItem, LockLevel lockLevel)
        {
            bool res = false;
            try
            {
                var llenum = tfsVersionControlClientAssembly.GetEnumValue("LockLevel", lockLevel.ToString());
                res = (int)((Workspace)ws).WS.InvokeMethod("SetLock", serverPathItem, llenum) != 0;
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
        public long WorkspaceGetLastItem(IWorkspace ws, String serverItemPath)
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
            var GetLastFile = ((Workspace)ws).WS.InvokeMethod("Get", GetRequest, GetOptionsValue);

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
        public String ShowDialogChooseServerFolder(IWin32Window parentWindow, IVersionControlServer vcs, String initalPath)
        {
            var dcsf = tfsVersionControlControlsCommonAssembly.CreateInstance("DialogChooseServerFolder", ((VersionControlServer)vcs).VCS, initalPath);

            dcsf.SetPropertyValue("ShowInTaskbar", false);
            var dr = (DialogResult)dcsf.InvokeMethod("ShowDialog", parentWindow);
            if (dr != DialogResult.OK)
                return null;

            return (String)dcsf.GetPropertyValue("CurrentServerItem");
        }
        /// <summary>
        /// Отображение диалога выбора элемента на сервере СКВ
        /// </summary>
        /// <param name="parentWindow">Родительское окно</param>
        /// <param name="vcs">СКВ</param>
        /// <returns>Выбранный элемент сервере. Иначе null.</returns>
        public SelItem ShowDialogChooseItem(IWin32Window parentWindow, IVersionControlServer vcs)
        {
            var typeDialog = tfsVersionControlControlsAssembly.GetTypes().Single(x => x.Name == "DialogChooseItem");
            var consInfo = typeDialog.GetConstructor(
                                   BindingFlags.Instance | BindingFlags.NonPublic,
                                   null,
                                   new Type[] { ((VersionControlServer)vcs).VCS.GetType() },
                                   null);
            var instDialog = (Form)consInfo.Invoke(new object[] { ((VersionControlServer)vcs).VCS });
            var dr = instDialog.ShowDialog(parentWindow);
            if (dr != DialogResult.OK)
                return null;

            var selitemprpinfo = typeDialog.GetProperty("SelectedItem", BindingFlags.Instance | BindingFlags.NonPublic);
            var selectedItem = selitemprpinfo.GetValue(instDialog, null);

            var res = new SelItem();
            res.Path = selectedItem.GetPropertyValue("ServerItem") as String;
            var it = selectedItem.GetPropertyValue("ItemType");
            res.ItemType = (ItemType)Enum.Parse(typeof(ItemType), it.ToString());

            return res;
        }

        /// <summary>
        /// Получение древовидного представления запросов. Папки, в которых ничего нет не выводятся.
        /// </summary>
        /// <param name="serverUri">Uri к TFS</param>
        /// <param name="serverItemPath">Путь к элементу на сервере СКВ</param>
        /// <returns></returns>
        public List<QueryItemNode> QueryItemsGet(Uri serverUri, String serverItemPath)
        {
            var res = new List<QueryItemNode>();
            var vc = VersionControlServerGet(serverUri);
            var projectNoServerPath = ((VersionControlServer)vc).VCS.InvokeMethod("GetTeamProjectForServerPath", serverItemPath);
            String projectName = projectNoServerPath.GetPropertyValue("Name") as String;

            var tpc = TeamProjectCollectionGet(serverUri);
            Type workItemStoreType = tfsWorkItemTrackingClientAssembly.
#if NET40
                GetTypes()
#else
            ExportedTypes
#endif
            .Single(x => x.Name == "WorkItemStore");

            var wis = tpc.InvokeMethod("GetService", workItemStoreType);
            var prs = wis.GetPropertyValue("Projects") as ICollection;

            IEnumerable wip = null;

            foreach (object pr in prs)
            {
                if ((pr.GetPropertyValue("Name") as String) != projectName)
                    continue;
                wip = pr.GetPropertyValue("QueryHierarchy") as IEnumerable;
                break;
            }

            if (wip == null)
                throw new ArgumentException("Не удалось определить проект");


            Action<QueryItemNode, object> recSeek = null;

            recSeek = new Action<QueryItemNode, object>((qin, itm) =>
                {
                    qin.Name = itm.GetPropertyValue("Name") as String;
                    qin.ProjectName = projectName;
                    IEnumerable folder = itm as IEnumerable;
                    if (folder == null)
                        qin.QueryID = (Guid)itm.GetPropertyValue("Id");

                    if (qin.IsFolder)
                    {
                        foreach (var itemInfolder in folder)
                        {

                            if (((itemInfolder as IEnumerable) != null) &&
                                ((int)itemInfolder.GetPropertyValue("Count")) == 0)
                                continue;

                            var childEl = new QueryItemNode();

                            recSeek(childEl, itemInfolder);
                            qin.ChildNodes.Add(childEl);
                        }
                    }
                });


            foreach (var item in wip)
            {
                if (((int)item.GetPropertyValue("Count")) == 0)
                    continue;

                var nitmNode = new QueryItemNode();

                recSeek(nitmNode, item);
                res.Add(nitmNode);
            }

            return res;
        }

        /// <summary>
        /// Получение рабочих элементов по сохраненному запросу
        /// </summary>
        /// <param name="serverUri">Uri сервера TFS</param>
        /// <param name="queryitem">экземпляр запроса</param>
        /// <returns>Коллекция рабочих элементов</returns>
        public List<WorkItem> WorkItemsFromQueryGet(Uri serverUri, QueryItemNode queryitem)
        {
            var res = new List<WorkItem>();

            if (queryitem.IsFolder)
                return res;

            var tpc = TeamProjectCollectionGet(serverUri);
            Type workItemStoreType = tfsWorkItemTrackingClientAssembly.
#if NET40
                GetTypes()
#else
            ExportedTypes
#endif
            .Single(x => x.Name == "WorkItemStore");

            var wis = tpc.InvokeMethod("GetService", workItemStoreType);
            var prs = wis.GetPropertyValue("Projects") as ICollection;

            object query = wis.InvokeMethod("GetQueryDefinition", queryitem.QueryID.Value);

            if (query == null)
                throw new ArgumentOutOfRangeException("Не найден запрос с именем " + queryitem.Name);

            String queryText = query.GetPropertyValue("QueryText") as String;

            Dictionary<String, String> variables = new Dictionary<string, string>();
            variables.Add("project", queryitem.ProjectName);



            queryText = "SELECT [System.Id], [System.Title] " + queryText.SubString(queryText.IndexOf(" FROM "));

            var wims = wis.InvokeMethod("Query", queryText, variables) as IEnumerable;
            foreach (var wim in wims)
            {
                var wi = new WorkItem();
                wi.ID = (int)wim.GetPropertyValue("Id");
                wi.Title = wim.GetPropertyValue("Title") as String;
                res.Add(wi);
            }

            return res;

        }

        /// <summary>
        /// Получение рабочего элемента ло его Id
        /// </summary>
        /// <param name="serverUri">Uri сервера TFS</param>
        /// <param name="id">Id рабочего элемента</param>
        /// <returns></returns>
        public WorkItem WorkItemByIdGet(Uri serverUri, int id)
        {
            var tpc = TeamProjectCollectionGet(serverUri);
            Type workItemStoreType = tfsWorkItemTrackingClientAssembly.
#if NET40
                GetTypes()
#else
            ExportedTypes
#endif
            .Single(x => x.Name == "WorkItemStore");

            var wis = tpc.InvokeMethod("GetService", workItemStoreType);
            var wim = wis.InvokeMethod("GetWorkItem", id);
            var wi = new WorkItem();
            wi.ID = (int)wim.GetPropertyValue("Id");
            wi.Title = wim.GetPropertyValue("Title") as String;

            return wi;
        }

        /// <summary>
        /// Скачать файл из СКВ
        /// </summary>
        /// <param name="vcs">СКВ</param>
        /// <param name="serverPath">Путь к фойлу на серврере СКВ</param>
        /// <param name="localFile">Путь к локальному расположению файла</param>
        public void VersionControlServerDownloadFile(IVersionControlServer vcs, String serverPath, String localFile)
        {
            ((VersionControlServer)vcs).VCS.InvokeMethod("DownloadFile", serverPath, localFile);
        }
    }
}


