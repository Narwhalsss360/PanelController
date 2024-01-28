using PanelController.PanelObjects.Properties;

namespace PanelController.PanelObjects
{
    public interface IPanelObject : IFormattable
    {
        public class NoneObject : IPanelObject
        {
        }

        public static readonly IPanelObject None = new NoneObject();

        public string Status { get => "OK"; }

        public string? ToString()
        {
            return this.GetItemName();
        }

        string IFormattable.ToString(string? _, IFormatProvider? _1)
        {
            return this.GetItemName();
        }
    }
}
