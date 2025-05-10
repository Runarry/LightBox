using LightBox.Core.Models;
using LightBox.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LightBox.PluginContracts; // Required for LogLevel

namespace LightBox.WPF
{
    public class LightBoxJsBridge
    {
        private readonly IApplicationSettingsService _applicationSettingsService;
        private readonly IPluginService _pluginService;
        private readonly ILoggingService _loggingService;
        private readonly IWorkspaceService _workspaceService; // Added

        public LightBoxJsBridge(
            IApplicationSettingsService applicationSettingsService,
            IPluginService pluginService,
            ILoggingService loggingService,
            IWorkspaceService workspaceService) // Added
        {
            _applicationSettingsService = applicationSettingsService ?? throw new ArgumentNullException(nameof(applicationSettingsService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService)); // Added
        }

        public async Task<string> GetApplicationSettings()
        {
            _loggingService.LogInfo("LightBoxJsBridge.GetApplicationSettings CALLED");
            try
            {
                var settings = await _applicationSettingsService.LoadSettingsAsync();
                return JsonSerializer.Serialize(settings);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in GetApplicationSettings: {ex.Message}", ex);
                return JsonSerializer.Serialize(new { error = $"Failed to get application settings: {ex.Message}" });
            }
        }

        public async Task SaveApplicationSettings(string settingsJson)
        {
            if (string.IsNullOrWhiteSpace(settingsJson))
            {
                _loggingService.LogWarning("SaveApplicationSettings called with empty or null JSON.");
                // It's better to return a Task that indicates failure or throw an appropriate exception.
                // For JS interop, throwing an exception that can be caught by JS is often clearer.
                throw new ArgumentNullException(nameof(settingsJson), "Settings JSON cannot be null or empty.");
            }

            try
            {
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(settingsJson);
                if (settings != null)
                {
                    await _applicationSettingsService.SaveSettingsAsync(settings);
                    _loggingService.LogInfo("Application settings saved successfully.");
                }
                else
                {
                    _loggingService.LogError("Failed to deserialize settings JSON to ApplicationSettings object (result was null).");
                    throw new JsonException("Deserialized ApplicationSettings object was null.");
                }
            }
            catch (JsonException jsonEx)
            {
                _loggingService.LogError($"Error deserializing settings JSON in SaveApplicationSettings: {jsonEx.Message}", jsonEx);
                throw;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in SaveApplicationSettings: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<string> GetAllPluginDefinitions()
        {
            try
            {
                var pluginDefinitions = await _pluginService.DiscoverPluginsAsync();
                return JsonSerializer.Serialize(pluginDefinitions);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in GetAllPluginDefinitions: {ex.Message}", ex);
                return JsonSerializer.Serialize(new { error = $"Failed to get plugin definitions: {ex.Message}" });
            }
        }

        // Workspace Management Methods
        public async Task<string> GetWorkspaces()
        {
            _loggingService.LogInfo("LightBoxJsBridge.GetWorkspaces CALLED");
            try
            {
                var workspaces = await _workspaceService.GetWorkspacesAsync();
                return JsonSerializer.Serialize(workspaces);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in GetWorkspaces: {ex.Message}", ex);
                return JsonSerializer.Serialize(new { error = $"Failed to get workspaces: {ex.Message}" });
            }
        }

        public async Task<string> CreateWorkspace(string name, string icon)
        {
            _loggingService.LogInfo($"LightBoxJsBridge.CreateWorkspace CALLED with name: {name}, icon: {icon}");
            if (string.IsNullOrWhiteSpace(name))
            {
                _loggingService.LogWarning("CreateWorkspace called with empty or null name.");
                throw new ArgumentNullException(nameof(name), "Workspace name cannot be null or empty.");
            }
            // Icon can be optional; the WorkspaceManager service will handle default if not provided.

            try
            {
                var newWorkspaceInfo = await _workspaceService.CreateWorkspaceAsync(name, icon);
                return JsonSerializer.Serialize(newWorkspaceInfo);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in CreateWorkspace (name: {name}): {ex.Message}", ex);
                // It's better to let the exception propagate to JS if it's an ArgumentException or similar
                // For other unexpected errors, serializing an error object might be an option, but consistency is key.
                // For now, rethrow to be caught by a global handler or by JS.
                throw;
            }
        }

        public async Task SetActiveWorkspace(string workspaceId)
        {
            _loggingService.LogInfo($"LightBoxJsBridge.SetActiveWorkspace CALLED with ID: {workspaceId}");
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                _loggingService.LogWarning("SetActiveWorkspace called with empty or null workspaceId.");
                throw new ArgumentNullException(nameof(workspaceId), "Workspace ID cannot be null or empty.");
            }
            try
            {
                await _workspaceService.SetActiveWorkspaceIdAsync(workspaceId);
                _loggingService.LogInfo($"Successfully set active workspace to: {workspaceId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in SetActiveWorkspace (ID: {workspaceId}): {ex.Message}", ex);
                throw;
            }
        }

        public async Task<string> GetActiveWorkspace()
        {
            _loggingService.LogInfo("LightBoxJsBridge.GetActiveWorkspace CALLED");
            try
            {
                var activeWorkspace = await _workspaceService.GetActiveWorkspaceAsync();
                if (activeWorkspace == null)
                {
                    // It's valid to have no active workspace, return null or an empty object string
                    return JsonSerializer.Serialize<Workspace>(null);
                }
                return JsonSerializer.Serialize(activeWorkspace);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in GetActiveWorkspace: {ex.Message}", ex);
                return JsonSerializer.Serialize(new { error = $"Failed to get active workspace: {ex.Message}" });
            }
        }

        public async Task UpdateWorkspace(string workspaceJson)
        {
            _loggingService.LogInfo("LightBoxJsBridge.UpdateWorkspace CALLED");
            if (string.IsNullOrWhiteSpace(workspaceJson))
            {
                _loggingService.LogWarning("UpdateWorkspace called with empty or null JSON.");
                throw new ArgumentNullException(nameof(workspaceJson), "Workspace JSON cannot be null or empty.");
            }

            try
            {
                var workspace = JsonSerializer.Deserialize<Workspace>(workspaceJson);
                if (workspace == null)
                {
                    _loggingService.LogError("Failed to deserialize workspace JSON to Workspace object (result was null).");
                    throw new JsonException("Deserialized Workspace object was null.");
                }
                await _workspaceService.SaveWorkspaceAsync(workspace);
                _loggingService.LogInfo($"Successfully updated workspace: {workspace.Name} (ID: {workspace.Id})");
            }
            catch (JsonException jsonEx)
            {
                _loggingService.LogError($"Error deserializing workspace JSON in UpdateWorkspace: {jsonEx.Message}", jsonEx);
                throw;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in UpdateWorkspace: {ex.Message}", ex);
                throw;
            }
        }

        public async Task DeleteWorkspace(string workspaceId)
        {
            _loggingService.LogInfo($"LightBoxJsBridge.DeleteWorkspace CALLED with ID: {workspaceId}");
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                _loggingService.LogWarning("DeleteWorkspace called with empty or null workspaceId.");
                throw new ArgumentNullException(nameof(workspaceId), "Workspace ID cannot be null or empty.");
            }
            try
            {
                await _workspaceService.DeleteWorkspaceAsync(workspaceId);
                _loggingService.LogInfo($"Successfully deleted workspace: {workspaceId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in DeleteWorkspace (ID: {workspaceId}): {ex.Message}", ex);
                throw;
            }
        }
    }
}
