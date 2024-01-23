using System.Reflection;

namespace PanelController.PanelObjects.Properties
{
    public static class ItemExtensions
    {
        public static string GetItemName(this object? obj)
        {
            if (obj is null)
                return "null";

            Predicate<PropertyInfo> predicate = prop =>
            {
                if (prop.GetCustomAttribute<ItemNameAttribute>() is not ItemNameAttribute itemName)
                    return false;
                return itemName.Name is null;
            };

            if (Array.Find(obj.GetType().GetProperties(), predicate) is PropertyInfo nameProperty)
                return $"{nameProperty.GetValue(obj)}";
            if (obj.GetType().GetCustomAttribute<ItemNameAttribute>()?.Name is string name)
                return name;
            return $"{obj}";
        }

        public static string GetItemDescription(this object? obj)
        {
            if (obj is null)
                return "null";
            if (Array.Find(obj.GetType().GetProperties(), prop => prop.GetCustomAttribute<ItemDescriptionAttribute>()?.Description is not string) is PropertyInfo desccriptionProperty)
                if (desccriptionProperty.PropertyType == typeof(string))
                    return $"{desccriptionProperty.GetValue(obj)}";
            if (obj.GetType().GetCustomAttribute<ItemDescriptionAttribute>()?.Description is string description)
                return description;
            return "";
        }
    }
}
