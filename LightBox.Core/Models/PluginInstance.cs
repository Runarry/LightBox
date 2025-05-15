using System;
using System.Diagnostics; // For Process
using LightBox.PluginContracts; // For ILightBoxPlugin

namespace LightBox.Core.Models
{
    public class PluginInstance
    {
        public string InstanceId { get; internal set; } // 由PluginManager生成和管理
        public string PluginId { get; internal set; } // 关联到 PluginDefinition.Id
        public string WorkspaceId { get; internal set; } // 所属工作区ID
        public PluginType Type { get; internal set; } // 从PluginDefinition获取
        public PluginInstanceStatus Status { get; internal set; }
        public string ConfigurationJson { get; internal set; } // 当前实例的配置
        public DateTime CreatedAt { get; internal set; }
        public DateTime? InitializedAt { get; internal set; }
        public DateTime? StartedAt { get; internal set; }
        public DateTime? StoppedAt { get; internal set; }
        public Exception LastError { get; internal set; } // 用于记录最后发生的错误

        // C# 库插件相关
        internal ILightBoxPlugin PluginObject { get; set; } 
        internal AppDomain PluginAppDomain { get; set; } // C#插件的独立AppDomain (可选，用于隔离和卸载)

        // 外部进程插件相关
        internal Process PluginProcess { get; set; }
        internal string TempConfigFilePath {get; set;} // 外部进程插件的临时配置文件路径

        public PluginInstance(string pluginId, string workspaceId, PluginType type, string configurationJson)
        {
            InstanceId = Guid.NewGuid().ToString(); // 生成唯一实例ID
            PluginId = pluginId;
            WorkspaceId = workspaceId;
            Type = type;
            ConfigurationJson = configurationJson;
            Status = PluginInstanceStatus.Created;
            CreatedAt = DateTime.UtcNow;
        }
    }
} 