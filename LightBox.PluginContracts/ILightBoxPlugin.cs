using System.Threading.Tasks;

namespace LightBox.PluginContracts
{
    public interface ILightBoxPlugin
    {
        string Id { get; } // 由插件实现者确保与 manifest.json 中的 id 一致
        void Initialize(ILightBoxHostContext hostContext, string instanceId, string configurationJson);
        void Start();
        void Stop();
        Task<object> ExecuteCommand(string commandName, object payload); // payload 和返回值可以是简单类型或可序列化对象
    }
}