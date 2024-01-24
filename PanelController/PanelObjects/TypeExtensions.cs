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

        public static IPanelObject CreatePanelObject(this Type type)
        {
            if (!type.Implements<IPanelObject>())
                throw new ArgumentException("\"type\" must implement IPanelObject");
            if (Activator.CreateInstance(type) is not IPanelObject obj)
                throw new InvalidProgramException("Instance was not a IPanelObject");
            return obj;
        }
    }
}
