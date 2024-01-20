namespace PanelController.PanelObjects.Properties
{
    public class ItemDescriptionAttribute : Attribute
    {
        public string? Description;

        public ItemDescriptionAttribute(string? description = null)
        {
            Description = description;
        }
    }
}
