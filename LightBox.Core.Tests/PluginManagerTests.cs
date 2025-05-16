using System;
using System.Linq;
using System.Threading.Tasks;
using LightBox.Core.Models;
using LightBox.Core.Services.Implementations;
using LightBox.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json;
using System.Text;

namespace LightBox.Core.Tests
{
    public class PluginManagerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginManager;
        private readonly ILoggingService _logger;
        private readonly IApplicationSettingsService _applicationSettingsService;
        private readonly HttpClient _httpClient;

        public PluginManagerTests(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pluginManager = _serviceProvider.GetRequiredService<IPluginService>();
            _logger = _serviceProvider.GetRequiredService<ILoggingService>();
            _applicationSettingsService = _serviceProvider.GetRequiredService<IApplicationSettingsService>();
            _httpClient = new HttpClient();
        }

        public async Task RunAllTests()
        {
            _logger.LogInfo("========== PluginManager Tests ==========");
            
            try
            {
                await TestDiscoverPlugins();
                await TestPluginLifecycle();
                await TestMultipleInstances();
                
                // New tests for PluginA and PluginB
                await TestDiscoverPluginsA_And_B();
                await TestPluginA_Commands();
                await TestPluginB_Process();
                
                _logger.LogInfo("All PluginManager tests completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Tests failed: {ex.Message}", ex);
            }
        }

        private async Task TestDiscoverPlugins()
        {
            _logger.LogInfo("Testing plugin discovery...");

            // 获取配置的插件扫描目录
            var settings = await _applicationSettingsService.LoadSettingsAsync();
            _logger.LogInfo($"Plugin scan directories configured: {settings.PluginScanDirectories.Count}");
            
            foreach (var dir in settings.PluginScanDirectories)
            {
                _logger.LogInfo($"Scan directory: {dir}");
                if (!Directory.Exists(dir))
                {
                    _logger.LogError($"Directory does not exist: {dir}");
                }
                else
                {
                    _logger.LogInfo($"Directory exists: {dir}");
                    // 检查manifest.json是否存在
                    var manifestPath = Path.Combine(dir, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        _logger.LogInfo($"Found manifest.json at: {manifestPath}");
                    }
                    else
                    {
                        _logger.LogError($"manifest.json not found at: {manifestPath}");
                    }
                }
            }

            var plugins = await _pluginManager.DiscoverPluginsAsync();
            
            _logger.LogInfo($"Discovered {plugins.Count} plugins.");

            if (plugins == null || !plugins.Any())
            {
                _logger.LogError("No plugins found. Discovery failed.");
                return;
            }

            foreach (var plugin in plugins)
            {
                _logger.LogInfo($"Found plugin: {plugin.Id} - {plugin.Name} (Type: {plugin.Type})");
            }

            _logger.LogInfo("Plugin discovery test completed.");
        }

        private async Task TestPluginLifecycle()
        {
            _logger.LogInfo("Testing plugin lifecycle (create, initialize, start, stop, dispose)...");
            
            // First discover plugins
            var plugins = await _pluginManager.DiscoverPluginsAsync();
            if (!plugins.Any()) throw new InvalidOperationException("No plugins available for lifecycle test");
            
            // Use the first plugin for testing
            var plugin = plugins.First();
            _logger.LogInfo($"Using plugin for lifecycle test: {plugin.Name} (ID: {plugin.Id}, Type: {plugin.Type})");
            
            const string workspaceId = "test-workspace-1";
            const string configJson = "{}"; // Empty config for testing
            
            // 1. Create the plugin instance
            _logger.LogInfo("1. Creating plugin instance...");
            var instanceInfo = await _pluginManager.CreatePluginInstanceAsync(plugin.Id, workspaceId, configJson);
            
            _logger.LogInfo($"Plugin instance created: {instanceInfo.InstanceId} (Status: {instanceInfo.Status})");
            
            // 2. Initialize the plugin instance
            _logger.LogInfo("2. Initializing plugin instance...");
            var initResult = await _pluginManager.InitializePluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!initResult)
            {
                _logger.LogError("Failed to initialize plugin instance");
                throw new InvalidOperationException("Plugin initialization failed");
            }
            
            var statusAfterInit = await _pluginManager.GetPluginInstanceStatusAsync(instanceInfo.InstanceId);
            _logger.LogInfo($"Plugin instance initialized (Status: {statusAfterInit})");
            
            // 3. Start the plugin instance
            _logger.LogInfo("3. Starting plugin instance...");
            var startResult = await _pluginManager.StartPluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!startResult)
            {
                _logger.LogError("Failed to start plugin instance");
                throw new InvalidOperationException("Plugin start failed");
            }
            
            var statusAfterStart = await _pluginManager.GetPluginInstanceStatusAsync(instanceInfo.InstanceId);
            _logger.LogInfo($"Plugin instance started (Status: {statusAfterStart})");
            
            // 4. Check plugin instance info
            _logger.LogInfo("4. Getting plugin instance info...");
            var detailedInfo = await _pluginManager.GetPluginInstanceInfoAsync(instanceInfo.InstanceId);
            
            _logger.LogInfo($"Plugin instance info: {detailedInfo.PluginName} (Status: {detailedInfo.Status})");
            
            // 5. Stop the plugin instance
            _logger.LogInfo("5. Stopping plugin instance...");
            var stopResult = await _pluginManager.StopPluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!stopResult)
            {
                _logger.LogError("Failed to stop plugin instance");
                throw new InvalidOperationException("Plugin stop failed");
            }
            
            var statusAfterStop = await _pluginManager.GetPluginInstanceStatusAsync(instanceInfo.InstanceId);
            _logger.LogInfo($"Plugin instance stopped (Status: {statusAfterStop})");
            
            // 6. Dispose the plugin instance
            _logger.LogInfo("6. Disposing plugin instance...");
            var disposeResult = await _pluginManager.DisposePluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!disposeResult)
            {
                _logger.LogError("Failed to dispose plugin instance");
                throw new InvalidOperationException("Plugin dispose failed");
            }
            
            // Instance should no longer be available after disposal
            var disposedInstanceInfo = await _pluginManager.GetPluginInstanceInfoAsync(instanceInfo.InstanceId);
            
            if (disposedInstanceInfo != null)
            {
                _logger.LogWarning("Plugin instance still exists after disposal");
            }
            else
            {
                _logger.LogInfo("Plugin instance successfully disposed");
            }
            
            _logger.LogInfo("Plugin lifecycle test completed successfully");
        }

        private async Task TestMultipleInstances()
        {
            _logger.LogInfo("Testing multiple plugin instances...");
            
            // First discover plugins
            var plugins = await _pluginManager.DiscoverPluginsAsync();
            if (!plugins.Any()) throw new InvalidOperationException("No plugins available for multiple instances test");
            
            // Use the first plugin for testing
            var plugin = plugins.First();
            
            const string workspaceId = "test-workspace-1";
            const string configJson = "{}"; // Empty config for testing
            
            // Create multiple instances
            _logger.LogInfo("Creating multiple instances of the same plugin...");
            var instance1 = await _pluginManager.CreatePluginInstanceAsync(plugin.Id, workspaceId, configJson);
            var instance2 = await _pluginManager.CreatePluginInstanceAsync(plugin.Id, workspaceId, configJson);
            var instance3 = await _pluginManager.CreatePluginInstanceAsync(plugin.Id, workspaceId, configJson);
            
            _logger.LogInfo($"Created instances: {instance1.InstanceId}, {instance2.InstanceId}, {instance3.InstanceId}");
            
            // Initialize all instances
            await _pluginManager.InitializePluginInstanceAsync(instance1.InstanceId);
            await _pluginManager.InitializePluginInstanceAsync(instance2.InstanceId);
            await _pluginManager.InitializePluginInstanceAsync(instance3.InstanceId);
            
            // Start instance 1 and 2, leave instance 3 initialized but not started
            await _pluginManager.StartPluginInstanceAsync(instance1.InstanceId);
            await _pluginManager.StartPluginInstanceAsync(instance2.InstanceId);
            
            // Get all instances for workspace
            _logger.LogInfo("Getting all instances for workspace...");
            var workspaceInstances = await _pluginManager.GetPluginInstancesByWorkspaceAsync(workspaceId);
            
            _logger.LogInfo($"Found {workspaceInstances.Count()} instances for workspace {workspaceId}");
            foreach (var instance in workspaceInstances)
            {
                _logger.LogInfo($"  - {instance.InstanceId} (Status: {instance.Status})");
            }
            
            // Get all active instances
            _logger.LogInfo("Getting all active instances...");
            var allInstances = await _pluginManager.GetAllActivePluginInstancesAsync();
            
            _logger.LogInfo($"Found {allInstances.Count()} active instances across all workspaces");
            
            // Clean up - dispose all instances
            _logger.LogInfo("Cleaning up instances...");
            await _pluginManager.StopPluginInstanceAsync(instance1.InstanceId);
            await _pluginManager.StopPluginInstanceAsync(instance2.InstanceId);
            
            await _pluginManager.DisposePluginInstanceAsync(instance1.InstanceId);
            await _pluginManager.DisposePluginInstanceAsync(instance2.InstanceId);
            await _pluginManager.DisposePluginInstanceAsync(instance3.InstanceId);
            
            _logger.LogInfo("Multiple instances test completed successfully");
        }

        // New test methods for PluginA and PluginB

        private async Task TestDiscoverPluginsA_And_B()
        {
            _logger.LogInfo("========== Testing PluginA and PluginB Discovery ==========");

            var plugins = await _pluginManager.DiscoverPluginsAsync();
            
            // Check if PluginA and PluginB are in the discovered plugins
            var pluginA = plugins.FirstOrDefault(p => p.Id == "test.plugin.a");
            var pluginB = plugins.FirstOrDefault(p => p.Id == "test.plugin.b");
            
            if (pluginA == null)
            {
                _logger.LogError("PluginA (test.plugin.a) was not found in discovered plugins.");
                throw new InvalidOperationException("PluginA not found");
            }
            
            if (pluginB == null)
            {
                _logger.LogError("PluginB (test.plugin.b) was not found in discovered plugins.");
                throw new InvalidOperationException("PluginB not found");
            }
            
            _logger.LogInfo($"Found PluginA: {pluginA.Name} (Type: {pluginA.Type})");
            _logger.LogInfo($"Found PluginB: {pluginB.Name} (Type: {pluginB.Type})");
            
            // Verify properties from manifest
            if (pluginA.Name != "Test Plugin A (C# Library)")
            {
                _logger.LogError($"PluginA name mismatch. Expected: 'Test Plugin A (C# Library)', Got: '{pluginA.Name}'");
            }
            
            if (pluginB.Name != "Test Plugin B (External Process)")
            {
                _logger.LogError($"PluginB name mismatch. Expected: 'Test Plugin B (External Process)', Got: '{pluginB.Name}'");
            }
            
            _logger.LogInfo("PluginA and PluginB discovery test passed.");
        }
        
        private async Task TestPluginA_Commands()
        {
            _logger.LogInfo("========== Testing PluginA Commands ==========");
            
            // First discover plugins
            var plugins = await _pluginManager.DiscoverPluginsAsync();
            var pluginA = plugins.FirstOrDefault(p => p.Id == "test.plugin.a");
            
            if (pluginA == null)
            {
                _logger.LogError("PluginA not found for command test.");
                throw new InvalidOperationException("PluginA not found");
            }
            
            const string workspaceId = "test-workspace-commands";
            string configJson = "{\"messagePrefix\": \"TestPrefix:\"}";
            
            // Create and initialize the plugin instance
            _logger.LogInfo("Creating PluginA instance...");
            var instanceInfo = await _pluginManager.CreatePluginInstanceAsync(pluginA.Id, workspaceId, configJson);
            
            _logger.LogInfo($"PluginA instance created: {instanceInfo.InstanceId} (Status: {instanceInfo.Status})");
            
            // Initialize the plugin instance
            _logger.LogInfo("Initializing PluginA instance...");
            var initResult = await _pluginManager.InitializePluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!initResult)
            {
                _logger.LogError("Failed to initialize PluginA instance");
                throw new InvalidOperationException("PluginA initialization failed");
            }
            
            // Start the plugin instance
            _logger.LogInfo("Starting PluginA instance...");
            var startResult = await _pluginManager.StartPluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!startResult)
            {
                _logger.LogError("Failed to start PluginA instance");
                throw new InvalidOperationException("PluginA start failed");
            }
            
            // Test echo command
            _logger.LogInfo("Testing 'echo' command...");
            var echoPayload = "Hello, Plugin!";
            var echoResult = await _pluginManager.ExecuteCommandAsync(instanceInfo.InstanceId, "echo", echoPayload);
            
            _logger.LogInfo($"Echo command result: {echoResult}");
            if (echoResult?.ToString() != $"PluginA echoes: {echoPayload}")
            {
                _logger.LogError($"Echo command failed. Expected: 'PluginA echoes: {echoPayload}', Got: '{echoResult}'");
            }
            
            // Test add command with valid payload
            _logger.LogInfo("Testing 'add' command with valid payload...");
            var addPayload = JsonSerializer.Serialize(new { a = 5, b = 3 });
            var addResult = await _pluginManager.ExecuteCommandAsync(instanceInfo.InstanceId, "add", addPayload);
            
            _logger.LogInfo($"Add command result: {addResult}");
            if (Convert.ToInt32(addResult) != 8)
            {
                _logger.LogError($"Add command failed. Expected: 8, Got: '{addResult}'");
            }
            
            // Test add command with invalid payload
            _logger.LogInfo("Testing 'add' command with invalid payload...");
            var invalidPayload = JsonSerializer.Serialize(new { x = 5, y = 3 });
            var invalidResult = await _pluginManager.ExecuteCommandAsync(instanceInfo.InstanceId, "add", invalidPayload);
            
            _logger.LogInfo($"Add command with invalid payload result: {invalidResult}");
            if (!invalidResult.ToString().Contains("Invalid payload"))
            {
                _logger.LogError($"Add command with invalid payload should return error message. Got: '{invalidResult}'");
            }
            
            // Test unknown command
            _logger.LogInfo("Testing unknown command...");
            var unknownResult = await _pluginManager.ExecuteCommandAsync(instanceInfo.InstanceId, "unknown", "test");
            
            _logger.LogInfo($"Unknown command result: {unknownResult}");
            if (!unknownResult.ToString().Contains("Unknown command"))
            {
                _logger.LogError($"Unknown command should return error message. Got: '{unknownResult}'");
            }
            
            // Stop the plugin instance
            _logger.LogInfo("Stopping PluginA instance...");
            var stopResult = await _pluginManager.StopPluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!stopResult)
            {
                _logger.LogError("Failed to stop PluginA instance");
            }
            
            // Dispose the plugin instance
            await _pluginManager.DisposePluginInstanceAsync(instanceInfo.InstanceId);
            
            _logger.LogInfo("PluginA commands test completed.");
        }
        
        private async Task TestPluginB_Process()
        {
            _logger.LogInfo("========== Testing PluginB Process ==========");
            
            // First discover plugins
            var plugins = await _pluginManager.DiscoverPluginsAsync();
            var pluginB = plugins.FirstOrDefault(p => p.Id == "test.plugin.b");
            
            if (pluginB == null)
            {
                _logger.LogError("PluginB not found for process test.");
                throw new InvalidOperationException("PluginB not found");
            }
            
            const string workspaceId = "test-workspace-process";
            string configJson = "{\"port\": 8092, \"startupMessage\": \"PluginB Test\"}";
            
            // Create the plugin instance
            _logger.LogInfo("Creating PluginB instance...");
            var instanceInfo = await _pluginManager.CreatePluginInstanceAsync(pluginB.Id, workspaceId, configJson);
            
            _logger.LogInfo($"PluginB instance created: {instanceInfo.InstanceId} (Status: {instanceInfo.Status})");
            
            // Wait a bit for the process to start and HTTP server to initialize
            await Task.Delay(2000);
            
            // Test HTTP endpoint
            try
            {
                _logger.LogInfo("Testing PluginB HTTP endpoint...");
                var response = await _httpClient.GetStringAsync("http://localhost:8092/ping");
                _logger.LogInfo($"PluginB HTTP response: {response}");
                
                if (!response.Contains("PluginB") || !response.Contains("pong"))
                {
                    _logger.LogError($"Unexpected response from PluginB: {response}");
                }
                else
                {
                    _logger.LogInfo("PluginB HTTP endpoint test passed.");
                }
                
                // Test config endpoint
                var configResponse = await _httpClient.GetStringAsync("http://localhost:8092/config");
                _logger.LogInfo($"PluginB config endpoint response: {configResponse}");
                
                if (!configResponse.Contains("config endpoint"))
                {
                    _logger.LogError($"Unexpected response from PluginB config endpoint: {configResponse}");
                }
                else
                {
                    _logger.LogInfo("PluginB config endpoint test passed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error testing PluginB HTTP endpoint: {ex.Message}");
            }
            
            // Stop the plugin instance
            _logger.LogInfo("Stopping PluginB instance...");
            var stopResult = await _pluginManager.StopPluginInstanceAsync(instanceInfo.InstanceId);
            
            if (!stopResult)
            {
                _logger.LogError("Failed to stop PluginB instance");
            }
            
            // Wait a bit for the process to stop
            await Task.Delay(1000);
            
            // Verify process is terminated
            try
            {
                _logger.LogInfo("Verifying PluginB process is terminated...");
                var response = await _httpClient.GetStringAsync("http://localhost:8092/ping");
                _logger.LogError("PluginB process is still running after stop!");
            }
            catch (HttpRequestException)
            {
                _logger.LogInfo("PluginB process successfully terminated.");
            }
            
            // Dispose the plugin instance
            await _pluginManager.DisposePluginInstanceAsync(instanceInfo.InstanceId);
            
            _logger.LogInfo("PluginB process test completed.");
        }
    }
} 