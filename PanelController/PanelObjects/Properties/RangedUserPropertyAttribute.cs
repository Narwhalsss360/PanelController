namespace PanelController.PanelObjects.Properties
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RangedUserPropertyAttribute : UserPropertyAttribute
    {
        private string _nameofLow;

        public string NameLow { get { return _nameofLow; } }

        private string _nameofHigh;

        public string NameHigh { get { return _nameofHigh; } }

        public RangedUserPropertyAttribute(string nameLow, string nameHigh)
        {
            _nameofLow = nameLow;
            _nameofHigh = nameHigh;
        }
    }
}
