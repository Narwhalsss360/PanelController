namespace PanelController.Profiling
{
    public class PanelInfo
    {
        public Guid PanelGuid;

        public string Name = string.Empty;

        public Dictionary<InterfaceTypes, uint> InterfaceCount = new()
        {
            { InterfaceTypes.Digital, 0 },
            { InterfaceTypes.Analog, 0 },
            { InterfaceTypes.Display, 0 }
        };
    }
}
