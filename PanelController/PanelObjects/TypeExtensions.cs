using System.Reflection;
using PanelController.PanelObjects.Properties;

namespace PanelController.PanelObjects
{
    public static class TypeExtensions
    {
        public static bool Implements<T>(this Type type)
        {
            if (!typeof(T).IsInterface)
                return false;
            return typeof(T).IsAssignableFrom(type);
        }

        public static string GetItemName(this Type type)
        {
            if (type.GetCustomAttribute<ItemNameAttribute>()?.Name is string name)
                return name;
            return type.Name;
        }
    }
}
