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

        public struct HistoricalLog
        {
            public readonly string Message;

            public readonly Levels Level;

            public readonly string From;

            public HistoricalLog(string message, Levels level, string from)
            {
                Message = message;
                Level = level;
                From = from;
            }
        }

        public static event EventHandler<HistoricalLog>? Logged;

        private static List<HistoricalLog> _historicalLogs = new();

        public static HistoricalLog[] Logs { get => _historicalLogs.ToArray(); }

        public static void Log(string message, Levels level, object? sender = null)
        {
            _historicalLogs.Add(new(message, level, sender.GetItemName()));
            Logged?.Invoke(typeof(Logger), _historicalLogs.Last());
        }
    }
}
