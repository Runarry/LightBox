using LightBox.PluginContracts;
using System;
using System.Threading.Tasks;

namespace TestPluginA
{
    public class Plugin : ILightBoxPlugin
    {
        public string Id => "test-plugin-a";

        public void Initialize(ILightBoxHostContext hostContext, string instanceId, string configurationJson)
        {
            Console.WriteLine($"Test Plugin A initialized with instance ID: {instanceId}");
        }

        public void Start()
        {
            Console.WriteLine("Test Plugin A started.");
        }

        public void Stop()
        {
            Console.WriteLine("Test Plugin A stopped.");
        }

        public Task<object> ExecuteCommand(string commandName, object payload)
        {
            Console.WriteLine($"Test Plugin A executing command: {commandName}");
            return Task.FromResult<object>($"Command {commandName} executed");
        }
    }
} 