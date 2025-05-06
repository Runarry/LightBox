namespace LightBox.PluginContracts
{
    public interface ILightBoxHostContext
    {
        void Log(LogLevel level, string message);
        string GetWorkspacePath(); // 返回当前活动工作区的相关路径
    }
}