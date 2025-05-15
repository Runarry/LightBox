using LightBox.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightBox.Core.Services.Interfaces
{
    public interface IPluginService
    {
        Task<List<PluginDefinition>> DiscoverPluginsAsync();
        Task<PluginDefinition?> GetPluginDefinitionByIdAsync(string pluginId);

        // --- New plugin instance management methods ---

        /// <summary>
        /// Creates a plugin instance based on the plugin definition ID and workspace ID.
        /// </summary>
        /// <param name="pluginId">The plugin definition ID.</param>
        /// <param name="workspaceId">The workspace ID.</param>
        /// <param name="initialConfigurationJson">The initial configuration JSON string for the plugin instance.</param>
        /// <returns>The created plugin instance information.</returns>
        Task<PluginInstanceInfo> CreatePluginInstanceAsync(string pluginId, string workspaceId, string initialConfigurationJson);

        /// <summary>
        /// Initializes a created plugin instance (mainly for C# library plugins).
        /// </summary>
        /// <param name="instanceId">The plugin instance ID.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> InitializePluginInstanceAsync(string instanceId);
        
        /// <summary>
        /// Starts a plugin instance.
        /// </summary>
        /// <param name="instanceId">The plugin instance ID.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> StartPluginInstanceAsync(string instanceId);

        /// <summary>
        /// Stops a plugin instance.
        /// </summary>
        /// <param name="instanceId">The plugin instance ID.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> StopPluginInstanceAsync(string instanceId);

        /// <summary>
        /// Disposes/removes a plugin instance, releasing its resources.
        /// </summary>
        /// <param name="instanceId">The plugin instance ID.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> DisposePluginInstanceAsync(string instanceId);

        /// <summary>
        /// Gets the status of a specified plugin instance.
        /// </summary>
        /// <param name="instanceId">The plugin instance ID.</param>
        /// <returns>The status of the plugin instance.</returns>
        Task<PluginInstanceStatus> GetPluginInstanceStatusAsync(string instanceId);

        /// <summary>
        /// Gets detailed information about a specified plugin instance.
        /// </summary>
        /// <param name="instanceId">The plugin instance ID.</param>
        /// <returns>Detailed information about the plugin instance, or null if not found.</returns>
        Task<PluginInstanceInfo> GetPluginInstanceInfoAsync(string instanceId);
        
        /// <summary>
        /// Gets information about all plugin instances in a specified workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace ID.</param>
        /// <returns>A list of information about all plugin instances in the workspace.</returns>
        Task<IEnumerable<PluginInstanceInfo>> GetPluginInstancesByWorkspaceAsync(string workspaceId);

        /// <summary>
        /// Gets information about all active plugin instances.
        /// </summary>
        /// <returns>A list of information about all active plugin instances.</returns>
        Task<IEnumerable<PluginInstanceInfo>> GetAllActivePluginInstancesAsync();
    }
}