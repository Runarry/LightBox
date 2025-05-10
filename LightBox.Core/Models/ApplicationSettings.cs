using System.Collections.Generic;

namespace LightBox.Core.Models
{
    public class ApplicationSettings
    {
        public List<string> PluginScanDirectories { get; set; } = new List<string>();
        public string LogFilePath { get; set; } = "Logs/lightbox-.log"; // Default, will be formatted with date
        public string LogLevel { get; set; } = "Information"; // Default LogLevel
        public int IpcApiPort { get; set; } = 0; // 0 means assign a random available port
        public string DefaultWorkspaceId { get; set; } // Stores the ID of the last active or default workspace
    }
}
