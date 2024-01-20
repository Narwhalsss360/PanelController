using System.Reflection;

namespace PanelController.PanelObjects
{
    public static class MethodInfoExtensions
    {
        public static bool IsDetector(this MethodInfo method)
        {
            if (method.GetCustomAttribute<IChannel.DetectorAttribute>() is null)
                return false;

            if (!method.IsStatic)
                return false;
            if (!method.ReturnType.IsArray)
                return false;
            if (!method.ReturnType.HasElementType)
                return false;
            if (method.ReturnType.GetElementType() != typeof(IChannel))
                return false;

            return true;
        }
    }
}
