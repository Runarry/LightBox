using LightBox.Core.Models;
using LightBox.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightBox.Core.Services.Implementations
{
    public class WorkspaceManager : IWorkspaceService
    {
        private readonly IApplicationSettingsService _applicationSettingsService;
        private readonly ILoggingService _loggingService;
        private const string WorkspacesDirName = "Workspaces";
        private const string WorkspacesFileName = "workspaces.json";
        private string _workspacesBaseDir; // 将在构造函数中初始化

        public WorkspaceManager(IApplicationSettingsService applicationSettingsService, ILoggingService loggingService)
        {
            _applicationSettingsService = applicationSettingsService ?? throw new ArgumentNullException(nameof(applicationSettingsService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            // 通常，应用数据的基础路径应该从一个更通用的服务获取，或者在 ApplicationSettingsService 中定义
            // 这里为了简化，我们假设它在用户文档目录下
            string userDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appDataDir = Path.Combine(userDocsPath, "LightBox"); // 与 MVP 文档一致
            _workspacesBaseDir = Path.Combine(appDataDir, WorkspacesDirName);
            
            EnsureDirectoryExists(_workspacesBaseDir);
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _loggingService.LogInfo($"Created directory: {path}");
            }
        }

        private string GetWorkspacesFilePath() => Path.Combine(_workspacesBaseDir, WorkspacesFileName);
        private string GetWorkspaceDataFilePath(string fileName) => Path.Combine(_workspacesBaseDir, fileName);


        public async Task<WorkspaceInfo> CreateWorkspaceAsync(string name, string icon)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Workspace name cannot be empty.", nameof(name));
            }

            string workspaceId = Guid.NewGuid().ToString();
            // Sanitize name for file path, or use ID for uniqueness if names can collide
            string workspaceFileName = $"{name.Replace(" ", "_")}_{workspaceId.Substring(0, 8)}.json";
            string workspaceFilePath = GetWorkspaceDataFilePath(workspaceFileName);

            string currentIcon = icon;
            if (string.IsNullOrEmpty(currentIcon))
            {
                currentIcon = "default-workspace-icon"; // Set default icon
                _loggingService.LogInfo($"CreateWorkspaceAsync: Icon was null or empty, using default: {currentIcon}");
            }

            var newWorkspace = new Workspace(workspaceId, name, string.Empty, currentIcon);
            var newWorkspaceInfo = new WorkspaceInfo(workspaceId, name, workspaceFileName, currentIcon, DateTime.UtcNow);

            try
            {
                // 1. Save the new Workspace object to its own JSON file
                string workspaceJson = JsonSerializer.Serialize(newWorkspace, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(workspaceFilePath, workspaceJson);
                _loggingService.LogInfo($"Created workspace file: {workspaceFilePath}");

                // 2. Update workspaces.json
                List<WorkspaceInfo> workspaces = await GetWorkspacesAsync();
                workspaces.Add(newWorkspaceInfo);

                string workspacesListJson = JsonSerializer.Serialize(workspaces, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(GetWorkspacesFilePath(), workspacesListJson);
                _loggingService.LogInfo($"Updated workspaces list with new workspace: {name}");

                return newWorkspaceInfo;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating workspace '{name}'", ex);
                // Attempt to clean up if workspace file was created but list update failed
                if (File.Exists(workspaceFilePath))
                {
                    try { File.Delete(workspaceFilePath); }
                    catch (Exception cleanupEx) { _loggingService.LogError($"Error cleaning up workspace file '{workspaceFilePath}'", cleanupEx); }
                }
                throw; // Re-throw the original exception to indicate failure
            }
        }

        public async Task<List<WorkspaceInfo>> GetWorkspacesAsync()
        {
            string workspacesFilePath = GetWorkspacesFilePath();
            if (!File.Exists(workspacesFilePath))
            {
                _loggingService.LogInfo($"Workspaces file not found: {workspacesFilePath}. Returning empty list.");
                return new List<WorkspaceInfo>();
            }

            try
            {
                string json = await File.ReadAllTextAsync(workspacesFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _loggingService.LogInfo($"Workspaces file is empty: {workspacesFilePath}. Returning empty list.");
                    return new List<WorkspaceInfo>();
                }
                var workspaces = JsonSerializer.Deserialize<List<WorkspaceInfo>>(json);
                return workspaces ?? new List<WorkspaceInfo>();
            }
            catch (JsonException jsonEx)
            {
                _loggingService.LogError($"Error deserializing workspaces file: {workspacesFilePath}", jsonEx);
                return new List<WorkspaceInfo>(); // Return empty list on error to prevent crash
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error reading workspaces file: {workspacesFilePath}", ex);
                return new List<WorkspaceInfo>();
            }
        }

        public async Task<Workspace> GetWorkspaceByIdAsync(string workspaceId)
        {
            if (string.IsNullOrEmpty(workspaceId))
            {
                _loggingService.LogWarning("GetWorkspaceByIdAsync called with null or empty workspaceId.");
                return null;
            }

            List<WorkspaceInfo> workspaces = await GetWorkspacesAsync();
            WorkspaceInfo workspaceInfo = workspaces.FirstOrDefault(w => w.Id == workspaceId);

            if (workspaceInfo == null)
            {
                _loggingService.LogWarning($"Workspace with ID '{workspaceId}' not found in workspaces.json.");
                return null;
            }

            string workspaceFilePath = GetWorkspaceDataFilePath(workspaceInfo.FilePath);
            if (!File.Exists(workspaceFilePath))
            {
                _loggingService.LogError($"Workspace file '{workspaceFilePath}' for ID '{workspaceId}' not found, though it was listed in workspaces.json.");
                return null; // Data inconsistency
            }

            try
            {
                string json = await File.ReadAllTextAsync(workspaceFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                     _loggingService.LogError($"Workspace file '{workspaceFilePath}' is empty.");
                     return null;
                }
                var workspace = JsonSerializer.Deserialize<Workspace>(json);
                return workspace;
            }
            catch (JsonException jsonEx)
            {
                _loggingService.LogError($"Error deserializing workspace file: {workspaceFilePath}", jsonEx);
                return null;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error reading workspace file: {workspaceFilePath}", ex);
                return null;
            }
        }

        public async Task SaveWorkspaceAsync(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            if (string.IsNullOrWhiteSpace(workspace.Id))
            {
                throw new ArgumentException("Workspace ID cannot be empty.", nameof(workspace.Id));
            }
            if (string.IsNullOrWhiteSpace(workspace.Name))
            {
                throw new ArgumentException("Workspace name cannot be empty.", nameof(workspace.Name));
            }

            List<WorkspaceInfo> workspaces = await GetWorkspacesAsync();
            WorkspaceInfo workspaceInfo = workspaces.FirstOrDefault(w => w.Id == workspace.Id);

            if (workspaceInfo == null)
            {
                _loggingService.LogError($"Cannot save workspace. Workspace with ID '{workspace.Id}' not found in workspaces.json.");
                throw new FileNotFoundException($"Workspace with ID '{workspace.Id}' not found.");
            }

            string workspaceFilePath = GetWorkspaceDataFilePath(workspaceInfo.FilePath);

            try
            {
                // 1. Serialize and save the Workspace object
                string workspaceJson = JsonSerializer.Serialize(workspace, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(workspaceFilePath, workspaceJson);
                _loggingService.LogInfo($"Saved workspace data to: {workspaceFilePath}");

                // 2. Check if WorkspaceInfo in workspaces.json needs updating (name or icon)
                bool infoUpdated = false;
                if (workspaceInfo.Name != workspace.Name)
                {
                    workspaceInfo.Name = workspace.Name;
                    infoUpdated = true;
                }
                if (workspaceInfo.Icon != workspace.Icon)
                {
                    workspaceInfo.Icon = workspace.Icon;
                    infoUpdated = true;
                }
                // Potentially, if name changes, filePath in WorkspaceInfo might need to change too,
                // but current logic for CreateWorkspaceAsync generates filename based on initial name + ID part.
                // For simplicity, we'll assume FilePath in WorkspaceInfo doesn't change upon name update here.
                // If it should, then the file itself would need to be renamed, which adds complexity.

                if (infoUpdated)
                {
                    workspaceInfo.LastOpened = DateTime.UtcNow; // Update last opened time on any save
                    string workspacesListJson = JsonSerializer.Serialize(workspaces, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(GetWorkspacesFilePath(), workspacesListJson);
                    _loggingService.LogInfo($"Updated workspace info for '{workspace.Name}' in workspaces.json.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error saving workspace '{workspace.Name}' (ID: {workspace.Id})", ex);
                throw;
            }
        }

        public async Task DeleteWorkspaceAsync(string workspaceId)
        {
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                _loggingService.LogWarning("DeleteWorkspaceAsync called with null or empty workspaceId.");
                return; // Or throw ArgumentException
            }

            List<WorkspaceInfo> workspaces = await GetWorkspacesAsync();
            WorkspaceInfo workspaceToDelete = workspaces.FirstOrDefault(w => w.Id == workspaceId);

            if (workspaceToDelete == null)
            {
                _loggingService.LogWarning($"Workspace with ID '{workspaceId}' not found. Cannot delete.");
                return; // Or throw FileNotFoundException
            }

            // 1. Delete the workspace's JSON file
            string workspaceFilePath = GetWorkspaceDataFilePath(workspaceToDelete.FilePath);
            try
            {
                if (File.Exists(workspaceFilePath))
                {
                    File.Delete(workspaceFilePath);
                    _loggingService.LogInfo($"Deleted workspace file: {workspaceFilePath}");
                }
                else
                {
                    _loggingService.LogWarning($"Workspace file to delete not found: {workspaceFilePath}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error deleting workspace file '{workspaceFilePath}' for ID '{workspaceId}'", ex);
                // Decide if we should proceed to remove from list or re-throw
                throw; // For now, re-throw as it's a critical part of deletion
            }

            // 2. Remove from workspaces.json
            workspaces.Remove(workspaceToDelete);
            try
            {
                string workspacesListJson = JsonSerializer.Serialize(workspaces, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(GetWorkspacesFilePath(), workspacesListJson);
                _loggingService.LogInfo($"Removed workspace ID '{workspaceId}' from workspaces.json.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error updating workspaces.json after deleting workspace ID '{workspaceId}'", ex);
                // At this point, the workspace file is deleted but the list isn't updated.
                // This is a partial failure state. Consider how to handle robustly.
                throw;
            }

            // 3. If deleted workspace was active, clear active workspace ID
            var activeWorkspaceId = await GetActiveWorkspaceIdAsync();
            if (activeWorkspaceId == workspaceId)
            {
                await SetActiveWorkspaceIdAsync(null); // Set to null or an empty string
                _loggingService.LogInfo($"Cleared active workspace ID as deleted workspace '{workspaceId}' was active.");
            }
        }

        public async Task SetActiveWorkspaceIdAsync(string workspaceId)
        {
            var settings = await _applicationSettingsService.LoadSettingsAsync();
            settings.DefaultWorkspaceId = workspaceId; // 使用 DefaultWorkspaceId 存储活动工作区ID
            await _applicationSettingsService.SaveSettingsAsync(settings);
            _loggingService.LogInfo($"Active workspace ID set to: {workspaceId}");
        }

        public async Task<string> GetActiveWorkspaceIdAsync()
        {
            var settings = await _applicationSettingsService.LoadSettingsAsync();
            return settings.DefaultWorkspaceId;
        }

        public async Task<Workspace> GetActiveWorkspaceAsync()
        {
            var activeWorkspaceId = await GetActiveWorkspaceIdAsync();
            if (string.IsNullOrEmpty(activeWorkspaceId))
            {
                _loggingService.LogWarning("No active workspace ID found.");
                return null;
            }
            return await GetWorkspaceByIdAsync(activeWorkspaceId);
        }
    }
}