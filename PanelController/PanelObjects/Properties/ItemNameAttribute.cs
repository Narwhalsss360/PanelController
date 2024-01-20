namespace PanelController.PanelObjects.Properties
{
    public class ItemNameAttribute : Attribute
    {
        public string? Name;

        public ItemNameAttribute(string? name = null)
        {
            Name = name;
        }
    }
}
