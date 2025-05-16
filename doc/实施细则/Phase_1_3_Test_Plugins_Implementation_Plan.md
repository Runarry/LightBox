# 阶段 1.3: 测试插件完成 - 实现计划

本文档详细规划了“阶段1.3 测试插件完成”的实现细节，涉及 PluginA (C# 库插件) 和 PluginB (外部进程插件) 的具体实现，以及相应的测试和验证流程。

## 1. PluginA (C# 库插件) 实现

**目标：** 完成一个基础的 C# 库插件，实现 `ILightBoxPlugin` 接口，并提供一个简单的命令。

**文件位置：** [`TestPlugins/PluginA/Plugin.cs`](TestPlugins/PluginA/Plugin.cs)
**项目文件：** [`TestPlugins/PluginA/PluginA.csproj`](TestPlugins/PluginA/PluginA.csproj)
**清单文件：** [`TestPlugins/PluginA/manifest.json`](TestPlugins/PluginA/manifest.json)

### 1.1. `Plugin.cs` 实现

```csharp
using LightBox.PluginContracts;
using System;
using System.Threading.Tasks;

namespace TestPlugins.PluginA
{
    public class Plugin : ILightBoxPlugin
    {
        private ILightBoxHostContext _hostContext;
        private string _instanceId;

        public string Id => "test.plugin.a"; // 与 manifest.json 中的 id 一致

        public void Initialize(ILightBoxHostContext hostContext, string instanceId, string configurationJson)
        {
            _hostContext = hostContext;
            _instanceId = instanceId;
            _hostContext.Log(LogLevel.Info, $"PluginA (Instance: {_instanceId}): Initialized with config: {configurationJson}");
        }

        public void Start()
        {
            _hostContext.Log(LogLevel.Info, $"PluginA (Instance: {_instanceId}): Started.");
            // 可以在这里添加插件启动后的逻辑，例如启动一个后台任务
        }

        public void Stop()
        {
            _hostContext.Log(LogLevel.Info, $"PluginA (Instance: {_instanceId}): Stopped.");
            // 可以在这里添加插件停止前的清理逻辑
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
```

### 1.2. `manifest.json` 更新

确保 [`TestPlugins/PluginA/manifest.json`](TestPlugins/PluginA/manifest.json) 内容如下：

```json
{
  "id": "test.plugin.a",
  "name": "Test Plugin A (C# Library)",
  "version": "1.0.0",
  "description": "A test C# library plugin for LightBox.",
  "author": "LightBox Team",
  "plugin_type": "csharp_library",
  "entry_point": {
    "assembly_path": "PluginA.dll", // 相对于插件目录
    "main_class": "TestPlugins.PluginA.Plugin"
  },
  "config_schema": {
    "type": "object",
    "properties": {
      "messagePrefix": {
        "type": "string",
        "title": "Message Prefix",
        "description": "A prefix for messages logged by PluginA.",
        "default": "PluginA:"
      }
    }
  }
}
```

### 1.3. 项目文件 `PluginA.csproj`

确保项目目标框架与 `LightBox.PluginContracts` 兼容 (例如 `net9.0`) 并引用 `LightBox.PluginContracts`。

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework> 
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\LightBox.PluginContracts\LightBox.PluginContracts.csproj" />
  </ItemGroup>

</Project>
```

## 2. PluginB (外部进程插件) 实现

**目标：** 完成一个基础的外部进程插件，该插件是一个简单的 .NET 控制台应用程序，启动后打印接收到的参数，并能响应简单的 HTTP 请求。

**项目目录：** `TestPlugins/PluginB/`
**清单文件：** [`TestPlugins/PluginB/manifest.json`](TestPlugins/PluginB/manifest.json)
**可执行文件（示例）：** `TestPlugins/PluginB/bin/Debug/net9.0/PluginB.exe` (编译后)

### 2.1. `PluginB` (C# Console App) 实现

创建一个新的 C# 控制台项目 `PluginB` 在 `TestPlugins/PluginB/` 目录下。

**`Program.cs` (示例):**

```csharp
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"PluginB: Started with arguments: {string.Join(" ", args)}");

        string portStr = "8091"; // Default port
        string instanceId = "unknown_instance";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
            {
                portStr = args[i + 1];
            }
            if (args[i] == "--instance-id" && i + 1 < args.Length)
            {
                instanceId = args[i + 1];
            }
        }

        if (!int.TryParse(portStr, out int port))
        {
            port = 8091; // Fallback to default if parsing fails
            Console.WriteLine($"PluginB: Invalid port specified, using default {port}");
        }
        
        Console.WriteLine($"PluginB (Instance: {instanceId}): Attempting to listen on port {port}");

        HttpListener listener = new HttpListener();
        try
        {
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            Console.WriteLine($"PluginB (Instance: {instanceId}): Listening on http://localhost:{port}/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PluginB (Instance: {instanceId}): Error starting listener on port {port}. {ex.Message}");
            return; // Exit if listener fails to start
        }
        
        // Handle Ctrl+C or termination signal for graceful shutdown
        Console.CancelKeyPress += (sender, eventArgs) => {
            Console.WriteLine($"PluginB (Instance: {instanceId}): Stopping listener...");
            listener.Stop();
            listener.Close();
            Console.WriteLine($"PluginB (Instance: {instanceId}): Stopped.");
            eventArgs.Cancel = true; // Prevent process termination by Ctrl+C if we want to do more cleanup
            Environment.Exit(0);
        };

        try
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = $"<HTML><BODY>Hello from PluginB (Instance: {instanceId})! Path: {request.Url.AbsolutePath}</BODY></HTML>";
                if (request.Url.AbsolutePath == "/ping")
                {
                    responseString = $"PluginB (Instance: {instanceId}) pong at {DateTime.Now}";
                }
                else if (request.Url.AbsolutePath == "/config")
                {
                    // Example: Read a config file passed via args or known location
                    // For simplicity, just returning a message
                    responseString = $"PluginB (Instance: {instanceId}) config endpoint. Args: {string.Join(" ", args)}";
                }


                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                output.Close();
                Console.WriteLine($"PluginB (Instance: {instanceId}): Responded to {request.Url}");
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Operation Aborted (typically on Stop)
        {
            Console.WriteLine($"PluginB (Instance: {instanceId}): Listener stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PluginB (Instance: {instanceId}): An error occurred: {ex.Message}");
        }
        finally
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
            listener.Close();
            Console.WriteLine($"PluginB (Instance: {instanceId}): Final shutdown.");
        }
    }
}
```

**`PluginB.csproj` (示例):**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```
编译此项目 (`dotnet build`)，并将生成的可执行文件路径配置到 `manifest.json`。

### 2.2. `manifest.json` 更新

确保 [`TestPlugins/PluginB/manifest.json`](TestPlugins/PluginB/manifest.json) 内容如下：

```json
{
  "id": "test.plugin.b",
  "name": "Test Plugin B (External Process)",
  "version": "1.0.0",
  "description": "A test external process plugin for LightBox.",
  "author": "LightBox Team",
  "plugin_type": "external_process",
  "entry_point": {
    "executable": "PluginB.exe", // Relative to plugin directory, or use an absolute path for testing
                                  // For a built .NET app, this might be "bin/Debug/net9.0/PluginB.exe"
                                  // Or a script like "run.bat" or "python my_plugin.py"
    "args_template": "--port {{config.port}} --instance-id {{instance_id}} --config-file {{temp_config_file_path}}"
    // temp_config_file_path will be provided by PluginManager if config_schema is present and needs a temp file
  },
  "config_schema": {
    "type": "object",
    "properties": {
      "port": {
        "type": "integer",
        "title": "HTTP Port",
        "description": "The port PluginB will listen on.",
        "default": 8091,
        "minimum": 1024,
        "maximum": 65535
      },
      "startupMessage": {
        "type": "string",
        "title": "Startup Message",
        "description": "A message to include in the startup log.",
        "default": "PluginB is starting up!"
      }
    },
    "required": [
      "port"
    ]
  },
  "communication": { // Optional: hints for how the host might communicate, beyond args
    "type": "http", // "stdio", "http", "custom"
    "base_url_template": "http://localhost:{{config.port}}" // If type is http
  }
}
```
**注意:** `executable` 路径需要根据实际编译输出或脚本位置进行调整。如果使用 `dotnet run --project TestPlugins/PluginB/PluginB.csproj` 这样的命令，`executable` 可能需要指向 `dotnet.exe`，而 `args_template` 会包含 `run --project ...`。更简单的方式是直接指向编译好的 `PluginB.exe`。

## 3. 插件测试和验证流程

测试将主要在 [`LightBox.Core.Tests`](LightBox.Core.Tests) 项目中进行，通过扩展 [`PluginManagerTests.cs`](LightBox.Core.Tests/PluginManagerTests.cs) 或创建新的测试类。

### 3.1. 测试环境准备

*   确保 `TestPlugins/PluginA/PluginA.dll` 和 `TestPlugins/PluginB/PluginB.exe` (及其依赖) 已编译并放置在 `PluginManager` 可扫描到的路径下。
*   `ApplicationSettings` 中配置的插件扫描路径应包含 `TestPlugins` 目录。

### 3.2. `PluginManager` 核心功能测试

**文件：** [`LightBox.Core.Tests/PluginManagerTests.cs`](LightBox.Core.Tests/PluginManagerTests.cs)

*   **`DiscoverPluginsAsync_Should_Find_PluginA_And_PluginB`**:
    *   验证 `DiscoverPluginsAsync` 返回的列表中包含 PluginA 和 PluginB 的定义。
    *   检查 `PluginDefinition` 的属性是否与 `manifest.json` 一致。
*   **`CreatePluginInstanceAsync_Should_Create_Instance_For_PluginA`**:
    *   调用 `CreatePluginInstanceAsync` 创建 PluginA 实例。
    *   验证返回的 `PluginInstanceInfo` 状态为 `Created` 或 `Initialized` (取决于实现)。
    *   验证 `Workspace` 中已添加该插件实例。
*   **`InitializePluginInstanceAsync_Should_Initialize_PluginA`**:
    *   创建 PluginA 实例后，调用 `InitializePluginInstanceAsync`。
    *   验证插件实例状态变为 `Initialized` (或 `ReadyToStart`)。
    *   (需要 `ILoggingService` mock 或真实输出来验证 PluginA 的 `Initialize` 方法中的日志)。
*   **`StartPluginInstanceAsync_Should_Start_PluginA`**:
    *   初始化 PluginA 实例后，调用 `StartPluginInstanceAsync`。
    *   验证插件实例状态变为 `Running`。
    *   (验证 PluginA 的 `Start` 方法中的日志)。
*   **`ExecuteCommandAsync_On_PluginA_Should_Return_Echoed_String`**:
    *   启动 PluginA 实例后，通过 `PluginManager` (或直接在实例上，如果可访问) 调用 `ExecuteCommand("echo", "hello")`。
    *   验证返回结果为 "PluginA echoes: hello"。
*   **`StopPluginInstanceAsync_Should_Stop_PluginA`**:
    *   启动 PluginA 实例后，调用 `StopPluginInstanceAsync`。
    *   验证插件实例状态变为 `Stopped`。
    *   (验证 PluginA 的 `Stop` 方法中的日志)。
*   **`CreatePluginInstanceAsync_Should_Start_Process_For_PluginB`**:
    *   调用 `CreatePluginInstanceAsync` 创建 PluginB 实例。
    *   验证 `PluginManager` 启动了 `PluginB.exe` 进程。
    *   验证进程接收到了通过 `args_template` 格式化后的参数 (例如，正确的端口号，实例ID，临时配置文件路径)。
    *   验证插件实例状态为 `Running` (外部进程插件通常创建即运行)。
*   **`StopPluginInstanceAsync_Should_Terminate_PluginB_Process`**:
    *   创建 PluginB 实例后，调用 `StopPluginInstanceAsync`。
    *   验证 `PluginB.exe` 进程已被终止。
    *   验证插件实例状态变为 `Stopped`。

### 3.3. PluginA 功能验证 (集成测试)

*   **日志验证：**
    *   在 `PluginManagerTests` 中，注入一个 mock `ILoggingService` 或配置一个可检查的日志输出。
    *   在调用 `Initialize`, `Start`, `Stop`, `ExecuteCommand` 后，验证 `ILoggingService` 是否收到了 PluginA 发出的预期日志消息。
*   **命令执行验证：**
    *   测试 `echo` 命令，使用不同 payload。
    *   测试 `add` 命令，使用正确和错误的 payload 格式。
    *   测试未知命令，验证返回合适的错误信息。

### 3.4. PluginB 功能验证 (集成/手动测试)

*   **进程启动和参数验证：**
    *   在 `PluginManagerTests` 中，当 PluginB 实例创建后，检查系统进程列表确认 `PluginB.exe` 正在运行。
    *   检查 PluginB 控制台输出 (如果可捕获) 或其自身日志文件 (如果实现) 确认接收到的参数正确。
*   **HTTP 服务验证：**
    *   PluginB 启动后，使用 `HttpClient` 从测试代码中向 `http://localhost:{configured_port}/ping` 发送请求。
    *   验证响应是否为 "PluginB (Instance: {instance_id}) pong at {DateTime.Now}"。
    *   向 `http://localhost:{configured_port}/config` 发送请求，验证响应。
*   **进程停止验证：**
    *   调用 `StopPluginInstanceAsync` 后，确认 `PluginB.exe` 进程已不存在。
    *   尝试再次访问其 HTTP 端点，应失败。

### 3.5. 配置文件和 `IConfigurationService` 测试

*   **PluginA 配置测试：**
    *   在创建 PluginA 实例时提供一个包含 `messagePrefix` 的 `initialConfigurationJson`。
    *   验证 PluginA 在 `Initialize` 时收到的 `configurationJson` 正确。
    *   (可选) 修改 PluginA 的 `Initialize` 方法，使其使用 `messagePrefix`。
*   **PluginB 配置测试：**
    *   在创建 PluginB 实例时，`PluginManager` 应使用 `IConfigurationService` 来处理 `config_schema`。
    *   验证 `IConfigurationService.GenerateTempConfigFile` 被调用 (如果 `args_template` 包含 `{{temp_config_file_path}}`)。
    *   验证生成的临时配置文件内容正确 (基于 `config_schema` 的默认值或提供的 `initialConfigurationJson`)。
    *   验证 PluginB 进程启动时，`args_template` 中的 `{{config.port}}` 被正确替换。

## 4. 涉及的核心文件和接口

*   **核心服务：**
    *   [`LightBox.Core/Services/Interfaces/IPluginService.cs`](LightBox.Core/Services/Interfaces/IPluginService.cs)
    *   [`LightBox.Core/Services/Implementations/PluginManager.cs`](LightBox.Core/Services/Implementations/PluginManager.cs)
    *   [`LightBox.Core/Services/Interfaces/IConfigurationService.cs`](LightBox.Core/Services/Interfaces/IConfigurationService.cs)
    *   [`LightBox.Core/Services/Implementations/ConfigurationService.cs`](LightBox.Core/Services/Implementations/ConfigurationService.cs)
    *   [`LightBox.Core/Services/Interfaces/ILoggingService.cs`](LightBox.Core/Services/Interfaces/ILoggingService.cs)
*   **插件契约：**
    *   [`LightBox.PluginContracts/ILightBoxPlugin.cs`](LightBox.PluginContracts/ILightBoxPlugin.cs)
    *   [`LightBox.PluginContracts/ILightBoxHostContext.cs`](LightBox.PluginContracts/ILightBoxHostContext.cs)
*   **数据模型：**
    *   [`LightBox.Core/Models/PluginDefinition.cs`](LightBox.Core/Models/PluginDefinition.cs)
    *   [`LightBox.Core/Models/PluginInstance.cs`](LightBox.Core/Models/PluginInstance.cs)
    *   [`LightBox.Core/Models/PluginInstanceInfo.cs`](LightBox.Core/Models/PluginInstanceInfo.cs)
    *   [`LightBox.Core/Models/PluginInstanceStatus.cs`](LightBox.Core/Models/PluginInstanceStatus.cs)
*   **测试项目：**
    *   [`LightBox.Core.Tests/PluginManagerTests.cs`](LightBox.Core.Tests/PluginManagerTests.cs)

此计划提供了完成阶段1.3所需的详细步骤和代码参考。在实现过程中，可能需要根据实际情况进行微调。