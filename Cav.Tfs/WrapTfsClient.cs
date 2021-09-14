using Cav.ReflectHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

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
    public sealed class WorkItem
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
        internal QueryItemNode() { }

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
        public ReadOnlyCollection<QueryItemNode> ChildNodes { get; internal set; }
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
    public sealed class WorkspaceInfo
    {
        internal WorkspaceInfo() { }
        internal Object WSI { get; set; }
    }

    /// <summary>
    /// Объект, содержащий VersionControlServer
    /// </summary>
    public sealed class VersionControlServer
    {
        internal VersionControlServer() { }
        internal Object VCS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий Workspace
    /// </summary>
    public sealed class Workspace
    {
        internal Workspace() { }
        internal Object WS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий VersionSpec
    /// </summary>    
    public sealed class VersionSpec
    {
        internal VersionSpec() { }
        internal Object VS { get; set; }
    }
    /// <summary>
    /// Объект, содержащий QueryHistoryParameters
    /// </summary>
    public sealed class QueryHistoryParameters
    {
        internal QueryHistoryParameters() { }
        internal Object QHP { get; set; }
    }
    /// <summary>
    /// Информация об Changeset
    /// </summary>
    public sealed class Changeset
    {
        internal Changeset() { }

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
    public sealed class PendingChange
    {
        internal PendingChange() { }
        internal Object PC { get; set; }
    }
    /// <summary>
    /// Информация о сопоставлении пути на сервере и локальном пути
    /// </summary>
    public sealed class WorkingFolder
    {
        internal WorkingFolder() { }

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

    /// <summary>
    /// Данные шельвы
    /// </summary>
    public sealed class ShelveSet
    {
        internal ShelveSet() { }

        /// <summary>
        /// Комментарий
        /// </summary>
        public String Comment { get; internal set; }
        /// <summary>
        /// Наименование
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// ID рабочих элементов, ассоциированных с шельвой
        /// </summary>
        public ReadOnlyCollection<int> AssociatedWorkItemsIDs { get; internal set; }
    }


    #endregion

    /// <summary>
    /// Обертка к сботкам TFS
    /// </summary>
    public class WrapTfs
    {
        private const string tfsClientServer15 = @"c:\Program Files\Common Files\microsoft shared\Team Foundation Server\15.0\";


        private const string tfsPrefix = "Microsoft.TeamFoundation.";
        private const string tfsClientDll = tfsPrefix + "Client.dll";
        private const string tfsVersionControlClientDll = tfsPrefix + "VersionControl.Client.dll";
        private const string tfsVersionControlCommonDll = tfsPrefix + "VersionControl.Common.dll";
        private const string tfsVersionControlControlsCommonDll = tfsPrefix + "VersionControl.Controls.Common.dll";
        private const string tfsVersionControlControlsDll = tfsPrefix + "VersionControl.Controls.dll";
        private const string tfsWorkItemTrackingClientDll = tfsPrefix + "WorkItemTracking.Client.dll";

        private static Assembly tfsClientAssembly = null;
        private static Assembly tfsVersionControlCommonAssembly = null;
        private static Assembly tfsVersionControlClientAssembly = null;
        private static Assembly tfsVersionControlControlsCommonAssembly = null;
        private static Assembly tfsVersionControlControlsAssembly = null;
        private static Assembly tfsWorkItemTrackingClientAssembly = null;

        private void initAssebly()
        {
            if (tfsVersionControlControlsAssembly != null)
                return;

            var vsDirs = Directory.EnumerateDirectories(@"C:\Program Files (x86)", "Microsoft Visual Studi*", SearchOption.TopDirectoryOnly);

            if (!vsDirs.Any())
                throw new InvalidOperationException("Не найдено установленной Visual Studio");

            var teDirs = vsDirs
                .SelectMany(x => Directory.GetFiles(x, tfsVersionControlControlsDll, SearchOption.AllDirectories))
                .ToList();

            if (!teDirs.Any())
                throw new InvalidOperationException("Не найдено расширение 'Team Explorer' в Visual Studio");

            var pathTfsdll = teDirs.OrderBy(x => x).Last();

            pathTfsdll = Path.GetDirectoryName(pathTfsdll);

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            tfsClientAssembly = Assembly.LoadFile(Path.Combine(pathTfsdll, tfsClientDll));
            tfsVersionControlClientAssembly = Assembly.LoadFile(Path.Combine(pathTfsdll, tfsVersionControlClientDll));
            tfsVersionControlControlsAssembly = Assembly.LoadFile(Path.Combine(pathTfsdll, tfsVersionControlControlsDll));

            try
            {
                tfsVersionControlControlsCommonAssembly = Assembly.LoadFile(Path.Combine(pathTfsdll, tfsVersionControlControlsCommonDll));
            }
            catch
            {
                //В 2019 студии объединили сборки.
                tfsVersionControlControlsCommonAssembly = tfsVersionControlControlsAssembly;
            }

            tfsVersionControlCommonAssembly = Assembly.LoadFile(Path.Combine(pathTfsdll, tfsVersionControlCommonDll));
            tfsWorkItemTrackingClientAssembly = Assembly.LoadFile(Path.Combine(pathTfsdll, tfsWorkItemTrackingClientDll));
        }


        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var asly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            if (asly != null)
                return asly;

            var pathTfsdll = Path.GetDirectoryName(args.RequestingAssembly.Location);

            var assemblyName = new AssemblyName(args.Name);
            string assemblyFile = assemblyName.Name + ".dll";

            var path = Path.Combine(pathTfsdll, assemblyFile);

            if (!File.Exists(path))
                path = Path.Combine(tfsClientServer15, assemblyFile);

            if (!File.Exists(path))
            {
                var targetculture = assemblyName.CultureInfo.TwoLetterISOLanguageName;
                if (targetculture == null)
                    throw new FileLoadException($"Not compute culture for {args.Name}");
                path = Path.Combine(Path.Combine(pathTfsdll, targetculture), assemblyFile);
            }
            return Assembly.LoadFile(path);

        }

        private Object ws = null;
        private Object WorkstationGet()
        {
            initAssebly();

            if (ws != null)
                return ws;
            ws = tfsVersionControlClientAssembly.GetStaticOrConstPropertyOrFieldValue("Workstation", "Current");
            return ws;
        }
        /// <summary>
        /// Обновить кэш
        /// </summary>
        public void WorkstationReloadCache()
        {
            initAssebly();

            WorkstationGet().InvokeMethod("ReloadCache");
        }

        private Object TeamProjectCollectionGet(Uri serverUri)
        {
            initAssebly();

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
        /// <param name="wsi">Объект, содержащий информацию о рабочей области</param>
        /// <returns>экземпляр СКВ</returns>
        public VersionControlServer VersionControlServerGet(WorkspaceInfo wsi)
        {
            Uri servUri = (Uri)wsi.WSI.GetPropertyValue("ServerUri");
            return VersionControlServerGet(servUri);
        }
        /// <summary>
        /// Получение экземпляра управления СКВ        
        /// </summary>
        /// <param name="serverUri">Uri к серверу TFS</param>
        /// <returns>экземпляр СКВ</returns>
        public VersionControlServer VersionControlServerGet(Uri serverUri)
        {
            var tpc = TeamProjectCollectionGet(serverUri);
            Type vcsType = tfsVersionControlClientAssembly.
                ExportedTypes.Single(x => x.Name == "VersionControlServer");

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
        public void WorkspaceCheckIn(Workspace ws, string commentOnCheckIn, IEnumerable<int> numberTasks = null)
        {
            var gpc = WorkspaceGetPendingChanges(ws);
            var wscp = tfsVersionControlClientAssembly.CreateInstance("WorkspaceCheckInParameters", gpc.PC, commentOnCheckIn);
            if ((numberTasks ?? new List<int>()).Any())
            {
                var tpc = ws.WS
                    .GetPropertyValue("VersionControlServer")
                    .GetPropertyValue("TeamProjectCollection");
                Type workItemStoreType = tfsWorkItemTrackingClientAssembly.
                    ExportedTypes.Single(x => x.Name == "WorkItemStore");

                var wis = tpc.InvokeMethod("GetService", workItemStoreType);

                var associate = tfsVersionControlClientAssembly.GetEnumValue("WorkItemCheckinAction", "Associate");

                Type workItemCheckinInfoType = tfsVersionControlClientAssembly.
                    ExportedTypes.Single(x => x.Name == "WorkItemCheckinInfo");

                List<Object> associatedWorkItems = new List<object>();
                foreach (var idTask in numberTasks)
                {
                    var wim = wis.InvokeMethod("GetWorkItem", idTask);
                    var wici = tfsVersionControlClientAssembly.CreateInstance("WorkItemCheckinInfo", wim, associate);
                    associatedWorkItems.Add(wici);
                }

                Array associatedWorkItemsArray = Array.CreateInstance(workItemCheckinInfoType, associatedWorkItems.Count);

                for (int i = 0; i < associatedWorkItems.Count; i++)
                    associatedWorkItemsArray.SetValue(Convert.ChangeType(associatedWorkItems[i], workItemCheckinInfoType), i);


                wscp.SetPropertyValue("AssociatedWorkItems", associatedWorkItemsArray);
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
        public ReadOnlyCollection<WorkingFolder> WorkspaceFoldersGet(Workspace ws)
        {
            var res = new List<WorkingFolder>();
            var flds = ws.WS.GetPropertyValue("Folders") as IEnumerable;
            foreach (var crnt in flds)
            {
                var fi = new WorkingFolder();
                fi.ServerItem = (String)crnt.GetPropertyValue("ServerItem");
                fi.LocalItem = (String)crnt.GetPropertyValue("LocalItem");
                fi.IsCloaked = (Boolean)crnt.GetPropertyValue("IsCloaked");
                res.Add(fi);
            }

            return new ReadOnlyCollection<WorkingFolder>(res);
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

            var VersionSpecLatest = tfsVersionControlClientAssembly.GetStaticOrConstPropertyOrFieldValue("VersionSpec", "Latest");

            var GetRequest = tfsVersionControlClientAssembly.CreateInstance("GetRequest", ItemSpec, VersionSpecLatest);

            var GetOptionsType = tfsVersionControlClientAssembly.
                ExportedTypes.Single(x => x.Name == "GetOptions");

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
            initAssebly();

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
        /// <summary>
        /// Отображение диалога выбора элемента на сервере СКВ
        /// </summary>
        /// <param name="parentWindow">Родительское окно</param>
        /// <param name="vcs">СКВ</param>
        /// <returns>Выбранный элемент сервере. Иначе null.</returns>
        public SelItem ShowDialogChooseItem(IWin32Window parentWindow, VersionControlServer vcs)
        {
            var typeDialog = tfsVersionControlControlsAssembly.GetType("Microsoft.TeamFoundation.VersionControl.Controls.DialogChooseItem");
            var consInfo = typeDialog.GetConstructor(
                                   BindingFlags.Instance | BindingFlags.NonPublic,
                                   null,
                                   new Type[] { vcs.VCS.GetType() },
                                   null);
            var instDialog = (Form)consInfo.Invoke(new object[] { vcs.VCS });
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
        public ReadOnlyCollection<QueryItemNode> QueryItemsGet(Uri serverUri, String serverItemPath)
        {
            var res = new List<QueryItemNode>();
            var vc = VersionControlServerGet(serverUri);
            var projectNoServerPath = vc.VCS.InvokeMethod("GetTeamProjectForServerPath", serverItemPath);
            String projectName = projectNoServerPath.GetPropertyValue("Name") as String;

            var tpc = TeamProjectCollectionGet(serverUri);
            Type workItemStoreType = tfsWorkItemTrackingClientAssembly.
                ExportedTypes.Single(x => x.Name == "WorkItemStore");

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


            Func<object, QueryItemNode> recSeek = null;

            recSeek = new Func<object, QueryItemNode>((itm) =>
                {
                    var qin = new QueryItemNode();

                    qin.Name = itm.GetPropertyValue("Name") as String;
                    qin.ProjectName = projectName;
                    IEnumerable folder = itm as IEnumerable;
                    if (folder == null)
                        qin.QueryID = (Guid)itm.GetPropertyValue("Id");

                    List<QueryItemNode> childEls = new List<QueryItemNode>();

                    if (qin.IsFolder)
                    {
                        foreach (var itemInfolder in folder)
                        {

                            if (((itemInfolder as IEnumerable) != null) &&
                                ((int)itemInfolder.GetPropertyValue("Count")) == 0)
                                continue;
                            var childEl = recSeek(itemInfolder);
                            if (childEl != null)
                                childEls.Add(childEl);
                        }
                    }
                    else
                    {
                        if (itm.GetPropertyValue("QueryType").ToString() != "List")
                            return null;
                    }

                    qin.ChildNodes = new ReadOnlyCollection<QueryItemNode>(childEls);

                    if (qin.IsFolder && !qin.ChildNodes.Any())
                        return null;

                    return qin;
                });


            foreach (var item in wip)
            {
                if (((int)item.GetPropertyValue("Count")) == 0)
                    continue;

                var nitmNode = recSeek(item);
                if (nitmNode != null)
                    res.Add(nitmNode);
            }

            return new ReadOnlyCollection<QueryItemNode>(res);
        }

        /// <summary>
        /// Получение рабочих элементов по сохраненному запросу
        /// </summary>
        /// <param name="serverUri">Uri сервера TFS</param>
        /// <param name="queryitem">экземпляр запроса</param>
        /// <returns>Коллекция рабочих элементов</returns>
        public ReadOnlyCollection<WorkItem> WorkItemsFromQueryGet(Uri serverUri, QueryItemNode queryitem)
        {
            var res = new List<WorkItem>();

            if (queryitem.IsFolder)
                return new ReadOnlyCollection<WorkItem>(res);

            var tpc = TeamProjectCollectionGet(serverUri);
            Type workItemStoreType = tfsWorkItemTrackingClientAssembly.
                ExportedTypes.Single(x => x.Name == "WorkItemStore");

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

            return new ReadOnlyCollection<WorkItem>(res);

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
                ExportedTypes.Single(x => x.Name == "WorkItemStore");

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
        public void VersionControlServerDownloadFile(VersionControlServer vcs, String serverPath, String localFile)
        {
            vcs.VCS.InvokeMethod("DownloadFile", serverPath, localFile);
        }

        /// <summary>
        /// Получение шельв текущего юзера
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<ShelveSet> ShelvesetsCurrenUserLoad(VersionControlServer vcs)
        {
            var authenticatedUser = tfsVersionControlCommonAssembly.GetStaticOrConstPropertyOrFieldValue("RepositoryConstants", "AuthenticatedUser");

            var mi = vcs.VCS.GetType().GetMethod("QueryShelvesets", new[] { typeof(string), typeof(string) });
            var ssts = mi.Invoke(vcs.VCS, new object[] { null, authenticatedUser }) as IEnumerable;

            var res = new List<ShelveSet>();

            foreach (var item in ssts)
            {
                var ss = new ShelveSet();
                ss.Comment = item.GetPropertyValue("Comment") as String;
                ss.Name = item.GetPropertyValue("Name") as String;

                var wiids = new List<int>();

                var awis = item.GetPropertyValue("AssociatedWorkItems") as IEnumerable;
                foreach (var awi in awis)
                    wiids.Add((int)awi.GetPropertyValue("Id"));

                ss.AssociatedWorkItemsIDs = new ReadOnlyCollection<int>(wiids);

                res.Add(ss);
            }

            return new ReadOnlyCollection<ShelveSet>(res);
        }

        /// <summary>
        /// Положить изменения рабочей область в шельву
        /// </summary>
        /// <param name="ws">Рабочая область</param>
        /// <param name="nameShelvset">Наименование шельвы</param>
        /// <param name="commentShelvset">Коммент к шельве</param>
        /// <param name="numberTasks">Привязанные рабочие элементы</param>
        public void WorkspaceShelvesetCreate(Workspace ws, string nameShelvset, string commentShelvset, IEnumerable<int> numberTasks = null)
        {
            if (nameShelvset.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(nameShelvset));

            numberTasks = numberTasks ?? new List<int>();

            var pcs = ws.WS.InvokeMethod("GetPendingChanges") as ICollection;

            if (pcs.Count == 0)
                return;

            var vcs = ws.WS.GetPropertyValue("VersionControlServer");

            var authenticatedUser = tfsVersionControlCommonAssembly.GetStaticOrConstPropertyOrFieldValue("RepositoryConstants", "AuthenticatedUser");

            var newShelveset = tfsVersionControlClientAssembly.CreateInstance("Shelveset", vcs, nameShelvset, authenticatedUser);

            if (!commentShelvset.IsNullOrWhiteSpace())
                newShelveset.SetPropertyValue("Comment", commentShelvset);

            var associatedWorkItems = new List<object>();

            if (numberTasks.Any())
            {
                var associate = tfsVersionControlClientAssembly.GetEnumValue("WorkItemCheckinAction", "Associate");

                Type workItemStoreType = tfsWorkItemTrackingClientAssembly.
                   ExportedTypes.Single(x => x.Name == "WorkItemStore");

                var wis = vcs.GetPropertyValue("TeamProjectCollection").InvokeMethod("GetService", workItemStoreType);
                Type workItemCheckinInfoType = tfsVersionControlClientAssembly.
                    ExportedTypes.Single(x => x.Name == "WorkItemCheckinInfo");

                foreach (var idTask in numberTasks)
                {
                    var wim = wis.InvokeMethod("GetWorkItem", idTask);
                    var wici = tfsVersionControlClientAssembly.CreateInstance("WorkItemCheckinInfo", wim, associate);
                    associatedWorkItems.Add(wici);
                }

                Array associatedWorkItemsArray = Array.CreateInstance(workItemCheckinInfoType, associatedWorkItems.Count);

                for (int i = 0; i < associatedWorkItems.Count; i++)
                    associatedWorkItemsArray.SetValue(Convert.ChangeType(associatedWorkItems[i], workItemCheckinInfoType), i);

                newShelveset.SetPropertyValue("WorkItemInfo", associatedWorkItemsArray);
            }

            ws.WS.InvokeMethod("Shelve", newShelveset, pcs, tfsVersionControlClientAssembly.GetEnumValue("ShelvingOptions", "None"));

        }

        /// <summary>
        /// Удаление шельвы
        /// </summary>
        /// <param name="vcs">СКВ</param>
        /// <param name="nameShelvset">Имя шельвы</param>
        public void ShelvesetDelete(VersionControlServer vcs, string nameShelvset)
        {
            var authenticatedUser = tfsVersionControlCommonAssembly.GetStaticOrConstPropertyOrFieldValue("RepositoryConstants", "AuthenticatedUser");

            var mi = vcs.VCS.GetType().GetMethod("QueryShelvesets", new[] { typeof(string), typeof(string) });
            var ssts = mi.Invoke(vcs.VCS, new object[] { null, authenticatedUser }) as IEnumerable;

            object shset = null;

            foreach (var item in ssts)
            {
                if ((item.GetPropertyValue("Name") as string) == nameShelvset)
                {
                    shset = item;
                    break;
                }
            }

            if (shset != null)
                vcs.VCS.InvokeMethod("DeleteShelveset", shset);
        }

    }


}



