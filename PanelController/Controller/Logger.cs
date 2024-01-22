using PanelController.PanelObjects.Properties;

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
            /// </summary>
            /// <param name="format">Format</param>
            /// <param name="_"></param>
            /// <returns>Formatted string</returns>
            public string ToString(string? format, IFormatProvider? _)
            {
                if (format is not string fmt)
                    return "";
                return format.Replace("/M", Message).Replace("/L", $"{Level}").Replace("/F", From);
            }
        }

        public static event EventHandler<HistoricalLog>? Logged;

        private static List<HistoricalLog> _historicalLogs = new();

        public static HistoricalLog[] Logs { get => _historicalLogs.ToArray(); }

        public static void Log(string message, Levels level, object? sender = null)
        {
            _historicalLogs.Add(new(message, level, sender.GetItemName(), DateTime.Now));
            Logged?.Invoke(typeof(Logger), _historicalLogs.Last());
        }
    }
}
