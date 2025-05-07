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
    public class PluginManager : IPluginService
    {
        private readonly IApplicationSettingsService _settingsService;
        private readonly ILoggingService _loggingService;
        private List<PluginDefinition> _discoveredPlugins = new List<PluginDefinition>();
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // Allow trailing commas and comments if necessary, though manifest.json should be strict
        };

        public PluginManager(IApplicationSettingsService settingsService, ILoggingService loggingService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public async Task<List<PluginDefinition>> DiscoverPluginsAsync()
        {
            _loggingService.LogInfo("Starting plugin discovery...");
            var settings = await _settingsService.LoadSettingsAsync();
            var scanDirectories = settings.PluginScanDirectories;
            var discovered = new List<PluginDefinition>();

            if (scanDirectories == null || !scanDirectories.Any())
            {
                _loggingService.LogWarning("No plugin scan directories configured.");
                _discoveredPlugins = discovered;
                return discovered;
            }

            foreach (var dir in scanDirectories)
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;

                var directoryPath = Environment.ExpandEnvironmentVariables(dir); // Expand environment variables in paths

                if (!Directory.Exists(directoryPath))
                {
                    _loggingService.LogWarning($"Plugin scan directory not found: {directoryPath}");
                    continue;
                }

                _loggingService.LogInfo($"Scanning directory for plugins: {directoryPath}");
                try
                {
                    var manifestFiles = Directory.GetFiles(directoryPath, "manifest.json", SearchOption.AllDirectories);
                    foreach (var manifestFile in manifestFiles)
                    {
                        _loggingService.LogDebug($"Found potential manifest: {manifestFile}");
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(manifestFile);
                            var pluginDef = JsonSerializer.Deserialize<PluginDefinition>(jsonContent, _jsonSerializerOptions);

                            if (pluginDef != null && !string.IsNullOrWhiteSpace(pluginDef.Id) && !string.IsNullOrWhiteSpace(pluginDef.Name))
                            {
                                // Basic validation passed
                                // Store the directory of the manifest as part of the plugin info if needed later, e.g. for relative paths
                                // For now, just add to the list.
                                discovered.Add(pluginDef);
                                _loggingService.LogInfo($"Successfully loaded plugin: {pluginDef.Name} (ID: {pluginDef.Id}) from {manifestFile}");
                            }
                            else
                            {
                                _loggingService.LogWarning($"Invalid or incomplete manifest file skipped: {manifestFile}. ID or Name is missing.");
                            }
                        }
                        catch (JsonException ex)
                        {
                            _loggingService.LogError($"Error deserializing manifest file {manifestFile}: {ex.Message}");
                        }
                        catch (IOException ex)
                        {
                            _loggingService.LogError($"Error reading manifest file {manifestFile}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"Unexpected error processing manifest file {manifestFile}: {ex.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _loggingService.LogError($"Access denied while scanning directory {directoryPath}: {ex.Message}");
                }
                catch (DirectoryNotFoundException ex) // Should be caught by Directory.Exists, but as a safeguard
                {
                    _loggingService.LogError($"Directory not found during scan (should not happen if Directory.Exists passed): {directoryPath} - {ex.Message}");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"An unexpected error occurred while scanning directory {directoryPath}: {ex.Message}");
                }
            }

            _discoveredPlugins = discovered;
            _loggingService.LogInfo($"Plugin discovery completed. Found {_discoveredPlugins.Count} plugins.");
            return _discoveredPlugins;
        }

        public Task<PluginDefinition?> GetPluginDefinitionByIdAsync(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
            {
                return Task.FromResult<PluginDefinition?>(null);
            }
            var plugin = _discoveredPlugins.FirstOrDefault(p => p.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(plugin);
        }
    }
}