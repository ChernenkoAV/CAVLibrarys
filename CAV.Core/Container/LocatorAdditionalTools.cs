using System;

namespace Cav
{
    /// <summary>
    /// Пометка свойсва как точки иньекции зависимости для локатора. Наследуется.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyInjectAttribute : Attribute { }

    /// <summary>
    /// Пометка класса для обозначения, что экземляр не хранить в кэше (не использовать сингтон). Не наследуется.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    [Obsolete("Для корректной работы необходимо присвоить Locator.UseCache = false")]
    public sealed class AlwaysNewAttribute : Attribute { }

    /// <summary>
    /// Интерфейс для возможности реализации инициализации экземпляра
    /// </summary>
    public interface IInitInstance
    {
        /// <summary>
        /// Метод инициализации экземпляра
        /// </summary>
        void InitInstance();
    }


}
