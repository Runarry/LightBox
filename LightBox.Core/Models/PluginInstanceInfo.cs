using System;

namespace LightBox.Core.Models
{
    public class PluginInstanceInfo
    {
        public string InstanceId { get; set; }
        public string PluginId { get; set; }
        public string PluginName { get; set; } // 从PluginDefinition获取
        public PluginType Type { get; set; }
        public PluginInstanceStatus Status { get; set; }
        public string WorkspaceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 