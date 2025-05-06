using LightBox.PluginContracts;
using LightBox.Core.Services.Interfaces;
using System;
using System.IO;
using System.Text; // For StringBuilder

namespace LightBox.Core.Services.Implementations
{
    public class SimpleFileLogger : ILoggingService
    {
        private readonly string _logFilePath;
        private readonly LogLevel _minimumLevel; // For basic filtering
        private static readonly object _lock = new object();

        // Constructor could take log file path and minimum level from settings later
        public SimpleFileLogger(string logFilePath = "Logs/lightbox_dev.log", LogLevel minimumLevel = LogLevel.Debug)
        {
            _logFilePath = Path.GetFullPath(logFilePath); // Ensure absolute path
            _minimumLevel = minimumLevel;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log directory: {ex.Message}");
            }
        }

        public void Log(LogLevel level, string message, Exception ex = null)
        {
            if (level < _minimumLevel)
            {
                return;
            }

            var logEntry = new StringBuilder();
            logEntry.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.ToString().ToUpper()}] ");
            logEntry.Append(message);

            if (ex != null)
            {
                logEntry.Append($"{Environment.NewLine}  Exception: {ex.GetType().FullName}: {ex.Message}");
                logEntry.Append($"{Environment.NewLine}  StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    logEntry.Append($"{Environment.NewLine}  InnerException: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                    logEntry.Append($"{Environment.NewLine}  InnerStackTrace: {ex.InnerException.StackTrace}");
                }
            }

            string formattedMessage = logEntry.ToString();

            Console.WriteLine(formattedMessage); // Always log to console

            try
            {
                lock (_lock) // Basic thread safety for file writing
                {
                    File.AppendAllText(_logFilePath, formattedMessage + Environment.NewLine);
                }
            }
            catch (Exception fileEx)
            {
                Console.WriteLine($"Error writing to log file '{_logFilePath}': {fileEx.Message}");
            }
        }

        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogInfo(string message) => Log(LogLevel.Info, message);
        public void LogWarning(string message, Exception ex = null) => Log(LogLevel.Warning, message, ex);
        public void LogError(string message, Exception ex = null) => Log(LogLevel.Error, message, ex);
    }
}