using LightBox.PluginContracts;
using System;
using System.Threading.Tasks;

namespace TestPlugins.PluginA
{
    public class Plugin : ILightBoxPlugin
    {
        private ILightBoxHostContext _hostContext;
        private string _instanceId;

        public string Id => "test.plugin.a";

        public void Initialize(ILightBoxHostContext hostContext, string instanceId, string configurationJson)
        {
            _hostContext = hostContext;
            _instanceId = instanceId;
            _hostContext.Log(LogLevel.Info, $"PluginA (Instance: {_instanceId}): Initialized with config: {configurationJson}");
        }

        public void Start()
        {
            _hostContext.Log(LogLevel.Info, $"PluginA (Instance: {_instanceId}): Started.");
        }

        public void Stop()
        {
            _hostContext.Log(LogLevel.Info, $"PluginA (Instance: {_instanceId}): Stopped.");
        }

        public Task<object> ExecuteCommand(string commandName, object payload)
        {
            _hostContext.Log(LogLevel.Info, $"PluginA (Instance: {_instanceId}): Executing command '{commandName}' with payload: {payload}");
            if (commandName.Equals("echo", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>($"PluginA echoes: {payload}");
            }
            else if (commandName.Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                if (payload is System.Text.Json.JsonElement jsonElement && jsonElement.TryGetProperty("a", out var aElement) && jsonElement.TryGetProperty("b", out var bElement))
                {
                    if (aElement.TryGetInt32(out int a) && bElement.TryGetInt32(out int b))
                    {
                        return Task.FromResult<object>(a + b);
                    }
                }
                return Task.FromResult<object>("PluginA add command: Invalid payload. Expected { \"a\": number, \"b\": number }");
            }
            return Task.FromResult<object>($"PluginA: Unknown command '{commandName}'");
        }
    }
} 