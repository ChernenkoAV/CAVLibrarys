using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Cav.ProgramSettins
{
    /// <summary>
    /// Область сохранения настрек
    /// </summary>
    public enum Area
    {
        /// <summary>
        /// Для пользователя
        /// </summary>
        User,
        /// <summary>
        /// Для приложения (В папке сборки)
        /// </summary>
        App,
        /// <summary>
        /// Общее хранилице для всех пользователей
        /// </summary>
        CommonApp
    }

    /// <summary>
    /// Базовый класс для хранения настроек
    /// </summary>    
    /// <example>
    /// Пример использования
    /// <code>
    /// public class ProgramSetting : ProgramSettingsBase
    /// {
    ///     private static ProgramSetting instance = null;
    /// 
    ///     public static ProgramSetting Instance
    ///     {
    ///         get
    ///         {
    ///             if (instance == null)
    ///                 instance = ProgramSettingsBase.Create&lt;ProgramSetting&gt;();
    /// 
    ///             return instance;
    ///         }
    ///     }
    ///     
    ///     public String OutputDir
    ///     {
    ///         get { return this.GetValue&lt;String&gt;("OutputDir"); }
    ///         set { this.SetValue(Area.User, "OutputDir", value); }
    ///     }
    /// }    
    /// </code>
    /// </example>

    // TODO Удалить
    [Obsolete("Будет удалено. Используйте Cav.Configuration.ProgramSettingsBase<>", true)]
    public abstract class ProgramSettingsBase : INotifyPropertyChanged, INotifyPropertyChanging
    {
        /// <summary>
        /// Создание экземпляра настроек
        /// </summary>
        /// <typeparam name="T">Класс настроек</typeparam>
        /// <param name="FileName">Имя файла. Если null = полное имя типа класса, в котором находится свойство, с расширением .prstg. Путь в зависимости от <c>Area</c> свойств</param>
        /// <returns>созданый экземпляр</returns>
        protected static T Create<T>(String FileName = null) where T : ProgramSettingsBase
        {
            ProgramSettingsBase res = (ProgramSettingsBase)Activator.CreateInstance(typeof(T));

            if (FileName.IsNullOrWhiteSpace())
                FileName = typeof(T).FullName + ".prstg";

            res.fileNameApp = Path.Combine(Path.GetDirectoryName(typeof(T).Assembly.Location), FileName);
            res.fileNameUser = Path.Combine(DomainContext.AppDataUserStorage, FileName);
            res.fileNameAppCommon = Path.Combine(DomainContext.AppDataCommonStorage, FileName);
            res.Reload();

            return (T)res;
        }

        /// <summary>
        /// Элемент настроек
        /// </summary>
        public class SetItem
        {
            /// <summary>
            /// Для сериализатора
            /// </summary>
            public SetItem() { }

            /// <summary>
            /// </summary>
            /// <param name="PropName">Имя свойства</param>
            public SetItem(String PropName)
            {
                this.PropName = PropName;
            }

            /// <summary>
            /// Имя свойства
            /// </summary>
            public String PropName;

            /// <summary>
            /// Область хранения
            /// </summary>
            public Area AreaVal;

            /// <summary>
            /// Сериализованонное значение свойства
            /// </summary>
            public String SelzeVal;

            /// <summary>
            /// Значение свойства(типа кэш)
            /// </summary>
            [XmlIgnore]
            public Object ValVal;
        }

        private String fileNameApp = null;
        private String fileNameUser = null;
        private String fileNameAppCommon = null;

        private object lockobj = new object();
        private List<SetItem> storage = new List<SetItem>();

        /// <summary>
        /// Получения значения свойства по его имени 
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="PropName">Имя свойства</param>
        /// <returns></returns>
        protected T GetValue<T>(String PropName)
        {
            T res = default(T);
            lock (lockobj)
            {
                SetItem item = storage.FirstOrDefault(x => x.PropName == PropName);
                if (item != null)
                    if (item.ValVal != null)
                        res = (T)item.ValVal;
                    else
                        if (!item.SelzeVal.IsNullOrWhiteSpace())
                    {
                        item.ValVal = item.SelzeVal.XMLDeserialize<T>();
                        res = (T)item.ValVal;
                    }
            }

            return res;
        }

        /// <summary>
        /// Сохранение значения в экземпляре настроек
        /// </summary>
        /// <param name="AreaProp">область хранения</param>
        /// <param name="PropName">Имя свойства</param>
        /// <param name="Value">Значение свойства</param>
        protected void SetValue(Area AreaProp, String PropName, Object Value)
        {

            lock (lockobj)
            {
                SetItem item = storage.FirstOrDefault(x => x.PropName == PropName);
                if (item == null)
                {
                    item = new SetItem(PropName);
                    storage.Add(item);
                }

                if (item.SelzeVal == null & Value == null)
                    return;

                if ((item.ValVal != null & Value != null) && item.ValVal.Equals(Value))
                    return;


                NotifyPropertyChanging(PropName);

                item.AreaVal = AreaProp;
                item.SelzeVal = Value == null ? null : Value.XMLSerialize().ToString();
                item.ValVal = Value;

                NotifyPropertyChanged(PropName);
            }
        }

        /// <summary>
        /// Загрузить все настройки заново
        /// </summary>
        public void Reload()
        {
            lock (lockobj)
            {
                storage.Clear();
                if (File.Exists(fileNameApp))
                    foreach (var item in fileNameApp.XMLDeserializeFromFile<List<SetItem>>())
                    {
                        if (storage.Any(x => x.PropName == item.PropName))
                            continue;
                        NotifyPropertyChanging(item.PropName);
                        storage.Add(item);
                        NotifyPropertyChanged(item.PropName);
                    }
                if (File.Exists(fileNameAppCommon))
                    foreach (var item in fileNameAppCommon.XMLDeserializeFromFile<List<SetItem>>())
                    {
                        if (storage.Any(x => x.PropName == item.PropName))
                            continue;
                        NotifyPropertyChanging(item.PropName);
                        storage.Add(item);
                        NotifyPropertyChanged(item.PropName);
                    }
                if (File.Exists(fileNameUser))
                    foreach (var item in fileNameUser.XMLDeserializeFromFile<List<SetItem>>())
                    {
                        if (storage.Any(x => x.PropName == item.PropName))
                            continue;
                        NotifyPropertyChanging(item.PropName);
                        storage.Add(item);
                        NotifyPropertyChanged(item.PropName);
                    }
            }
        }

        /// <summary>
        /// Сохранить настройки
        /// </summary>
        public void Save()
        {
            lock (lockobj)
            {

                if (storage.Any(x => x.AreaVal == Area.User))
                    storage.Where(x => x.AreaVal == Area.User).ToList().XMLSerialize(fileNameUser);
                else
                    File.Delete(fileNameUser);

                if (storage.Any(x => x.AreaVal == Area.CommonApp))
                    storage.Where(x => x.AreaVal == Area.CommonApp).ToList().XMLSerialize(fileNameAppCommon);
                else
                    File.Delete(fileNameAppCommon);

                if (storage.Any(x => x.AreaVal == Area.App))
                    storage.Where(x => x.AreaVal == Area.App).ToList().XMLSerialize(fileNameApp);
                else
                    File.Delete(fileNameApp);
            }

        }

        #region Члены INotifyPropertyChanging
        /// <summary/>
        public event PropertyChangingEventHandler PropertyChanging;

        private void NotifyPropertyChanging(String PName)
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(PName));
        }

        #endregion

        #region Члены INotifyPropertyChanged
        /// <summary/>
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String PName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(PName));
        }

        #endregion
    }
}