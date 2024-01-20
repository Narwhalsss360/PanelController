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

        public static ObservableCollection<Tuple<Type, MethodInfo, IChannel.Detect>> Detectors = new();

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

        public static ObservableCollection<IPanelObject> GenericObjects = new();

        public static void Load(Type type)
        {
            if (type.GetConstructor(new Type[] { }) is null)
                return;

            if (type.Implements<IChannel>())
            {
                ExtensionsByCategory[ExtensionCategories.Channel].Add(type);
                foreach (var method in type.GetMethods())
                {
                    if (!method.IsDetector())
                        continue;
                    Detectors.Add(new(type, method, (IChannel.Detect)Delegate.CreateDelegate(typeof(IChannel.Detect), method)));
                }
            }
            else if (type.Implements<IPanelAction>())
            {
                ExtensionsByCategory[ExtensionCategories.Action].Add(type);
            }
            else if (type.Implements<IPanelSettable>())
            {
                ExtensionsByCategory[ExtensionCategories.Settable].Add(type);
            }
            else if (type.Implements<IPanelSource>())
            {
                ExtensionsByCategory[ExtensionCategories.Source].Add(type);
            }
            else if (type.Implements<IPanelObject>())
            {
                ExtensionsByCategory[ExtensionCategories.Generic].Add(type);
                if (type.GetCustomAttribute<AutoLaunchAttribute>() is not null)
                    if (Activator.CreateInstance(type) is IPanelObject obj)
                        GenericObjects.Add(obj);
            }
            else
            {
                return;
            }
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
