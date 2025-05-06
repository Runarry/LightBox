using LightBox.PluginContracts;
using LightBox.Core.Services.Interfaces; // For ILoggingService
using System; // For ArgumentNullException
using System.IO; // For Path

namespace LightBox.Core.Host
{
    public class LightBoxHostContext : ILightBoxHostContext
    {
        private readonly ILoggingService _loggingService;
        // private readonly IWorkspaceService _workspaceService; // Will be needed for GetWorkspacePath

        // For MVP, GetWorkspacePath can return a fixed or placeholder value.
        // Later, it will use an IWorkspaceService to get the actual active workspace path.
        public LightBoxHostContext(ILoggingService loggingService /*, IWorkspaceService workspaceService */)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            // _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        }

        public void Log(LogLevel level, string message)
        {
            _loggingService.Log(level, message);
        }

        public string GetWorkspacePath()
        {
            // MVP: Return a placeholder or a configured path.
            // TODO: Implement properly using IWorkspaceService once it's available.
            // For now, let's assume a path relative to the application's execution directory or a user-specific path.
            // This needs to be consistent with how workspaces are actually managed.
            // string basePath = AppDomain.CurrentDomain.BaseDirectory;
            // return Path.Combine(basePath, "Workspaces", "Default"); // Example placeholder

            // As per MVP plan, this can return a fixed value or a simple configurable path for now.
            // Let's return a path that might be configurable via ApplicationSettings later.
            // For now, a hardcoded example:
            string userDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(userDocsPath, "LightBox", "ActiveWorkspace"); // Placeholder
        }
    }
}