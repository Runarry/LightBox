using System.Collections.Generic;
using System.Text.Json; // Added for JsonElement
using System.Text.Json.Serialization;

namespace LightBox.Core.Models
{
    public class PluginDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("plugin_type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PluginType Type { get; set; } = PluginType.CSharpLibrary; // Default to CSharpLibrary

        [JsonPropertyName("executable")]
        public string? Executable { get; set; }

        [JsonPropertyName("args_template")]
        public string? ArgsTemplate { get; set; }

        [JsonPropertyName("assembly_path")]
        public string? AssemblyPath { get; set; }

        [JsonPropertyName("main_class")]
        public string? MainClass { get; set; }

        [JsonPropertyName("config_schema")]
        public JsonElement? ConfigSchema { get; set; } // Using JsonElement for flexibility

        [JsonPropertyName("communication")]
        public CommunicationInfo Communication { get; set; } = new CommunicationInfo { Type = "stdio" }; // Changed type to CommunicationInfo

        [JsonPropertyName("icon")]
        public string? Icon { get; set; } // Path to icon file or base64 string
    }

    public class CommunicationInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}