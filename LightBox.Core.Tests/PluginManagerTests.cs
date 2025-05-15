using System;
using System.Linq;
using System.Threading.Tasks;
using LightBox.Core.Models;
using LightBox.Core.Services.Implementations;
using LightBox.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LightBox.Core.Tests
{
    public class PluginManagerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginManager;
        private readonly ILoggingService _logger;

        public PluginManagerTests(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pluginManager = _serviceProvider.GetRequiredService<IPluginService>();
            _logger = _serviceProvider.GetRequiredService<ILoggingService>();
        }

        public async Task RunAllTests()
        {
            _logger.LogInfo("========== PluginManager Tests ==========");
            
            try
            {
                await TestDiscoverPlugins();
                await TestPluginLifecycle();
                await TestMultipleInstances();
                
                _logger.LogInfo("All PluginManager tests completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Tests failed: {ex.Message}", ex);
            }
        }

        private async Task TestDiscoverPlugins()
        {
            _logger.LogInfo("Testing plugin discovery...");
            
            var plugins = await _pluginManager.DiscoverPluginsAsync();
            
            if (plugins == null || !plugins.Any())
            {
                _logger.LogError("No plugins discovered. Make sure test plugins are configured correctly.");
                throw new InvalidOperationException("No plugins discovered");
            }
            
            _logger.LogInfo($"Successfully discovered {plugins.Count} plugins:");
            foreach (var plugin in plugins)
            {
                _logger.LogInfo($"  - {plugin.Name} (ID: {plugin.Id}, Type: {plugin.Type})");
            }
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
    }
} 