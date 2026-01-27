namespace ABDTools.Core.Logging;

public static class Logger
{
    private static readonly object _lock = new();
    private static readonly string _logPath;

    static Logger()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appDataPath, "ABDTools");
        Directory.CreateDirectory(folder);
        _logPath = Path.Combine(folder, "ABDTools.log");
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null)
    {
        var full = ex == null ? message : $"{message}{Environment.NewLine}{ex}";
        Write("ERROR", full);
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Nunca lanzar desde el logger
        }
    }
}
