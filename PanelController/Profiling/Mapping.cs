using PanelController.PanelObjects;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace PanelController.Profiling
{
    public class Mapping : IEquatable<Mapping>
    {
        public Guid PanelGuid;

        public InterfaceTypes InterfaceType;

        public uint InterfaceID;

        public object? InterfaceOption;

        public ObservableCollection<Tuple<IPanelObject, TimeSpan, object?>> Objects = new();

        public Dictionary<IPanelObject, object?> Execute(object? value = null)
        {
            Dictionary<IPanelObject, object?> results = new();
            foreach (var item in Objects)
            {
                Task.Delay(item.Item2).Wait();
                if (InterfaceType == InterfaceTypes.Digital && item.Item1 is IPanelAction action)
                    results.Add(action, action.Run());
                else if (InterfaceType == InterfaceTypes.Analog && item.Item1 is IPanelSettable settable)
                    results.Add(settable, settable.Set(value is null ? item.Item3 : value));
                else if (item.Item1 is IPanelSource source)
                    results.Add(source, source.Get());
                else
                    results.Add(item.Item1, new InvalidOperationException($"Execute: Unkown IPanelObject type: {item.Item1}"));
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
    }
}
