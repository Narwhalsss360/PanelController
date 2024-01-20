namespace PanelController.PanelObjects
{
    public interface IPanelSettable : IPanelObject
    {
        public object? Set(object? value);
    }
}
