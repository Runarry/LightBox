using LightBox.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightBox.Core.Services.Interfaces
{
    public interface IPluginService
    {
        Task<List<PluginDefinition>> DiscoverPluginsAsync();
        Task<PluginDefinition?> GetPluginDefinitionByIdAsync(string pluginId);
    }
}