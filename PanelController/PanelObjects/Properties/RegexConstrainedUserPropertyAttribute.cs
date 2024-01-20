namespace PanelController.PanelObjects.Properties
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegexConstrainedUserPropertyAttribute : UserPropertyAttribute
    {
        private string _nameofRegex;

        public string NameRegex { get { return _nameofRegex; } }

        public RegexConstrainedUserPropertyAttribute(string nameofRegex)
        {
            _nameofRegex = nameofRegex;
        }
    }
}
