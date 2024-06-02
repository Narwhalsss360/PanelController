using PanelController.PanelObjects.Properties;
using System.Diagnostics;
using System.Reflection;

namespace PanelController.Controller
{
    public static class Logger
    {
        public enum Levels
        {
            Error,
            Warning,
            Info,
            Debug
        }

        public class HistoricalLog : IFormattable
        {
            public readonly string Message;

            public readonly Levels Level;

            public readonly string From;

            public readonly DateTime LoggedAt;

            public HistoricalLog(string message, Levels level, string from, DateTime loggedAt)
            {
                Message = message;
                Level = level;
                From = from;
                LoggedAt = loggedAt;
            }

            /// <summary>
            /// /M -> Message
            /// /L -> Level
            /// /F -> From
            /// /T -> LoggedAt
            /// </summary>
            /// <param name="format">Format</param>
            /// <param name="_"></param>
            /// <returns>Formatted string</returns>
            public string ToString(string? format = null, IFormatProvider? _ = null)
            {
                if (format is not string fmt)
                    return "";
                return format.Replace("/M", Message).Replace("/L", $"{Level}").Replace("/F", From).Replace("/T", LoggedAt.ToString());
            }
        }

        public static event EventHandler<HistoricalLog>? Logged;

        private static List<HistoricalLog> _historicalLogs = new();

        public static HistoricalLog[] Logs { get => _historicalLogs.ToArray(); }

        public static void Log(string message, Levels level, object? sender = null)
        {
            if (sender is null)
            {
                StackFrame[] frames = new StackTrace().GetFrames();
                if (frames.Length > 1 && frames[1].GetMethod() is MethodInfo info)
                    sender = $"{info.DeclaringType?.Name}.{info.Name}";
            }
            _historicalLogs.Add(new(message, level, sender.GetItemName(), DateTime.Now));
            Logged?.Invoke(typeof(Logger), _historicalLogs.Last());
        }
    }
}
