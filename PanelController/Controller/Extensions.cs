using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using System.Collections.ObjectModel;
using System.Reflection;

namespace PanelController.Controller
{
    public static class Extensions
    {
        public enum ExtensionCategories
        {
            Generic,
            Channel,
            Action,
            Settable,
            Source
        }

        public static ObservableCollection<Tuple<bool, MethodInfo, IChannel.Detect>> Detectors = new();

        public static Dictionary<ExtensionCategories, ObservableCollection<Type>> ExtensionsByCategory = new()
        {
            { ExtensionCategories.Generic, new() },
            { ExtensionCategories.Channel, new() },
            { ExtensionCategories.Action, new() },
            { ExtensionCategories.Settable, new() },
            { ExtensionCategories.Source, new() }
        };

        public static Type[] AllExtensions
        {
            get
            {
                List<Type> types = new();
                foreach (var collection in  ExtensionsByCategory.Values)
                    types.AddRange(collection);
                return types.ToArray();
            }
        }

        public static ObservableCollection<IPanelObject> Objects = new();

        public static ExtensionCategories? GetExtensionCategory(this Type type)
        {
            if (type.Implements<IChannel>())
                return ExtensionCategories.Channel;
            if (type.Implements<IPanelAction>())
                return ExtensionCategories.Action;
            if (type.Implements<IPanelSettable>())
                return ExtensionCategories.Settable;
            if (type.Implements<IPanelSource>())
                return ExtensionCategories.Source;
            if (type.Implements<IPanelObject>())
                return ExtensionCategories.Generic;
            return null;
        }

        public static Type? FindType(this string fullname, ExtensionCategories? category = null)
        {
            Type? type = null;
            if (category is null)
            {
                type = Array.Find(AllExtensions, t => t.FullName == fullname);
            }
            else
            {
                foreach (Type t in ExtensionsByCategory[category.Value])
                {
                    if (t.FullName == fullname)
                    {
                        type = t;
                        break;
                    }
                }
            }
            return type;
        }

        public static void Load(Type type)
        {
            if (type.GetConstructor(new Type[] { }) is null || type.GetExtensionCategory() is not ExtensionCategories category)
                return;

            switch (category)
            {
                case ExtensionCategories.Generic:
                    if (type.GetCustomAttribute<AutoLaunchAttribute>() is not null)
                        if (Activator.CreateInstance(type) is IPanelObject obj)
                            Objects.Add(obj);
                    break;
                case ExtensionCategories.Channel:
                    foreach (var method in type.GetMethods())
                    {
                        if (!method.IsDetector())
                            continue;
                        Detectors.Add(new(true, method, (IChannel.Detect)Delegate.CreateDelegate(typeof(IChannel.Detect), method)));
                    }
                    break;
                default:
                    break;
            }

            ExtensionsByCategory[category].Add(type);
            Logger.Log($"Loaded {type.GetItemName()}.", Logger.Levels.Info, "Extension Loader");
        }

        public static void Load<T>() => Load(typeof(T));

        public static void Load(Assembly assembly)
        {
            if (assembly.GetCustomAttribute<PanelExtensionAttribute>() is null)
                return;
            foreach (var type in assembly.GetTypes())
                Load(type);
        }
    }
}
