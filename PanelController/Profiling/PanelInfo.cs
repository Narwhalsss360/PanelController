namespace PanelController.Profiling
{
    [Serializable]
    public class PanelInfo : IFormattable
    {
        public Guid PanelGuid;

        public string Name = string.Empty;

        public uint DigitalCount = 0;

        public uint AnalogCount = 0;

        public uint DisplayCount = 0;

        public string ToString(string? format = null, IFormatProvider? formatProvider = null) => $"{Name}";
    }
}
