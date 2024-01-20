namespace PanelController.PanelObjects
{
    public interface IPanelSource : IPanelObject
    {
        public object? Get();
    }
}
