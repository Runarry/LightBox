using LightBox.PluginContracts;
using LightBox.Core.Services.Interfaces;
using System;
using System.IO;
using System.Text; 

namespace LightBox.Core.Services.Implementations
{
    public class SimpleFileLogger : ILoggingService
    {
        private readonly string _logFilePath;
        private readonly LogLevel _minimumLevel;
        private static readonly object _lock = new object();

        public SimpleFileLogger(string logFilePath = "", LogLevel minimumLevel = LogLevel.Debug)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                _logFilePath = Path.Combine(desktopPath, "LightBox_Debug.log");
            }
            else
            {
                _logFilePath = Path.GetFullPath(logFilePath);
            }
            
            _minimumLevel = minimumLevel;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
                // Initial log to confirm logger is working and path is correct.
                // This internal call to Log needs to be careful if Log itself can throw before basic setup.
                // For simplicity, we'll assume AppendAllText is robust enough for this initial message.
                File.AppendAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] SimpleFileLogger initialized. Log file at: {_logFilePath}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log directory or writing initial log: {ex.Message}");
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

            Console.WriteLine(formattedMessage); 

            try
            {
                lock (_lock) 
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