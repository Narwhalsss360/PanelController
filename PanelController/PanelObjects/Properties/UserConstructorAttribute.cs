namespace PanelController.PanelObjects.Properties
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class UserConstructorAttribute : Attribute
    {
        public readonly string? Description;

        public UserConstructorAttribute(string? description)
        {
            Description = description;
        }
    }
}
