using LightBox.PluginContracts; // For LogLevel
using System;

namespace LightBox.Core.Services.Interfaces
{
    public interface ILoggingService
    {
        void Log(LogLevel level, string message, Exception ex = null);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message, Exception ex = null);
        void LogError(string message, Exception ex = null);
    }
}