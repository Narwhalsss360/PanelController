using PanelController.Controller;
using System.Runtime.InteropServices;

namespace PanelController.Profiling
{
    [Serializable]
    public class PanelInfo : IFormattable, IEquatable<PanelInfo>
    {
        public Guid PanelGuid;

        public string Name = string.Empty;

        public uint DigitalCount = 0;

        public uint AnalogCount = 0;

        public uint DisplayCount = 0;

        public bool IsConnected
        {
            get => Main.ConnectedPanels.Any(connected => connected.PanelGuid == PanelGuid);
        }

        public bool Equals(PanelInfo? other)
        {
            if (other is null)
                return false;
            return PanelGuid == other.PanelGuid;
        }

        public string ToString(string? format = null, IFormatProvider? formatProvider = null) => $"{Name}";

        public override bool Equals(object? obj) => Equals(obj as PanelInfo);

        public override int GetHashCode() => PanelGuid.GetHashCode();
    }
}
