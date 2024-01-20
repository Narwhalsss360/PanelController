namespace PanelController.PanelObjects
{
    public interface IPanelAction : IPanelObject
    {
        public object? Run();
    }
}
