using LightBox.Core.Models;
using LightBox.Core.Services.Interfaces;
using LightBox.Core.Host;
using LightBox.PluginContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

        // Store active plugin instances in a thread-safe dictionary
        private readonly ConcurrentDictionary<string, PluginInstance> _activeInstances = new ConcurrentDictionary<string, PluginInstance>();

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

        // New plugin instance management methods

        public async Task<PluginInstanceInfo> CreatePluginInstanceAsync(string pluginId, string workspaceId, string initialConfigurationJson)
        {
            _loggingService.LogInfo($"Creating plugin instance for plugin {pluginId} in workspace {workspaceId}");
            
            try
            {
                // Get plugin definition
                var pluginDefinition = await GetPluginDefinitionByIdAsync(pluginId);
                if (pluginDefinition == null)
                {
                    throw new ArgumentException($"Plugin with ID '{pluginId}' was not found.");
                }

                // Create plugin instance
                var instance = new PluginInstance(pluginId, workspaceId, pluginDefinition.Type, initialConfigurationJson);
                
                // Add to active instances
                if (!_activeInstances.TryAdd(instance.InstanceId, instance))
                {
                    throw new InvalidOperationException("Failed to add plugin instance to active instances.");
                }

                _loggingService.LogInfo($"Successfully created plugin instance {instance.InstanceId} for plugin {pluginId}");
                
                // Return plugin instance info
                return ConvertToPluginInstanceInfo(instance, pluginDefinition.Name);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating plugin instance for plugin {pluginId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> InitializePluginInstanceAsync(string instanceId)
        {
            _loggingService.LogInfo($"Initializing plugin instance {instanceId}");
            
            try
            {
                // Get plugin instance
                if (!_activeInstances.TryGetValue(instanceId, out var instance))
                {
                    throw new ArgumentException($"Plugin instance with ID '{instanceId}' was not found.");
                }

                // Check instance state
                if (instance.Status != PluginInstanceStatus.Created)
                {
                    throw new InvalidOperationException($"Cannot initialize plugin instance {instanceId} because it is in {instance.Status} state (expected Created).");
                }

                // For C# library plugins
                if (instance.Type == PluginType.CSharpLibrary)
                {
                    // Update status
                    instance.Status = PluginInstanceStatus.Initializing;
                    
                    // Get plugin definition
                    var pluginDefinition = await GetPluginDefinitionByIdAsync(instance.PluginId);
                    if (pluginDefinition == null)
                    {
                        throw new InvalidOperationException($"Plugin definition with ID '{instance.PluginId}' was not found.");
                    }

                    if (string.IsNullOrWhiteSpace(pluginDefinition.AssemblyPath))
                    {
                        throw new InvalidOperationException($"Plugin definition with ID '{instance.PluginId}' has no assembly path specified.");
                    }

                    if (string.IsNullOrWhiteSpace(pluginDefinition.MainClass))
                    {
                        throw new InvalidOperationException($"Plugin definition with ID '{instance.PluginId}' has no main class specified.");
                    }

                    // Load the assembly and create an instance of the plugin
                    try
                    {
                        // For MVP, load in the current AppDomain
                        // TODO: Consider using a separate AppDomain for better isolation
                        var assemblyPath = Path.GetFullPath(pluginDefinition.AssemblyPath);
                        var assembly = Assembly.LoadFrom(assemblyPath);
                        
                        var pluginType = assembly.GetType(pluginDefinition.MainClass);
                        if (pluginType == null)
                        {
                            throw new InvalidOperationException($"Plugin main class '{pluginDefinition.MainClass}' was not found in assembly '{assemblyPath}'.");
                        }

                        var pluginObject = Activator.CreateInstance(pluginType) as ILightBoxPlugin;
                        if (pluginObject == null)
                        {
                            throw new InvalidOperationException($"Plugin main class '{pluginDefinition.MainClass}' does not implement ILightBoxPlugin interface.");
                        }

                        // Store the plugin object in the instance
                        instance.PluginObject = pluginObject;

                        // Create host context and initialize the plugin
                        var hostContext = new LightBoxHostContext(_loggingService);
                        instance.PluginObject.Initialize(hostContext, instance.InstanceId, instance.ConfigurationJson);

                        // Update status
                        instance.Status = PluginInstanceStatus.Initialized;
                        instance.InitializedAt = DateTime.UtcNow;

                        _loggingService.LogInfo($"Successfully initialized C# library plugin instance {instanceId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        instance.Status = PluginInstanceStatus.Error;
                        instance.LastError = ex;
                        _loggingService.LogError($"Error initializing C# library plugin instance {instanceId}: {ex.Message}");
                        throw;
                    }
                }
                else if (instance.Type == PluginType.ExternalProcess)
                {
                    // For external process plugins, initialization is simpler
                    // Just update the status to Initialized
                    instance.Status = PluginInstanceStatus.Initialized;
                    instance.InitializedAt = DateTime.UtcNow;
                    _loggingService.LogInfo($"Successfully initialized external process plugin instance {instanceId}");
                    return true;
                }
                else
                {
                    throw new NotSupportedException($"Unsupported plugin type: {instance.Type}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error initializing plugin instance {instanceId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> StartPluginInstanceAsync(string instanceId)
        {
            _loggingService.LogInfo($"Starting plugin instance {instanceId}");
            
            try
            {
                // Get plugin instance
                if (!_activeInstances.TryGetValue(instanceId, out var instance))
                {
                    throw new ArgumentException($"Plugin instance with ID '{instanceId}' was not found.");
                }

                // Check instance state
                if (instance.Type == PluginType.CSharpLibrary && instance.Status != PluginInstanceStatus.Initialized && instance.Status != PluginInstanceStatus.Stopped)
                {
                    throw new InvalidOperationException($"Cannot start C# library plugin instance {instanceId} because it is in {instance.Status} state (expected Initialized or Stopped).");
                }
                else if (instance.Type == PluginType.ExternalProcess && 
                        instance.Status != PluginInstanceStatus.Created && 
                        instance.Status != PluginInstanceStatus.Initialized && 
                        instance.Status != PluginInstanceStatus.Stopped)
                {
                    throw new InvalidOperationException($"Cannot start external process plugin instance {instanceId} because it is in {instance.Status} state (expected Created, Initialized, or Stopped).");
                }

                // Update status
                instance.Status = PluginInstanceStatus.Starting;

                // For C# library plugins
                if (instance.Type == PluginType.CSharpLibrary)
                {
                    // Check if the plugin object exists
                    if (instance.PluginObject == null)
                    {
                        throw new InvalidOperationException($"Plugin object is null for instance {instanceId}. Initialize the plugin first.");
                    }

                    try
                    {
                        // Start the plugin
                        instance.PluginObject.Start();

                        // Update status
                        instance.Status = PluginInstanceStatus.Running;
                        instance.StartedAt = DateTime.UtcNow;

                        _loggingService.LogInfo($"Successfully started C# library plugin instance {instanceId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        instance.Status = PluginInstanceStatus.Error;
                        instance.LastError = ex;
                        _loggingService.LogError($"Error starting C# library plugin instance {instanceId}: {ex.Message}");
                        throw;
                    }
                }
                else if (instance.Type == PluginType.ExternalProcess)
                {
                    try
                    {
                        // Get plugin definition
                        var pluginDefinition = await GetPluginDefinitionByIdAsync(instance.PluginId);
                        if (pluginDefinition == null)
                        {
                            throw new InvalidOperationException($"Plugin definition with ID '{instance.PluginId}' was not found.");
                        }

                        if (string.IsNullOrWhiteSpace(pluginDefinition.Executable))
                        {
                            throw new InvalidOperationException($"Plugin definition with ID '{instance.PluginId}' has no executable specified.");
                        }

                        // Prepare configuration file if needed
                        var tempConfigFilePath = Path.Combine(Path.GetTempPath(), $"lightbox_plugin_{instance.InstanceId}_config.json");
                        File.WriteAllText(tempConfigFilePath, instance.ConfigurationJson);
                        instance.TempConfigFilePath = tempConfigFilePath;

                        // Format arguments
                        string formattedArgs = string.Empty;
                        if (!string.IsNullOrWhiteSpace(pluginDefinition.ArgsTemplate))
                        {
                            formattedArgs = pluginDefinition.ArgsTemplate
                                .Replace("{instanceId}", instance.InstanceId)
                                .Replace("{configPath}", tempConfigFilePath)
                                .Replace("{workspaceId}", instance.WorkspaceId);
                        }

                        // Start the process
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = pluginDefinition.Executable,
                            Arguments = formattedArgs,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = false
                        };

                        var process = Process.Start(startInfo);
                        if (process == null)
                        {
                            throw new InvalidOperationException($"Failed to start process for plugin instance {instanceId}.");
                        }

                        // Capture output and error asynchronously
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                _loggingService.LogInfo($"[Plugin {instance.InstanceId}]: {e.Data}");
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                _loggingService.LogError($"[Plugin {instance.InstanceId}]: {e.Data}");
                            }
                        };
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        // Handle process exit
                        process.Exited += (sender, e) =>
                        {
                            if (process.ExitCode != 0)
                            {
                                _loggingService.LogError($"Plugin process for instance {instance.InstanceId} exited with code {process.ExitCode}");
                                instance.Status = PluginInstanceStatus.Error;
                                instance.LastError = new Exception($"Process exited with code {process.ExitCode}");
                            }
                            else
                            {
                                _loggingService.LogInfo($"Plugin process for instance {instance.InstanceId} exited normally");
                                instance.Status = PluginInstanceStatus.Stopped;
                                instance.StoppedAt = DateTime.UtcNow;
                            }
                        };
                        process.EnableRaisingEvents = true;

                        // Store the process in the instance
                        instance.PluginProcess = process;

                        // Update status
                        instance.Status = PluginInstanceStatus.Running;
                        instance.StartedAt = DateTime.UtcNow;

                        _loggingService.LogInfo($"Successfully started external process plugin instance {instanceId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        instance.Status = PluginInstanceStatus.Error;
                        instance.LastError = ex;
                        
                        // Clean up temp config file if it exists
                        if (!string.IsNullOrEmpty(instance.TempConfigFilePath) && File.Exists(instance.TempConfigFilePath))
                        {
                            try
                            {
                                File.Delete(instance.TempConfigFilePath);
                            }
                            catch (Exception deleteEx)
                            {
                                _loggingService.LogWarning($"Failed to delete temp config file {instance.TempConfigFilePath}: {deleteEx.Message}");
                            }
                        }
                        
                        _loggingService.LogError($"Error starting external process plugin instance {instanceId}: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    throw new NotSupportedException($"Unsupported plugin type: {instance.Type}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error starting plugin instance {instanceId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> StopPluginInstanceAsync(string instanceId)
        {
            _loggingService.LogInfo($"Stopping plugin instance {instanceId}");
            
            try
            {
                // Get plugin instance
                if (!_activeInstances.TryGetValue(instanceId, out var instance))
                {
                    throw new ArgumentException($"Plugin instance with ID '{instanceId}' was not found.");
                }

                // Check instance state
                if (instance.Status != PluginInstanceStatus.Running)
                {
                    throw new InvalidOperationException($"Cannot stop plugin instance {instanceId} because it is in {instance.Status} state (expected Running).");
                }

                // Update status
                instance.Status = PluginInstanceStatus.Stopping;

                // For C# library plugins
                if (instance.Type == PluginType.CSharpLibrary)
                {
                    try
                    {
                        // Stop the plugin
                        instance.PluginObject?.Stop();

                        // Update status
                        instance.Status = PluginInstanceStatus.Stopped;
                        instance.StoppedAt = DateTime.UtcNow;

                        _loggingService.LogInfo($"Successfully stopped C# library plugin instance {instanceId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        instance.Status = PluginInstanceStatus.Error;
                        instance.LastError = ex;
                        _loggingService.LogError($"Error stopping C# library plugin instance {instanceId}: {ex.Message}");
                        throw;
                    }
                }
                else if (instance.Type == PluginType.ExternalProcess)
                {
                    try
                    {
                        // Stop the process
                        if (instance.PluginProcess != null && !instance.PluginProcess.HasExited)
                        {
                            // Try to close the process gracefully
                            if (!instance.PluginProcess.CloseMainWindow())
                            {
                                // If that fails, kill it
                                instance.PluginProcess.Kill();
                            }

                            // Wait for the process to exit
                            instance.PluginProcess.WaitForExit(5000); // Wait up to 5 seconds

                            // If it's still not exited, force kill
                            if (!instance.PluginProcess.HasExited)
                            {
                                instance.PluginProcess.Kill(true);
                                instance.PluginProcess.WaitForExit();
                            }
                        }

                        // Clean up temp config file if it exists
                        if (!string.IsNullOrEmpty(instance.TempConfigFilePath) && File.Exists(instance.TempConfigFilePath))
                        {
                            File.Delete(instance.TempConfigFilePath);
                            instance.TempConfigFilePath = null;
                        }

                        // Update status
                        instance.Status = PluginInstanceStatus.Stopped;
                        instance.StoppedAt = DateTime.UtcNow;

                        _loggingService.LogInfo($"Successfully stopped external process plugin instance {instanceId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        instance.Status = PluginInstanceStatus.Error;
                        instance.LastError = ex;
                        _loggingService.LogError($"Error stopping external process plugin instance {instanceId}: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    throw new NotSupportedException($"Unsupported plugin type: {instance.Type}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error stopping plugin instance {instanceId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DisposePluginInstanceAsync(string instanceId)
        {
            _loggingService.LogInfo($"Disposing plugin instance {instanceId}");
            
            try
            {
                // Get plugin instance
                if (!_activeInstances.TryGetValue(instanceId, out var instance))
                {
                    throw new ArgumentException($"Plugin instance with ID '{instanceId}' was not found.");
                }

                // Check if the instance is running and stop it if necessary
                if (instance.Status == PluginInstanceStatus.Running)
                {
                    await StopPluginInstanceAsync(instanceId);
                }

                // Update status
                instance.Status = PluginInstanceStatus.Disposing;

                // For C# library plugins
                if (instance.Type == PluginType.CSharpLibrary)
                {
                    try
                    {
                        // Dispose the plugin if it implements IDisposable
                        if (instance.PluginObject is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        instance.PluginObject = null;

                        // TODO: If we use a separate AppDomain, unload it here
                        // if (instance.PluginAppDomain != null)
                        // {
                        //     AppDomain.Unload(instance.PluginAppDomain);
                        //     instance.PluginAppDomain = null;
                        // }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"Error disposing C# library plugin instance {instanceId}: {ex.Message}");
                        // Continue with removal even if there's an error
                    }
                }
                else if (instance.Type == PluginType.ExternalProcess)
                {
                    try
                    {
                        // Dispose the process
                        instance.PluginProcess?.Dispose();
                        instance.PluginProcess = null;

                        // Clean up temp config file if it exists
                        if (!string.IsNullOrEmpty(instance.TempConfigFilePath) && File.Exists(instance.TempConfigFilePath))
                        {
                            File.Delete(instance.TempConfigFilePath);
                            instance.TempConfigFilePath = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"Error cleaning up resources for external process plugin instance {instanceId}: {ex.Message}");
                        // Continue with removal even if there's an error
                    }
                }

                // Remove from active instances
                if (_activeInstances.TryRemove(instanceId, out _))
                {
                    instance.Status = PluginInstanceStatus.Disposed;
                    _loggingService.LogInfo($"Successfully disposed plugin instance {instanceId}");
                    return true;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to remove plugin instance {instanceId} from active instances.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error disposing plugin instance {instanceId}: {ex.Message}");
                throw;
            }
        }

        public Task<PluginInstanceStatus> GetPluginInstanceStatusAsync(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || !_activeInstances.TryGetValue(instanceId, out var instance))
            {
                return Task.FromResult(PluginInstanceStatus.Unknown);
            }
            
            return Task.FromResult(instance.Status);
        }

        public async Task<PluginInstanceInfo> GetPluginInstanceInfoAsync(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || !_activeInstances.TryGetValue(instanceId, out var instance))
            {
                return null;
            }
            
            var pluginDefinition = await GetPluginDefinitionByIdAsync(instance.PluginId);
            string pluginName = pluginDefinition?.Name ?? "Unknown Plugin";
            
            return ConvertToPluginInstanceInfo(instance, pluginName);
        }

        public async Task<IEnumerable<PluginInstanceInfo>> GetPluginInstancesByWorkspaceAsync(string workspaceId)
        {
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                return Enumerable.Empty<PluginInstanceInfo>();
            }
            
            var instances = _activeInstances.Values
                .Where(i => i.WorkspaceId.Equals(workspaceId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            var results = new List<PluginInstanceInfo>();
            
            foreach (var instance in instances)
            {
                var pluginDefinition = await GetPluginDefinitionByIdAsync(instance.PluginId);
                string pluginName = pluginDefinition?.Name ?? "Unknown Plugin";
                
                results.Add(ConvertToPluginInstanceInfo(instance, pluginName));
            }
            
            return results;
        }

        public async Task<IEnumerable<PluginInstanceInfo>> GetAllActivePluginInstancesAsync()
        {
            var results = new List<PluginInstanceInfo>();
            
            foreach (var instance in _activeInstances.Values)
            {
                var pluginDefinition = await GetPluginDefinitionByIdAsync(instance.PluginId);
                string pluginName = pluginDefinition?.Name ?? "Unknown Plugin";
                
                results.Add(ConvertToPluginInstanceInfo(instance, pluginName));
            }
            
            return results;
        }

        // Helper method to convert PluginInstance to PluginInstanceInfo
        private PluginInstanceInfo ConvertToPluginInstanceInfo(PluginInstance instance, string pluginName)
        {
            return new PluginInstanceInfo
            {
                InstanceId = instance.InstanceId,
                PluginId = instance.PluginId,
                PluginName = pluginName,
                Type = instance.Type,
                Status = instance.Status,
                WorkspaceId = instance.WorkspaceId,
                CreatedAt = instance.CreatedAt
            };
        }
    }
}