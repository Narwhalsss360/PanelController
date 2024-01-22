namespace PanelController.PanelObjects.Properties
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ConstrainedUserPropertyAttribute : UserPropertyAttribute
    {
        private string _nameofConstraints;

        public string NameConstraints { get { return _nameofConstraints; } }

        public ConstrainedUserPropertyAttribute(string nameContstraints = "")
        {
            _nameofConstraints = nameContstraints;
        }
    }
}
