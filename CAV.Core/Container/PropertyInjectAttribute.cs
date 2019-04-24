using System;

namespace Cav
{
    /// <summary>
    /// Пометка свойсва как точки иньекции зависимости для локатора.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyInjectAttribute : Attribute { }
}
