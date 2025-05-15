namespace LightBox.Core.Models
{
    public enum PluginInstanceStatus
    {
        Unknown,      // 未知状态
        Created,      // 实例已创建，但未初始化/启动
        Initializing, // 正在初始化 (主要用于C#插件)
        Initialized,  // 已初始化 (主要用于C#插件)
        Starting,     // 正在启动
        Running,      // 正在运行
        Stopping,     // 正在停止
        Stopped,      // 已停止
        Disposing,    // 正在卸载/释放资源
        Disposed,     // 已卸载/释放资源
        Error         // 发生错误
    }
} 