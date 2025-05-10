using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LightBox.Core.Models
{
    public class Workspace
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; } // 新增字段

        [JsonPropertyName("pluginInstances")]
        public List<PluginInstanceEntry> PluginInstances { get; set; } // 假设 PluginInstanceEntry 已定义或稍后定义

        public Workspace(string id, string name, string description, string icon)
        {
            Id = id;
            Name = name;
            Description = description;
            Icon = icon;
            PluginInstances = new List<PluginInstanceEntry>();
        }
    }

    // 占位符，实际项目中可能已存在或需要更详细定义
    public class PluginInstanceEntry
    {
        [JsonPropertyName("instanceId")]
        public string InstanceId { get; set; }

        [JsonPropertyName("pluginId")]
        public string PluginId { get; set; }

        [JsonPropertyName("configuration")]
        public object Configuration { get; set; } // 可以是具体的类型或 JsonElement

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("lastErrorMessage")]
        public string LastErrorMessage { get; set; }
    }
}