using PanelController.Controller;
using PanelController.PanelObjects;
using PanelController.PanelObjects.Properties;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Reflection;
using static PanelController.Profiling.Mapping.SerializableMapping.MappedObjectSerializable;

namespace PanelController.Profiling
{
    public class Mapping : IEquatable<Mapping>
    {
        public string Name = string.Empty;

        public Guid PanelGuid;

        public InterfaceTypes InterfaceType;

        public uint InterfaceID;

        public object? InterfaceOption;

        public class MappedObject
        {
            public IPanelObject Object;

            public TimeSpan Delay;

            public object? Value;

            public MappedObject()
            {
                Object = IPanelObject.None;
                Delay = TimeSpan.Zero;
                Value = null;
            }

            public MappedObject(IPanelObject @object, TimeSpan delay, object? value)
            {
                Object = @object;
                Delay = delay;
                Value = value;
            }
        }

        public ObservableCollection<MappedObject> Objects = new();

        public Mapping()
        {
        }

        public Mapping(SerializableMapping serializable)
        {
            Name = serializable.Name;
            PanelGuid = serializable.PanelGuid;
            InterfaceType = serializable.InterfaceType;
            InterfaceID = serializable.InterfaceID;
            if (serializable.OnActivated is bool activated)
                InterfaceOption = activated;
            LoadObjectsFrom(serializable);
        }

        public void LoadObjectsFrom(SerializableMapping serializable)
        {
            Objects.Clear();
            foreach (var obj in serializable.Objects)
            {
                if (Array.Find(Extensions.AllExtensions, extension => extension.FullName == obj.FullName) is not Type type)
                    continue;
                IPanelObject panelObject = type.CreatePanelObject();

                foreach (var objectProperty in obj.Properties)
                {
                    if (type.GetProperty(objectProperty.Name) is not PropertyInfo property)
                        continue;
                    try
                    {
                        property.SetValue(panelObject, objectProperty.Value);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                Objects.Add(new(panelObject, obj.Delay, obj.Value));
            }
        }

        public Dictionary<IPanelObject, object?> Execute(object? value = null)
        {
            Dictionary<IPanelObject, object?> results = new();
            foreach (var item in Objects)
            {
                Task.Delay(item.Delay).Wait();
                if (InterfaceType == InterfaceTypes.Digital && item.Object is IPanelAction action)
                    results.Add(action, action.Run());
                else if (InterfaceType == InterfaceTypes.Analog && item.Object is IPanelSettable settable)
                    results.Add(settable, settable.Set(value as IPanelSettable.SettableValue));
                else if (item.Object is IPanelSource source)
                    results.Add(source, source.Get());
                else
                    results.Add(item.Object, new InvalidOperationException($"Execute: Unkown IPanelObject type: {item.Object}"));
            }
            return results;
        }

        public bool Equals(Mapping? other)
        {
            if (other is null)
                return false;

            return
                PanelGuid == other.PanelGuid &&
                InterfaceType == other.InterfaceType &&
                InterfaceID == other.InterfaceID &&
                InterfaceOption == other.InterfaceOption;
        }

        public override string? ToString() => Name != "" ? Name : $"{PanelGuid.PanelInfoNameOrGuid()} {InterfaceType} {InterfaceID} {InterfaceOption}";

        [Serializable]
        public class SerializableMapping
        {
            public string Name = string.Empty;

            public Guid PanelGuid;

            public InterfaceTypes InterfaceType;

            public uint InterfaceID;

            public bool? OnActivated;

            [Serializable]
            public class MappedObjectSerializable
            {
                public string FullName;

                public TimeSpan Delay;

                public object? Value;

                public ObjectProperty[] Properties;

                public class ObjectProperty
                {
                    public string Name;

                    public object? Value;

                    public ObjectProperty()
                    {
                        Name = string.Empty;
                        Value = null;
                    }

                    public ObjectProperty(string name, object? value)
                    {
                        Name = name;
                        Value = value;
                    }

                    public static ObjectProperty[] GetObjectProperties(object? @object)
                    {
                        if (@object is null)
                            return Array.Empty<ObjectProperty>();

                        List<ObjectProperty> properties = new();

                        foreach (var property in @object.GetType().GetUserProperties())
                            properties.Add(new(property.Name, property.GetValue(@object)));

                        return properties.ToArray();
                    }
                }

                public MappedObjectSerializable()
                {
                    FullName = string.Empty;
                    Properties = Array.Empty<ObjectProperty>();
                }

                public MappedObjectSerializable(string fullName, TimeSpan delay, object? value, ObjectProperty[] properties)
                {
                    FullName = fullName;
                    Delay = delay;
                    Value = value;
                    Properties = properties;
                }
            }

            public MappedObjectSerializable[] Objects;

            public SerializableMapping()
            {
                Objects = Array.Empty<MappedObjectSerializable>();
            }

            public SerializableMapping(Mapping mapping)
            {
                Name = mapping.Name;
                PanelGuid = mapping.PanelGuid;
                InterfaceType = mapping.InterfaceType;
                InterfaceID = mapping.InterfaceID;
                if (mapping.InterfaceOption is bool option)
                    OnActivated = option;
                Objects = new MappedObjectSerializable[mapping.Objects.Count];

                for (int i = 0; i < Objects.Length; i++)
                    Objects[i] = new MappedObjectSerializable(mapping.Objects[i].Object.GetType().FullName ?? "",
                        mapping.Objects[i].Delay,
                        mapping.Objects[i].Value,
                        ObjectProperty.GetObjectProperties(mapping.Objects[i].Object));
            }
        }
    }
}
