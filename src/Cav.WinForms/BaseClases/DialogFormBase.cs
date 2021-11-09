using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Cav.WinForms.BaseClases
{
    /// <summary>
    /// Базовое окно для диалоговых окон. С кнопками ок и отмена.
    /// </summary>
    public partial class DialogFormBase : Form
    {
        /// <summary>
        /// конструктор по умолчанию
        /// </summary>
        public DialogFormBase()
        {
            InitializeComponent();

            Focus();
        }

        /// <summary>
        /// Компонент находится в режиме дизайнера
        /// </summary>
        protected Boolean IsDesignMode => DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }
}
