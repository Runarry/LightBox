using System;
using System.Text.Json.Serialization;

namespace LightBox.Core.Models
{
    public class WorkspaceInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; } // 新增字段

        [JsonPropertyName("lastOpened")]
        public DateTime LastOpened { get; set; }

        public WorkspaceInfo(string id, string name, string filePath, string icon, DateTime lastOpened)
        {
            Id = id;
            Name = name;
            FilePath = filePath;
            Icon = icon;
            LastOpened = lastOpened;
        }
    }
}