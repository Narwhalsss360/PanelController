using System.Reflection;

namespace PanelController.PanelObjects.Properties
{
    public static class ItemExtensions
    {
        public static bool IsUserProperty(this PropertyInfo property)
        {
            return property.GetCustomAttribute<UserPropertyAttribute>() is not null;
        }

        public static PropertyInfo[] GetUserProperties(this Type type)
        {
            List<PropertyInfo> properties = new();
            foreach (PropertyInfo property in type.GetProperties())
                if (property.IsUserProperty())   
                    properties.Add(property);
            return properties.ToArray();
        }

        public static PropertyInfo[] GetUserProperties(this IPanelObject @object) => GetUserProperties(@object.GetType());

        public static Dictionary<PropertyInfo, object?> GetAllPropertiesValues(this IPanelObject? @object)
        {
            Dictionary<PropertyInfo, object?> propertyValuePairs = new();
            if (@object is null)
                return propertyValuePairs;

            foreach (PropertyInfo property in @object.GetType().GetProperties())
            {
                if (!property.IsUserProperty())
                    continue;
                object? value = null;

                try
                {
                    value = property.GetValue(@object, null);
                }
                catch (Exception)
                {
                    continue;
                }
                propertyValuePairs.Add(property, value);
            }
            return propertyValuePairs;
        }

        public static string GetItemName(this IPanelObject? @object)
        {
            if (@object is null)
                return "null";

            Predicate<PropertyInfo> predicate = prop =>
            {
                if (prop.GetCustomAttribute<ItemNameAttribute>() is not ItemNameAttribute itemName)
                    return false;
                return itemName.Name is null;
            };

            if (Array.Find(@object.GetType().GetProperties(), predicate) is PropertyInfo nameProperty)
                return $"{nameProperty.GetValue(@object)}";
            if (@object.GetType().GetCustomAttribute<ItemNameAttribute>()?.Name is string name)
                return name;
            if (@object.GetType().FullName is string fullName)
                return fullName;
            return @object.GetType().Name;
        }

        public static string GetItemName(this object? @object)
        {
            if (@object is null)
                return "null";

            Predicate<PropertyInfo> predicate = prop =>
            {
                if (prop.GetCustomAttribute<ItemNameAttribute>() is not ItemNameAttribute itemName)
                    return false;
                return itemName.Name is null;
            };

            if (Array.Find(@object.GetType().GetProperties(), predicate) is PropertyInfo nameProperty)
                return $"{nameProperty.GetValue(@object)}";
            if (@object.GetType().GetCustomAttribute<ItemNameAttribute>()?.Name is string name)
                return name;
            return $"{@object}";
        }

        public static bool TrySetItemName(this IPanelObject @object, string name)
        {
            foreach (PropertyInfo property in @object.GetType().GetProperties())
            {
                if (property.PropertyType != typeof(string))
                    continue;
                if (property.GetCustomAttribute<ItemNameAttribute>() is not ItemNameAttribute itemName)
                    continue;
                if (itemName.Name is not null)
                    continue;
                property.SetValue(@object, name);
                return true;
            }
            return false;
        }

        public static string GetItemDescription(this object? @object)
        {
            if (@object is null)
                return "null";
            if (Array.Find(@object.GetType().GetProperties(), prop => prop.GetCustomAttribute<ItemDescriptionAttribute>()?.Description is not string) is PropertyInfo desccriptionProperty)
                if (desccriptionProperty.PropertyType == typeof(string))
                    return $"{desccriptionProperty.GetValue(@object)}";
            if (@object.GetType().GetCustomAttribute<ItemDescriptionAttribute>()?.Description is string description)
                return description;
            return "";
        }
    }
}
