using System.Reflection;
using System.Runtime.CompilerServices;

namespace PanelController.PanelObjects
{
    public interface IChannel : IPanelObject
    {
        public delegate IChannel[] Detect();

        [AttributeUsage(AttributeTargets.Method)]
        public class DetectorAttribute : Attribute
        {
        }

        public bool IsOpen { get; }

        public event EventHandler<byte[]>? BytesReceived;

        public object? Open();

        public object? Send(byte[] data);

        public void Close();
    }
}
