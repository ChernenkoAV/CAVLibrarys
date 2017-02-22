using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Cav.WinForms.BaseClases
{
    /// <summary>
    /// Базовый клас модуля интерфейса (UserControl)
    /// </summary>
    public partial class UserControlBase : UserControl
    {
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public UserControlBase()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Компонент находится в режиме дизайнера
        /// </summary>
        protected Boolean IsDesignMode
        {
            get
            {
                return this.DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
            }
        }

        /// <summary>
        /// Метод для обновления состояния модуля пользовательского интерфейса
        /// </summary>
        public virtual void RefreshControl() { }

    }
}
