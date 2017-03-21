using System;
using System.ComponentModel;

namespace Cav.BaseClases
{
    /// <summary>
    /// Базовый класс для реализации бизнесс-логик
    /// </summary>
    [Obsolete("Будет удалено. Используйте внедрение зависимости в конструктор и контейнер Cav.Container.Locator", true)]
    // TODO Удалить
    public class BusinessLogicBase : Component
    {
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
    }
}
