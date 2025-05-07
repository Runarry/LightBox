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

        public LightBoxJsBridge(
            IApplicationSettingsService applicationSettingsService,
            IPluginService pluginService,
            ILoggingService loggingService)
        {
            _applicationSettingsService = applicationSettingsService ?? throw new ArgumentNullException(nameof(applicationSettingsService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public async Task<string> GetApplicationSettings() // Restored to async
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
    }
}